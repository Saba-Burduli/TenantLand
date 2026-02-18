$ErrorActionPreference = "Stop"

$root = "C:\Users\User\PostyLand"
$log = Join-Path $root "api-run.log"
$err = Join-Path $root "api-run.err.log"
$pidFile = Join-Path $root "api.pid"

if (Test-Path $log) { Remove-Item $log -Force }
if (Test-Path $err) { Remove-Item $err -Force }

$process = Start-Process -FilePath "dotnet" `
    -ArgumentList "run --project src/PostyLand.API/PostyLand.API.csproj --no-build" `
    -WorkingDirectory $root `
    -RedirectStandardOutput $log `
    -RedirectStandardError $err `
    -PassThru

Set-Content -Path $pidFile -Value $process.Id
Write-Output $process.Id
