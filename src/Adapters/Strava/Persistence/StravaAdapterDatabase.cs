using MongoDB.Driver;

namespace Adapters.Strava.Persistence;

/// <summary>
/// Provides typed access to the <c>strava_adapter_db</c> MongoDB database.
/// Uses raw <c>MongoDB.Driver</c> — no EF Core dependency required for the adapter's
/// small, simple persistence needs.
/// </summary>
/// <remarks>
/// Registered as a singleton in DI. The underlying <see cref="MongoClient"/> is thread-safe
/// and intended to be reused across the lifetime of the application.
/// </remarks>
public class StravaAdapterDatabase
{
    private readonly IMongoCollection<StravaTokenDocument> _tokens;

    /// <summary>
    /// Initializes a new instance of the <see cref="StravaAdapterDatabase"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client used to access the Strava adapter database.</param>
    public StravaAdapterDatabase(IMongoClient mongoClient)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        var database = mongoClient.GetDatabase("strava_adapter_db");
        _tokens = database.GetCollection<StravaTokenDocument>("strava_tokens");
    }

    /// <summary>
    /// Retrieves a token document by the Planthor member's unique identifier.
    /// </summary>
    /// <param name="memberId">The Planthor member identifier.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The token document, or <c>null</c> if no document exists for the member.</returns>
    public async Task<StravaTokenDocument?> GetByMemberIdAsync(
        Guid memberId,
        CancellationToken cancellationToken)
    {
        var filter = Builders<StravaTokenDocument>.Filter.Eq(t => t.Id, memberId);
        return await _tokens.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves a token document by the Strava athlete numeric identifier.
    /// Used during webhook processing where only the athlete ID is available.
    /// </summary>
    /// <param name="athleteId">The Strava athlete numeric identifier.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The token document, or <c>null</c> if no document exists for the athlete.</returns>
    public async Task<StravaTokenDocument?> GetByAthleteIdAsync(
        long athleteId,
        CancellationToken cancellationToken)
    {
        var filter = Builders<StravaTokenDocument>.Filter.Eq(t => t.AthleteId, athleteId);
        return await _tokens.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Inserts or replaces a token document, keyed by <see cref="StravaTokenDocument.Id"/>.
    /// </summary>
    /// <param name="document">The token document to persist.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    public async Task UpsertAsync(
        StravaTokenDocument document,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);
        var filter = Builders<StravaTokenDocument>.Filter.Eq(t => t.Id, document.Id);
        await _tokens.ReplaceOneAsync(
            filter,
            document,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    /// <summary>
    /// Deletes a token document by the Planthor member's unique identifier.
    /// </summary>
    /// <param name="memberId">The Planthor member identifier whose tokens should be removed.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    public async Task DeleteAsync(
        Guid memberId,
        CancellationToken cancellationToken)
    {
        var filter = Builders<StravaTokenDocument>.Filter.Eq(t => t.Id, memberId);
        await _tokens.DeleteOneAsync(filter, cancellationToken);
    }
}
