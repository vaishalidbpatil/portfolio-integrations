Write-Host "=== Portfolio Integrations Setup ===" -ForegroundColor Cyan

# Step 1 - Tear down
Write-Host "`nTearing down existing containers..." -ForegroundColor Yellow
docker compose down --remove-orphans
if ($LASTEXITCODE -ne 0) {
    Write-Host "docker compose down failed." -ForegroundColor Red
    exit 1
}

# Step 2 - Check portfolio-service is reachable (MCP server proxies the Orders API)
Write-Host "`nChecking portfolio-service is up (API on port 8080)..." -ForegroundColor Yellow
$retries = 5
$apiReady = $false
do {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:8080/health/live" -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
        if ($response.StatusCode -eq 200) { $apiReady = $true }
    } catch {}
    if (-not $apiReady) { Start-Sleep -Seconds 2 }
    $retries--
} while (-not $apiReady -and $retries -gt 0)

if (-not $apiReady) {
    Write-Host "WARNING: portfolio-service API is not reachable on port 8080." -ForegroundColor Yellow
    Write-Host "         Run setup.ps1 from the portfolio-service repo first." -ForegroundColor Yellow
    Write-Host "         Continuing anyway — MCP server will still start." -ForegroundColor DarkGray
} else {
    Write-Host "portfolio-service API is up." -ForegroundColor Green
}

# Step 3 - Build and start MCP server
Write-Host "`nBuilding and starting MCP Server..." -ForegroundColor Yellow
docker compose up -d --build
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to start MCP server." -ForegroundColor Red
    docker compose logs
    exit 1
}

# Step 4 - Wait for health check
Write-Host "`nWaiting for MCP server to be ready..." -ForegroundColor Yellow
$retries = 20
do {
    Start-Sleep -Seconds 2
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5002/health/live" -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
        if ($response.StatusCode -eq 200) { $retries = 0 }
    } catch { $retries-- }
} while ($retries -gt 0)

docker compose logs --tail 15

Write-Host "`nSetup complete." -ForegroundColor Cyan
Write-Host "  MCP Server  : http://localhost:5002/health/live" -ForegroundColor Green
Write-Host "  Tools       : http://localhost:5002/tools/weather-forecast" -ForegroundColor Green
Write-Host "  Orders API  : http://localhost:8080/swagger" -ForegroundColor Green
