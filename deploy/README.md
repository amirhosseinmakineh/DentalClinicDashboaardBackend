# Environment deployment files

The same `compose.yml` is used for `develop`, `stage`, and `production`. Each environment has a
different project name, loopback-only host ports, image tags, and runtime environment file.

Copy only the required example on the server and fill its secrets outside Git:

```bash
cp deploy/production.env.example deploy/production.env
docker compose --env-file deploy/production.env -f deploy/compose.yml config
```

`config` only validates the rendered Compose model; it does not start containers.

Do not run `docker compose up` for the backend until the database backup is verified. Backend
startup applies EF Core migrations and starts lead/notification workers. Production must run
exactly one backend replica.

Host ports are bound to `127.0.0.1` and must be exposed only through the reverse proxy with TLS.
