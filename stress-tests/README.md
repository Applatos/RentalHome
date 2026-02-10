# Stress Tests

Load testing suite for the Sommerhus API using [k6](https://k6.io/).

## Prerequisites

1. **k6** installed: `winget install k6` or download from https://k6.io/docs/get-started/installation/
2. **API running** with stress data seeded

## Seed Stress Data

Start the API, then seed data via the admin endpoint:

```powershell
# Start API
dotnet run --project Sommerhus.Api

# Login and seed (PowerShell)
$login = Invoke-RestMethod -Uri "http://localhost:5183/api/admin/auth/login" `
  -Method POST -ContentType "application/json" `
  -Body '{"username":"admin","password":"Sommerhus123!"}'

$headers = @{ Authorization = "Bearer $($login.token)" }

# Seed 500 houses (default)
Invoke-RestMethod -Uri "http://localhost:5183/api/admin/stress/seed" `
  -Method POST -Headers $headers

# Or seed 5000 houses for full stress test
Invoke-RestMethod -Uri "http://localhost:5183/api/admin/stress/seed?houses=5000" `
  -Method POST -Headers $headers
```

## Run Tests

```powershell
# Public search (ramp to 100 VUs)
k6 run stress-tests/k6/public-search.js

# House detail + availability
k6 run stress-tests/k6/house-detail.js

# Concurrent browsing (100 VUs for 60s)
k6 run stress-tests/k6/concurrent-browse.js

# Admin CRUD operations
k6 run stress-tests/k6/admin-crud.js
```

### Custom API URL

```powershell
k6 run -e BASE_URL=http://localhost:5183 stress-tests/k6/public-search.js
```

## Save Results

```powershell
# JSON output for comparison
k6 run --out json=stress-tests/results/baseline-$(Get-Date -Format yyyy-MM-dd).json stress-tests/k6/public-search.js
```

## Test Scenarios

| Script              | Description                              | Target p95 |
| ------------------- | ---------------------------------------- | ---------- |
| `public-search.js`  | Search with terms, ramp to 100 VUs       | < 200ms    |
| `house-detail.js`   | Detail page + availability, ramp to 50   | < 300ms    |
| `concurrent-browse.js` | 100 VUs browsing for 60s              | < 500ms    |
| `admin-crud.js`     | Admin list operations, ramp to 20 VUs    | < 500ms    |

## Benchmark Protocol

1. **Baseline** (before Phase 20): Seed 5,000 houses → run all scenarios → save to `results/`
2. **After Phase 20**: Same data → same scenarios → compare metrics
3. **Report**: Document response time improvement, throughput change

## Clear Stress Data

```powershell
Invoke-RestMethod -Uri "http://localhost:5183/api/admin/stress/clear" `
  -Method DELETE -Headers $headers
```
