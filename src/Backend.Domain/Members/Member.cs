using System;
using System.Collections.Generic;
using System.Linq;
using Backend.Domain.ExternalConnections;
using Backend.Domain.Members.Events;
using Backend.Domain.Shared;
using NodaTime;

namespace Backend.Domain.Members;

/// <summary>
/// Aggregate root representing a registered member of the platform.
/// </summary>
/// <remarks>
/// Owns a collection of <see cref="ExternalConnection"/> entities representing
/// the member's authenticated links to external services (e.g., Strava, GitHub).
/// All connection lifecycle operations are performed through the member aggregate
/// to maintain consistency within the aggregate boundary.
/// </remarks>
public class Member(
    string firstName,
    string middleName,
    string lastName,
    string description,
    string pathAvatar,
    string preferredTimezone) : AggregateRoot<Guid>
{
    private readonly List<ExternalConnection> _externalConnections = [];
    private readonly List<PersonalPlan> _personalPlans = [];

    /// <summary>
    /// Gets the first name of the member.
    /// </summary>
    public string FirstName { get; private set; } = firstName;

    /// <summary>
    /// Gets the middle name of the member. Can be empty.
    /// </summary>
    public string MiddleName { get; private set; } = middleName;

    /// <summary>
    /// Gets the last name of the member.
    /// </summary>
    public string LastName { get; private set; } = lastName;

    /// <summary>
    /// Gets the free-text description or bio of the member. Can be empty.
    /// </summary>
    public string Description { get; private set; } = description;

    /// <summary>
    /// Gets the path or URL to the member's avatar image. Can be empty.
    /// </summary>
    public string PathAvatar { get; private set; } = pathAvatar;

    /// <summary>
    /// Gets the IANA timezone identifier preferred by the member.
    /// </summary>
    /// <example>Asia/Ho_Chi_Minh</example>
    public string PreferredTimezone { get; private set; } = preferredTimezone;

    /// <summary>
    /// Gets all external service connections owned by this member.
    /// </summary>
    public IReadOnlyList<ExternalConnection> ExternalConnections => _externalConnections.AsReadOnly();

    /// <summary>
    /// Gets all personal plan subscriptions owned by this member.
    /// </summary>
    public IReadOnlyList<PersonalPlan> PersonalPlans => _personalPlans.AsReadOnly();

    /// <summary>
    /// Establishes a new connection to an external service provider.
    /// If a revoked or expired connection already exists for the same provider,
    /// it will be reactivated instead of creating a duplicate.
    /// </summary>
    /// <param name="provider">The external service provider to connect.</param>
    /// <param name="externalUserId">The member's unique identifier on the external platform.</param>
    /// <param name="scopes">The OAuth scopes granted during authorization.</param>
    /// <param name="clock">The system clock providing the current UTC instant.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when an active connection to the same provider already exists.
    /// </exception>
    public void ConnectExternalProvider(
        ExternalProvider provider,
        string externalUserId,
        IReadOnlyList<string> scopes,
        IClock clock)
    {
        var existing = _externalConnections
            .FirstOrDefault(c => c.Provider.Id == provider.Id);

        if (existing is not null && existing.Status == ConnectionStatus.Active)
        {
            throw new InvalidOperationException(
                $"An active connection to '{provider.Name}' already exists.");
        }

        if (existing is not null)
        {
            // Reactivate a previously revoked or expired connection.
            existing.Reactivate(externalUserId, scopes, clock);
        }
        else
        {
            // Create a brand-new connection.
            var connection = ExternalConnection.Create(Id, provider, externalUserId, scopes, clock);
            _externalConnections.Add(connection);
            existing = connection;
        }

        StampUpdatedAudit(Id, clock);

        RaiseDomainEvent(new ExternalConnectionEstablishedEvent(
            Id,
            existing.Id,
            provider,
            externalUserId,
            clock,
            $"{nameof(Member)} / {nameof(ConnectExternalProvider)}"));
    }

    /// <summary>
    /// Revokes an existing connection to an external service provider.
    /// </summary>
    /// <param name="provider">The external service provider to disconnect.</param>
    /// <param name="clock">The system clock providing the current UTC instant.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no active connection to the specified provider exists.
    /// </exception>
    public void RevokeExternalProvider(ExternalProvider provider, IClock clock)
    {
        var existing = _externalConnections
            .FirstOrDefault(c => c.Provider.Id == provider.Id && c.Status == ConnectionStatus.Active);

        if (existing is null)
        {
            throw new InvalidOperationException(
                $"No active connection to '{provider.Name}' exists.");
        }

        existing.Revoke(clock);

        StampUpdatedAudit(Id, clock);

        RaiseDomainEvent(new ExternalConnectionRevokedEvent(
            Id,
            existing.Id,
            provider,
            clock,
            $"{nameof(Member)} / {nameof(RevokeExternalProvider)}"));
    }

    /// <summary>
    /// Checks whether this member has an active connection to the specified provider.
    /// </summary>
    /// <param name="provider">The external service provider to check.</param>
    /// <returns><c>true</c> if an active connection exists; otherwise, <c>false</c>.</returns>
    public bool HasActiveConnection(ExternalProvider provider)
    {
        return _externalConnections
            .Any(c => c.Provider.Id == provider.Id && c.Status == ConnectionStatus.Active);
    }

    /// <summary>
    /// Subscribes this member to a plan, creating a new <see cref="PersonalPlan"/> entity.
    /// </summary>
    /// <param name="planId">The identifier of the plan to subscribe to.</param>
    /// <param name="displayOnProfile">Whether the plan appears on the member's public profile.</param>
    /// <param name="prioritize">Display priority on the member's profile. Range: 0–999.</param>
    /// <param name="linkUserAdapter">Whether Strava activity sync is enabled for this plan.</param>
    /// <param name="clock">The system clock providing the current UTC instant.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the member is already subscribed to the specified plan.
    /// </exception>
    public void SubscribeToPlan(
        Guid planId,
        bool displayOnProfile,
        int prioritize,
        bool linkUserAdapter,
        IClock clock)
    {
        if (_personalPlans.Any(p => p.PlanId == planId))
        {
            throw new InvalidOperationException(
                $"Member is already subscribed to plan '{planId}'.");
        }

        var personalPlan = PersonalPlan.Create(
            Id,
            planId,
            displayOnProfile,
            prioritize,
            linkUserAdapter,
            clock);

        _personalPlans.Add(personalPlan);

        StampUpdatedAudit(Id, clock);

        RaiseDomainEvent(new MemberSubscribedToPlanEvent(
            Id,
            personalPlan.Id,
            planId,
            displayOnProfile,
            prioritize,
            linkUserAdapter,
            clock,
            $"{nameof(Member)} / {nameof(SubscribeToPlan)}"));
    }

    /// <summary>
    /// Removes the member's subscription to a plan.
    /// </summary>
    /// <param name="planId">The identifier of the plan to unsubscribe from.</param>
    /// <param name="clock">The system clock providing the current UTC instant.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the member is not currently subscribed to the specified plan.
    /// </exception>
    public void UnsubscribeFromPlan(Guid planId, IClock clock)
    {
        var personalPlan = _personalPlans.FirstOrDefault(p => p.PlanId == planId);

        if (personalPlan is null)
        {
            throw new InvalidOperationException(
                $"Member is not subscribed to plan '{planId}'.");
        }

        _personalPlans.Remove(personalPlan);

        StampUpdatedAudit(Id, clock);
    }

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(FirstName))
        {
            errors.Add(new ValidationError(
                "firstName", "First name is required.", "REQUIRED_FIRST_NAME"));
        }

        if (string.IsNullOrWhiteSpace(LastName))
        {
            errors.Add(new ValidationError(
                "lastName", "Last name is required.", "REQUIRED_LAST_NAME"));
        }

        if (string.IsNullOrWhiteSpace(PreferredTimezone))
        {
            errors.Add(new ValidationError(
                "preferredTimezone", "Preferred timezone is required.", "REQUIRED_TIMEZONE"));
        }
        else if (DateTimeZoneProviders.Tzdb.GetZoneOrNull(PreferredTimezone) is null)
        {
            errors.Add(new ValidationError(
                "preferredTimezone",
                $"'{PreferredTimezone}' is not a valid IANA timezone identifier.",
                "INVALID_TIMEZONE"));
        }

        return errors.Count == 0
            ? new ValidationResult(new List<ValidationError>().AsReadOnly())
            : new ValidationResult(new List<ValidationError>(errors).AsReadOnly());
    }
}
