using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Members;
using Infrastructure.Services;
using Microsoft.Extensions.Logging;
using NodaTime;
using Quartz;

namespace Infrastructure.BackgroundJobClient.Jobs;

/// <summary>
/// Quartz job to synchronize federated identities from Keycloak for a member.
/// </summary>
/// <remarks>
/// Marked with <see cref="DisallowConcurrentExecutionAttribute"/> to prevent database concurrency
/// exceptions and duplicate connection inserts if multiple triggers fire for the same member simultaneously.
/// </remarks>
/// <param name="keycloakAdminClient">The client used to interact with the Keycloak Admin API.</param>
/// <param name="memberRepository">The repository used to fetch and update member aggregates.</param>
/// <param name="clock">The system clock used to timestamp new connections.</param>
/// <param name="logger">The logger instance.</param>
[DisallowConcurrentExecution]
public partial class SyncIdentityJob(
    IKeycloakAdminClient keycloakAdminClient,
    IMemberRepository memberRepository,
    IClock clock,
    ILogger<SyncIdentityJob> logger) : IJob
{
    private readonly IKeycloakAdminClient _keycloakAdminClient = keycloakAdminClient;
    private readonly IMemberRepository _memberRepository = memberRepository;
    private readonly IClock _clock = clock;
    private readonly ILogger<SyncIdentityJob> _logger = logger;
    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var dataMap = context.MergedJobDataMap;
        var memberIdString = dataMap.GetString("MemberId");
        var identifyName = dataMap.GetString("IdentifyName");

        if (!Guid.TryParse(memberIdString, out var memberId) || string.IsNullOrEmpty(identifyName))
        {
            LogInvalidJobData();
            return;
        }

        var member = await _memberRepository.GetByIdAsync(memberId, context.CancellationToken);
        if (member == null)
        {
            LogMemberNotFound(memberId);
            return;
        }

        var keycloakConnection = member.ExternalConnections
            .FirstOrDefault(c => c.Provider == ExternalProvider.Keycloak && 
                                 c.Type == ExternalConnectionType.Identity && 
                                 c.Status == ConnectionStatus.Active);
        
        if (keycloakConnection == null)
        {
            LogKeycloakConnectionNotFound(memberId);
            return;
        }

        await SyncIdentitiesAsync(member, keycloakConnection.ExternalUserId, identifyName, context.CancellationToken);
    }

    private async Task SyncIdentitiesAsync(Member member, string externalUserId, string identifyName, CancellationToken cancellationToken)
    {
        try
        {
            var identities = await _keycloakAdminClient.GetUserFederatedIdentitiesAsync(externalUserId, cancellationToken);
            
            bool hasChanges = false;
            foreach (var identity in identities)
            {
                if (ProcessIdentity(member, identity, identifyName))
                {
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await _memberRepository.SaveChangesAsync(cancellationToken);
                LogIdentitiesSynced(member.Id);
            }
        }
        catch (Exception ex)
        {
            LogSyncFailed(ex, member.Id);
            throw; // Rethrow to let Quartz handle retries if configured
        }
    }

    /// <summary>
    /// Processes a single federated identity from Keycloak and ensures the member has a corresponding connection.
    /// </summary>
    /// <param name="member">The member aggregate.</param>
    /// <param name="identity">The federated identity fetched from Keycloak.</param>
    /// <param name="identifyName">The local member's identifying name, used for logging.</param>
    /// <returns><c>true</c> if a new connection was added; otherwise, <c>false</c>.</returns>
    private bool ProcessIdentity(Member member, FederatedIdentityDto identity, string identifyName)
    {
        try 
        {
            var provider = ExternalProvider.FromId(identity.IdentityProvider.ToUpperInvariant());
            if (!member.HasActiveConnection(provider, ExternalConnectionType.Identity))
            {
                member.ConnectExternalProvider(
                    provider,
                    ExternalConnectionType.Identity,
                    identity.UserId,
                    [], // No scopes for identity sync
                    _clock);
                
                return true;
            }
        }
        catch (ArgumentException ex)
        {
            LogUnrecognizedProvider(ex, identity.IdentityProvider, identifyName);
        }

        return false;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "SyncIdentityJob: Invalid or missing MemberId/IdentifyName in JobDataMap.")]
    private partial void LogInvalidJobData();

    [LoggerMessage(Level = LogLevel.Warning, Message = "SyncIdentityJob: Member {MemberId} not found.")]
    private partial void LogMemberNotFound(Guid memberId);

    [LoggerMessage(Level = LogLevel.Information, Message = "SyncIdentityJob: Synced federated identities for Member {MemberId}.")]
    private partial void LogIdentitiesSynced(Guid memberId);

    [LoggerMessage(Level = LogLevel.Error, Message = "SyncIdentityJob: Failed to sync federated identities for Member {MemberId}.")]
    private partial void LogSyncFailed(Exception ex, Guid memberId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "SyncIdentityJob: Unrecognized identity provider '{IdentityProvider}' for user '{IdentifyName}'")]
    private partial void LogUnrecognizedProvider(Exception ex, string identityProvider, string identifyName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "SyncIdentityJob: Active Keycloak identity connection not found for Member {MemberId}.")]
    private partial void LogKeycloakConnectionNotFound(Guid memberId);
}
