using System;
using System.Threading;
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
public class SyncIdentityJob(
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
            _logger.LogWarning("SyncIdentityJob: Invalid or missing MemberId/IdentifyName in JobDataMap.");
            return;
        }

        var member = await _memberRepository.GetByIdAsync(memberId, context.CancellationToken);
        if (member == null)
        {
            _logger.LogWarning("SyncIdentityJob: Member {MemberId} not found.", memberId);
            return;
        }

        try
        {
            var identities = await _keycloakAdminClient.GetUserFederatedIdentitiesAsync(identifyName, context.CancellationToken);
            
            bool hasChanges = false;
            foreach (var identity in identities)
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
                        
                        hasChanges = true;
                    }
                }
                catch (ArgumentException ex)
                {
                    _logger.LogWarning(ex, "SyncIdentityJob: Unrecognized identity provider '{IdentityProvider}' for user '{IdentifyName}'", identity.IdentityProvider, identifyName);
                }
            }

            if (hasChanges)
            {
                await _memberRepository.SaveChangesAsync(context.CancellationToken);
                _logger.LogInformation("SyncIdentityJob: Synced federated identities for Member {MemberId}.", memberId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SyncIdentityJob: Failed to sync federated identities for Member {MemberId}.", memberId);
            throw; // Rethrow to let Quartz handle retries if configured
        }
    }
}
