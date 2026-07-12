using System;
using System.Threading.Tasks;
using Infrastructure.Services;
using Domain.Members;
using Microsoft.Extensions.Logging;
using NodaTime;
using Quartz;

namespace Infrastructure.BackgroundJobClient.Jobs;

/// <summary>
/// Quartz job to synchronize federated identities from Keycloak for a member.
/// </summary>
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

    public async Task Execute(IJobExecutionContext context)
    {
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

        try
        {
            var identities = await _keycloakAdminClient.GetUserFederatedIdentitiesAsync(identifyName, context.CancellationToken);
            
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
                await _memberRepository.SaveChangesAsync(context.CancellationToken);
                LogIdentitiesSynced(memberId);
            }
        }
        catch (Exception ex)
        {
            LogSyncFailed(ex, memberId);
            throw; // Rethrow to let Quartz handle retries if configured
        }
    }

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
}
