# Local development

Use Podman for the local container environment.

Start the stack:

```powershell
podman machine start
$env:RAILWAY_BUCKET_NAME = "<bucket>"
$env:RAILWAY_BUCKET_ACCESS_KEY_ID = "<access-key>"
$env:RAILWAY_BUCKET_SECRET_ACCESS_KEY = "<secret-key>"
podman compose up --build
```

These values stay in the current shell and are forwarded only to the two API
containers. Do not commit them. When running an API directly with `dotnet run`,
set the equivalent project User Secrets instead:

```text
RailwayBucket:BucketName
RailwayBucket:AccessKeyId
RailwayBucket:SecretAccessKey
```

Stop it while retaining the local PostgreSQL volume:

```powershell
podman compose down
```

The current compose file owns the local PostgreSQL configuration. It is not a
production deployment definition. It starts `student-api` on port 8080 and
`trainer-api` on port 8081; both use the same PostgreSQL database. Only
`student-api` seeds the demo catalog, avoiding concurrent seed attempts. A
second startup is idempotent and keeps the same 231 exercises.

For a clean Railway database, deploy `StudentApi` first with
`DemoData__SeedOnStartup=true`. After its health check succeeds, deploy
`TrainerApi` with `DemoData__SeedOnStartup=false`. Both services require these
Variable References from the private bucket:

```text
RailwayBucket__BucketName
RailwayBucket__AccessKeyId
RailwayBucket__SecretAccessKey
```

This sequential first deploy avoids both hosts applying the initial migrations
at the same time. Later restarts remain safe; keep the seed enabled in only one
host.

For Expo on a physical device, copy `apps/mobile/.env.example`. The configured
public demo endpoints are `student-api-production-a4fe.up.railway.app` and
`trainer-api-production-b0f7.up.railway.app`. Override them with local LAN
addresses when testing the Podman stack from a device.

After a clean Railway deploy, verify both `/health` routes, then authenticate as
Trainer and query `/api/v1/training/exercises`. The expected response has 231
items: 231 referências remotas estáveis `media://exercise-catalog/delivery/v1/...`; nenhum desenho de exercício é empacotado no Expo
references with temporary HTTPS `imageUrl` values. Open at least one returned
URL and confirm a PNG response before testing the same exercise in preview,
Student execution and summary.
