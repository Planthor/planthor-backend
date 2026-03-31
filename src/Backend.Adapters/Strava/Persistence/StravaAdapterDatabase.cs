using MongoDB.Driver;

namespace Backend.Adapters.Strava.Persistence;

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

}
