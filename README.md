













# PostyLand

Core multi-tenant SaaS foundation for a marketing management platform.


## How to start

### 1. Prerequisites

- .NET SDK 10.x
- Docker Desktop (or Docker Engine with Compose)
- PostgreSQL client tools (optional, for manual checks)

### 2. Start PostgreSQL

```powershell
docker compose up -d

```

This starts PostgreSQL on `localhost:5432` with:
- user: `postgres`
- password: `postgres`

### 3. Restore and build

```powershell
dotnet restore
dotnet build PostyLand.sln
```

### 4. Apply MainDB migrations

```powershell
dotnet ef database update --context MainDbContext --project src/PostyLand.Persistence --startup-project src/PostyLand.API
```

If `dotnet ef` is missing:

```powershell
dotnet tool install --global dotnet-ef
```

### 5. Run the API

```powershell
dotnet run --project src/PostyLand.API/PostyLand.API.csproj
```

Default development URL is:
- `http://localhost:5166`

Optional background start script:

```powershell
.\scripts\start-api.ps1
```

### 6. Health check

```powershell
Invoke-RestMethod -Method Get -Uri "http://localhost:5166/health"
```

### 7. Register a tenant (onboarding + Hangfire job)

```powershell
$body = @{
  name = "Acme Inc"
  subdomain = "acme"
  adminEmail = "owner@acme.com"
  adminPassword = "StrongPass123!"
  plan = "Pro"
  employeeLimit = 25
  renewalDate = "2030-01-01T00:00:00Z"
} | ConvertTo-Json

$register = Invoke-RestMethod -Method Post -Uri "http://localhost:5166/api/tenants/register" -ContentType "application/json" -Body $body
$register
```

Expected response includes:
- `tenantId`
- `onboardingJobId`
- `status`

### 8. Call a tenant-scoped endpoint with JWT

Generate token:

```powershell
$tenantId = $register.tenantId
$userId = [guid]::NewGuid().ToString()
$token = .\scripts\gen-jwt.ps1 -TenantId $tenantId -UserId $userId -Role "Owner" -Scope "tenant.api"
```

Call tenant diagnostics endpoint:

```powershell
Invoke-RestMethod -Method Get -Uri "http://localhost:5166/api/tenant/ping" -Headers @{
  Authorization = "Bearer $token"
  "X-Tenant-Subdomain" = "acme"
}
```

### 9. Run integration tests

```powershell
dotnet test PostyLand.sln
```

## Useful endpoints

- Health: `GET /health`
- Tenant registration: `POST /api/tenants/register`
- Tenant diagnostic: `GET /api/tenant/ping` (JWT + tenant required)
- Hangfire dashboard: `/hangfire` (platform admin policy)
- Tenant migration endpoint: `POST /api/admin/tenants/{tenantId}/migrations/run` (platform admin policy)

## Notes

- Tenant resolution for local development should use header `X-Tenant-Subdomain`.
- External provisioning (Route53/S3) is disabled in development by default (`Provisioning:DisableExternalProvisioning=true`).
