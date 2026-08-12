# Local development

Use Podman for the local container environment.

Start the stack:

```powershell
podman machine start
podman compose up --build
```

Stop it while retaining the local PostgreSQL volume:

```powershell
podman compose down
```

The current compose file owns the local PostgreSQL configuration. It is not a
production deployment definition. It starts `student-api` on port 8080 and
`trainer-api` on port 8081; both use the same PostgreSQL database.
