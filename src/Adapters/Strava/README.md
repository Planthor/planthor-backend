# Strava rate limit

Once your app is configured in your API dashboard, you can upgrade your access directly from your API Settings Dashboard. This raises your access to:

Athlete capacity of 10
Read read limits: 200 requests / 15min & 2,000 requests / day
Overall rate limits: 400 requests / 15min & 4,000 requests / day

Once your app reaches 10 connected athletes and you’re ready to scale further, please submit your app for review!

## Activities

id (long)
external_id (string)
upload_id (long)
name (string)
distance (float - in meters)
moving_time (integer - in seconds)
elapsed_time (integer - in seconds)

type (deprecated - suggest ignore):

- Ride

sport_type:

...
- Badminton
- Basketball
- ...
- EBikeRide
- Elliptical
- EMountainBikeRide
- GravelRide
- Handcycle
- HighIntensityIntervalTraining
- Hike
...
- MountainBikeRide
- Ride
- Run
- ...
- StairStepper
- Swim
...
- TrailRun
- Velomobile
- VirtualRide
- VirtualRun
- Walk
- Wheelchair

start_date (DateTime)
start_date_local (DateTime)
timezone (string)
private (bool)
workout_type (integer)

average_speed (float - m/s)
max_speed (float - m/s)
description (string)
