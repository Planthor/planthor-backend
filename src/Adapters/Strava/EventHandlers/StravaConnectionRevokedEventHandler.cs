using Adapters.Strava.Client;
using Application.Shared;
using Domain.Members;
using Domain.Members.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Adapters.Strava.EventHandlers;

/// <summary>
/// Listens for the <see cref="ExternalConnectionRevokedEvent"/> and deauthorizes
/// the member's Strava account if the revoked connection belongs to Strava.
/// </summary>
public sealed partial class StravaConnectionRevokedEventHandler : IDomainEventHandler<ExternalConnectionRevokedEvent>
{
    private readonly IStravaApiClient _stravaClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StravaConnectionRevokedEventHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StravaConnectionRevokedEventHandler"/> class.
    /// </summary>
    public StravaConnectionRevokedEventHandler(
        IStravaApiClient stravaClient,
        IServiceProvider serviceProvider,
        ILogger<StravaConnectionRevokedEventHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(stravaClient);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _stravaClient = stravaClient;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task HandleAsync(ExternalConnectionRevokedEvent domainEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        if (domainEvent.Provider.Id != ExternalProvider.Strava.Id)
        {
            return;
        }

        try
        {
            var memberRepository = _serviceProvider.GetRequiredService<IMemberRepository>();

            var member = await memberRepository.GetByIdAsync(domainEvent.MemberId, cancellationToken);

            if (member != null)
            {
                var success = await _stravaClient.DeauthorizeAsync(member.IdentifyName, cancellationToken);
                if (success)
                {
                    LogDeauthorizationSucceeded(member.IdentifyName);
                }
                else
                {
                    LogDeauthorizationFailed(member.IdentifyName);
                }
            }
            else
            {
                LogMemberNotFound(domainEvent.MemberId);
            }
        }
        catch (OperationCanceledException ex)
        {
            LogOperationCanceled(ex, domainEvent.MemberId);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            LogServiceResolutionFailed(ex, domainEvent.MemberId);
        }
        catch (Exception ex)
        {
            LogErrorDeauthorizingStrava(ex, domainEvent.MemberId);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully deauthorized Strava for member {IdentifyName}")]
    private partial void LogDeauthorizationSucceeded(string identifyName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to deauthorize Strava for member {IdentifyName}")]
    private partial void LogDeauthorizationFailed(string identifyName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Member {MemberId} not found during Strava deauthorization")]
    private partial void LogMemberNotFound(Guid memberId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Strava deauthorization was canceled for member {MemberId}")]
    private partial void LogOperationCanceled(Exception ex, Guid memberId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to resolve required services during Strava deauthorization for member {MemberId}")]
    private partial void LogServiceResolutionFailed(Exception ex, Guid memberId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error occurred while deauthorizing Strava for member {MemberId}")]
    private partial void LogErrorDeauthorizingStrava(Exception ex, Guid memberId);
}
