# Develop, stage and production

The three deployments use separate Compose project names, image tags, host ports and
runtime environment files. `ConnectionStrings__DefaultConnection` in each env file is
the SQL Server connection string for **that environment only**. Use three separate
databases; never point two environments at the same database. The backend applies EF
Core migrations when it starts.

| Environment | Env file | Backend port | Frontend port | Angular configuration |
| --- | --- | ---: | ---: | --- |
| develop | `deploy/develop.env` | 5182 | 3002 | `development` |
| stage | `deploy/stage.env` | 8081 | 3001 | `stage` |
| production | `deploy/production.env` | 8080 | 3000 | `production` |

## Configure

From the backend repository root, copy the examples and fill the connection strings,
JWT keys, VAPID keys and other empty values. Real `.env` files are ignored by Git.

```bash
cp deploy/develop.env.example deploy/develop.env
cp deploy/stage.env.example deploy/stage.env
cp deploy/production.env.example deploy/production.env
```

Assign three **different** values to `ConnectionStrings__DefaultConnection`. Keep
`ASPNETCORE_ENVIRONMENT` as `Development`, `Stage`, or `Production` respectively.
Stage and production refuse to start without an explicit connection string in the
process environment, so neither can fall back to a checked-in development database.

## Build and run

Build each backend image from this repository and each frontend image from the frontend
repository with the matching Angular configuration:

```bash
# In the backend repository:
docker build -t dental-backend:develop .
docker build -t dental-backend:stage .
docker build -t dental-backend:production .

# In the frontend repository (use your public VAPID key):
docker build --build-arg ANGULAR_CONFIGURATION=development --build-arg WEBPUSH_VAPID_PUBLIC_KEY="<public-key>" -t dental-frontend:develop .
docker build --build-arg ANGULAR_CONFIGURATION=stage --build-arg WEBPUSH_VAPID_PUBLIC_KEY="<public-key>" -t dental-frontend:stage .
docker build --build-arg ANGULAR_CONFIGURATION=production --build-arg WEBPUSH_VAPID_PUBLIC_KEY="<public-key>" -t dental-frontend:production .

# Back in the backend repository, each environment can run independently:
docker compose --env-file deploy/develop.env -f deploy/compose.yml up -d
docker compose --env-file deploy/stage.env -f deploy/compose.yml up -d
docker compose --env-file deploy/production.env -f deploy/compose.yml up -d
```

Verify a backup of each database before the first `up`: backend startup runs migrations
and background jobs. Run one backend replica per environment. To validate a Compose
configuration without starting it, use
`docker compose --env-file deploy/stage.env -f deploy/compose.yml config --quiet`.
The host ports bind to `127.0.0.1`; remote access requires a TLS reverse proxy.
Do not share `docker compose config` output because it can contain credentials.

The frontend API URL is compiled into its bundle. Development currently targets
`http://localhost:5182/api`, stage targets `https://api-stage.drsaeedmoghadam.com/api`,
and production targets `https://api.drsaeedmoghadam.com/api`. Set up the corresponding
API endpoint for each. For a remote develop frontend, set a reachable develop API URL
in its environment configuration before building; browser localhost refers to the
visitor's machine.

For direct .NET execution, set `ASPNETCORE_ENVIRONMENT` and
`ConnectionStrings__DefaultConnection` in the process environment and run
`dotnet run --project DentalDashboard --no-launch-profile`. Local development may
also use the existing `appsettings.Development.json` SQL Server connection.
