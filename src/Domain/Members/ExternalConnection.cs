using System;
using System.Collections.Generic;
using Domain.Shared;
using NodaTime;

namespace Domain.Members;

/// <summary>
/// Entity representing a member's authenticated link to an external service.
/// </summary>
/// <remarks>
/// Owned by the <see cref="Member"/> aggregate root. An external connection
/// has no meaningful existence outside of its parent member.
/// <para>
/// Invariant: At most one <see cref="ConnectionStatus.Active"/> connection
/// may exist per member + <see cref="Provider"/> pair.
/// </para>
/// </remarks>
public sealed class ExternalConnection : IEntity<Guid>, IHasAudit
{
    private ExternalConnection() { }

    /// <inheritdoc/>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the identifier of the member who owns this connection.
    /// </summary>
    public Guid MemberId { get; private set; }

    /// <summary>
    /// Gets the external service provider this connection links to.
    /// </summary>
    public ExternalProvider Provider { get; private set; } = default!;

    /// <summary>
    /// Gets the member's unique identifier on the external platform.
    /// </summary>
    /// <example>12345678 (Strava athlete ID), octocat (GitHub username)</example>
    public string ExternalUserId { get; private set; } = default!;

    /// <summary>
    /// Gets the current lifecycle status of this connection.
    /// </summary>
    public ConnectionStatus Status { get; private set; } = default!;

    /// <summary>
    /// Gets the UTC instant at which the connection was first established.
    /// </summary>
    public Instant ConnectedAt { get; private set; }

    /// <summary>
    /// Gets the UTC instant at which the connection was revoked or expired.
    /// <c>null</c> while the connection is <see cref="ConnectionStatus.Active"/>.
    /// </summary>
    public Instant? DisconnectedAt { get; private set; }

    /// <summary>
    /// Gets the OAuth scopes granted by the member during authorization.
    /// </summary>
    /// <example>["read", "activity:read_all"]</example>
    public IReadOnlyList<string> Scopes { get; private set; } = [];

    /// <inheritdoc/>
    public Instant CreatedAt { get; private set; }

    /// <inheritdoc/>
    public Guid CreatedBy { get; private set; }

    /// <inheritdoc/>
    public Instant LastUpdatedAt { get; private set; }

    /// <inheritdoc/>
    public Guid LastUpdatedBy { get; private set; }

    /// <summary>
    /// Creates a new active external connection for a member.
    /// </summary>
    /// <param name="memberId">The identifier of the member establishing the connection.</param>
    /// <param name="provider">The external service provider being connected.</param>
    /// <param name="externalUserId">The member's identifier on the external platform.</param>
    /// <param name="scopes">The OAuth scopes granted during authorization.</param>
    /// <param name="clock">The system clock providing the current UTC instant.</param>
    /// <returns>A new active <see cref="ExternalConnection"/> instance.</returns>
    internal static ExternalConnection Create(
        Guid memberId,
        ExternalProvider provider,
        string externalUserId,
        IReadOnlyList<string> scopes,
        IClock clock)
    {
        var now = clock.GetCurrentInstant();

        return new ExternalConnection
        {
            Id = Guid.NewGuid(),
            MemberId = memberId,
            Provider = provider,
            ExternalUserId = externalUserId,
            Status = ConnectionStatus.Active,
            ConnectedAt = now,
            DisconnectedAt = null,
            Scopes = scopes ?? [],
            CreatedAt = now,
            CreatedBy = memberId,
            LastUpdatedAt = now,
            LastUpdatedBy = memberId
        };
    }

    /// <summary>
    /// Revokes this connection, marking it as explicitly disconnected by the member.
    /// </summary>
    /// <param name="clock">The system clock providing the current UTC instant.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the connection is not currently <see cref="ConnectionStatus.Active"/>.
    /// </exception>
    internal void Revoke(IClock clock)
    {
        if (Status != ConnectionStatus.Active)
        {
            throw new InvalidOperationException(
                $"Cannot revoke a connection that is in '{Status.Name}' status.");
        }

        var now = clock.GetCurrentInstant();
        Status = ConnectionStatus.Revoked;
        DisconnectedAt = now;
        LastUpdatedAt = now;
        LastUpdatedBy = MemberId;
    }

    /// <summary>
    /// Reactivates a previously revoked or expired connection with updated credentials.
    /// </summary>
    /// <param name="externalUserId">The (possibly updated) external user identifier.</param>
    /// <param name="scopes">The newly granted OAuth scopes.</param>
    /// <param name="clock">The system clock providing the current UTC instant.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the connection is already <see cref="ConnectionStatus.Active"/>.
    /// </exception>
    internal void Reactivate(string externalUserId, IReadOnlyList<string> scopes, IClock clock)
    {
        if (Status == ConnectionStatus.Active)
        {
            throw new InvalidOperationException(
                "Cannot reactivate a connection that is already active.");
        }

        var now = clock.GetCurrentInstant();
        ExternalUserId = externalUserId;
        Scopes = scopes ?? [];
        Status = ConnectionStatus.Active;
        DisconnectedAt = null;
        ConnectedAt = now;
        LastUpdatedAt = now;
        LastUpdatedBy = MemberId;
    }
}
