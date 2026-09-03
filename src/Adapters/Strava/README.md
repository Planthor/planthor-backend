# Strava rate limit

Once your app is configured in your API dashboard, you can upgrade your access directly from your API Settings Dashboard. This raises your access to:

Athlete capacity of 10
Read read limits: 200 requests / 15min & 2,000 requests / day
Overall rate limits: 400 requests / 15min & 4,000 requests / day

Once your app reaches 10 connected athletes and you’re ready to scale further, please submit your app for review!

## Activity contract

id (long)
external_id (string)
upload_id (long)
name (string)
distance (float - in meters)
moving_time (integer - in seconds)
elapsed_time (integer - in seconds)

`type` is deprecated by Strava. Planthor primarily reads `sport_type` and falls
back to `type` only when `sport_type` is missing or blank.

## Sport-type normalization

Planthor does not expose Strava `sport_type` values as its business contract.
The Strava adapter translates them into canonical Planthor sport-type identifiers
before activities reach application and domain matching rules. Clients can discover
the canonical identifiers through `GET /v1/sport-types` and should use those values
when creating or updating sport plans.

### Supported mappings

| Canonical Planthor ID | Strava `sport_type` values |
| --- | --- |
| `RUN` | `Run`, `TrailRun`, `VirtualRun` |
| `WALK` | `Walk` |
| `HIKE` | `Hike` |
| `RIDE` | `Ride`, `MountainBikeRide`, `GravelRide`, `EBikeRide`, `EMountainBikeRide`, `VirtualRide`, `Velomobile`, `Handcycle`, `Wheelchair` |
| `SWIM` | `Swim` |

Matching is case-insensitive, but integrations should use the exact Strava values
shown above and clients should use the uppercase Planthor identifiers returned by
`GET /v1/sport-types`.

### Wildcard and unsupported activities

- `ALL` is a Planthor plan-selection wildcard; it is not a Strava value and is not
  produced by the mapper.
- `ALL` matches every activity that was successfully normalized to a supported
  canonical Planthor sport type.
- A missing, blank, or unlisted Strava value has no supported mapping. The adapter
  ignores that activity during automatic synchronization, so it does not contribute
  even to a plan selecting `ALL`.

Examples:

- `TrailRun` is normalized to `RUN` and can contribute to plans selecting `RUN` or `ALL`.
- `VirtualRide` is normalized to `RIDE` and can contribute to plans selecting `RIDE` or `ALL`.
- `IceSkate` has no mapping and is ignored during automatic synchronization.

The implementation source of truth is
[`Mapping/StravaSportTypeMapper.cs`](Mapping/StravaSportTypeMapper.cs), with coverage in
[`StravaSportTypeMapperTests.cs`](../../../tests/UnitTests/Adapters.Tests/Strava/Mapping/StravaSportTypeMapperTests.cs).
When a mapping changes, update this table and its tests together. Because mapping
changes affect which activities contribute to plans, describe them as user-visible
behavior in the corresponding release notes.

## Other activity fields

start_date (DateTime)
start_date_local (DateTime)
timezone (string)
private (bool)
workout_type (integer)

average_speed (float - m/s)
max_speed (float - m/s)
description (string)
