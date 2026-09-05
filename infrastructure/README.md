# Infrastructure Guide

## Quartz PostgreSQL

Background jobs require the dedicated `quartz-postgres` service in
[compose.yaml](compose.yaml). MongoDB stores application data; the separate
`keycloak-postgres` service stores identity data. Quartz stores job definitions,
pending triggers, and scheduler coordination data in its own PostgreSQL database.

Run these commands from the repository root:

```bash
docker compose -f infrastructure/compose.yaml up -d quartz-postgres
docker compose -f infrastructure/compose.yaml ps quartz-postgres
```

### Connection settings

| Setting | Compose default |
| --- | --- |
| Database / `QUARTZ_DB_NAME` | `quartz` |
| User / `QUARTZ_DB_USER` | `quartz` |
| Password / `QUARTZ_DB_PASSWORD` | `Planthor_Quartz_123` (local development only) |
| Published host port / `QUARTZ_DB_PORT` | `5433` |
| Container port | `5432` |

For an API running on your host, [appsettings.Development.json](../src/Api/appsettings.Development.json)
already includes:

```json
{
  "ConnectionStrings": {
    "Quartz": "Host=localhost;Port=5433;Database=quartz;Username=quartz;Password=Planthor_Quartz_123"
  }
}
```

The equivalent environment variable is `ConnectionStrings__Quartz`. Environment
variables override JSON settings, including when the supplied value is blank.
If you change Compose's `QUARTZ_DB_*` values, update the API configuration too;
Compose database settings do not automatically configure the API. Restart the API
after changing the connection string.

An API container on the same Compose `app_network` uses:

```text
Host=quartz-postgres;Port=5432;Database=quartz;Username=quartz;Password=Planthor_Quartz_123
```

The containerized pgAdmin also uses host `quartz-postgres` and port `5432`. A
host-side PostgreSQL client uses `localhost:5433`. Port `5432` on the host belongs
to Keycloak's PostgreSQL service with the default Compose configuration.

For deployed environments, provision a reachable PostgreSQL database and supply
`ConnectionStrings__Quartz` through the deployment's secret configuration with
the provider's required connection options. The base `appsettings.json` deliberately
leaves it empty. API startup rejects missing, empty, or whitespace values; it does
not fall back to an in-memory scheduler.

### Schema and volume lifecycle

The named volume `quartz_postgres_data` holds the database. Compose mounts
[quartz/tables_postgres.sql](quartz/tables_postgres.sql) as
`/docker-entrypoint-initdb.d/001-quartz.sql`, which initializes a fresh PostgreSQL
data directory. Restarting a container with an existing volume does not rerun this
initialization script. Quartz does not create these tables through EF Core migrations.

The checked-in script targets Quartz.NET 3.18.2, matching
[Directory.Packages.props](../Directory.Packages.props). It contains `DROP TABLE`
statements: use it only for a fresh, dedicated Quartz database. Do not rerun it
against a populated database to repair a connection issue. For an existing database
or a Quartz upgrade, back up its data and plan the required schema migration;
deleting the volume would discard scheduled work.

### Verify persistence

Start the API after PostgreSQL is healthy, then run these read-only checks from
the repository root (adjust the user/database if you changed the defaults):

```bash
docker compose -f infrastructure/compose.yaml exec -T quartz-postgres \
  psql -U quartz -d quartz \
  -c "SELECT sched_name, job_name, job_group, is_durable FROM qrtz_job_details;" \
  -c "SELECT sched_name, trigger_name, job_name, trigger_state FROM qrtz_triggers;" \
  -c "SELECT sched_name, instance_name, last_checkin_time FROM qrtz_scheduler_state;"
```

Startup registers the durable `DownloadAvatar` and `SyncIdentity` jobs. External
activity sync and revocation jobs are added when requested. With the API scheduler
running, the clustered store also writes scheduler check-ins. The startup log
reports `JobStoreTX` with `supports persistence: True`.

Completed one-shot triggers can disappear after execution. `qrtz_fired_triggers`
tracks executions in progress and is not a permanent history table. Use durable
job rows and pending delayed triggers to verify persistence; business results
remain in MongoDB or the configured external storage.

### Troubleshooting

- `ConnectionStrings:Quartz is required`: supply the configuration above and
  check for a blank `ConnectionStrings__Quartz` override in your launch environment.
- Connection refused or authentication failure: check container health, the
  host-versus-container address, published port, and the database's actual credentials.
- Missing `qrtz_*` tables: inspect the schema and volume lifecycle above. A healthy
  PostgreSQL container alone does not prove the Quartz schema has been initialized.
- Jobs execute but trigger tables are empty afterward: check durable job rows;
  completed triggers are not an execution audit log.

For scheduler implementation rules, see the [Infrastructure layer](../src/Infrastructure/README.md#background-job-persistence).
For isolated verification, see [API tests](../tests/ApiTests/README.md#quartz-persistence-tests).

## Export Keycloak full realm and users

- From `infrastructure` folder.
- Stop the main Keycloak container to prevent database locks:

```bash
docker stop keycloak
```

- Clear out the target export directory (optional, but prevents old files from causing conflicts):

```bash
rm -f keycloak/export/*
```

- Execute the Export Command using a temporary container (this ensures it has access to the same database network and environment variables):

```bash
docker compose run --rm -u root \
  -v $(pwd)/keycloak/export:/opt/keycloak/data/export \
  keycloak export --dir /opt/keycloak/data/export --users different_files
```

- Fix the ownership of the exported files (since the export was run as root):

```bash
docker run --rm -u root -v $(pwd)/keycloak/export:/export alpine chown -R 1000:1000 /export
```

- The exported JSON files will be available in the `keycloak/export/` directory on your host machine.
