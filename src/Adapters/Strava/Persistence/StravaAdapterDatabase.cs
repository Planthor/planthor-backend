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
        var database = mongoClient.GetDatabase("strava_activity_sync_adapter_db");
        _tokens = database.GetCollection<StravaTokenDocument>("strava_tokens");
    }

    /// <summary>
    /// Retrieves a token document by the Planthor member's identity name (Keycloak Subject ID).
    /// </summary>
    /// <param name="identifyName">The Planthor member's Keycloak Identity Name.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The token document, or <c>null</c> if no document exists for the member.</returns>
    public async Task<StravaTokenDocument?> GetByIdentifyNameAsync(
        string identifyName,
        CancellationToken cancellationToken)
    {
        var filter = Builders<StravaTokenDocument>.Filter.Eq(t => t.Id, identifyName);
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
    public Task UpsertAsync(
        StravaTokenDocument document,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        return Core();

        async Task Core()
        {
            var filter = Builders<StravaTokenDocument>.Filter.Eq(t => t.Id, document.Id);
            await _tokens.ReplaceOneAsync(
                filter,
                document,
                new ReplaceOptions { IsUpsert = true },
                cancellationToken);
        }
    }

    /// <summary>
    /// Deletes a token document by the Planthor member's identity name.
    /// </summary>
    /// <param name="identifyName">The Planthor member's Keycloak Identity Name whose tokens should be removed.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    public async Task DeleteAsync(
        string identifyName,
        CancellationToken cancellationToken)
    {
        var filter = Builders<StravaTokenDocument>.Filter.Eq(t => t.Id, identifyName);
        await _tokens.DeleteOneAsync(filter, cancellationToken);
    }
}
