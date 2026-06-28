# Infrastructure Guide

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
