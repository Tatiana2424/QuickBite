[CmdletBinding()]
param(
    [switch]$SkipFrontend
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$logDir = Join-Path $env:TEMP "quickbite-runlogs"

New-Item -ItemType Directory -Force -Path $logDir | Out-Null

sqllocaldb start MSSQLLocalDB | Out-Null

$services = @(
    @{
        Name = "identity"
        Project = "src/Services/Identity/QuickBite.Identity.Api"
        Connection = "Server=(localdb)\MSSQLLocalDB;Database=QuickBiteIdentityDb;Integrated Security=true;TrustServerCertificate=True"
    },
    @{
        Name = "catalog"
        Project = "src/Services/Catalog/QuickBite.Catalog.Api"
        Connection = "Server=(localdb)\MSSQLLocalDB;Database=QuickBiteCatalogDb;Integrated Security=true;TrustServerCertificate=True"
    },
    @{
        Name = "orders"
        Project = "src/Services/Orders/QuickBite.Orders.Api"
        Connection = "Server=(localdb)\MSSQLLocalDB;Database=QuickBiteOrdersDb;Integrated Security=true;TrustServerCertificate=True"
    },
    @{
        Name = "payments"
        Project = "src/Services/Payments/QuickBite.Payments.Api"
        Connection = "Server=(localdb)\MSSQLLocalDB;Database=QuickBitePaymentsDb;Integrated Security=true;TrustServerCertificate=True"
    },
    @{
        Name = "delivery"
        Project = "src/Services/Delivery/QuickBite.Delivery.Api"
        Connection = "Server=(localdb)\MSSQLLocalDB;Database=QuickBiteDeliveryDb;Integrated Security=true;TrustServerCertificate=True"
    }
)

foreach ($service in $services) {
    $stdout = Join-Path $logDir "$($service.Name).out.log"
    $stderr = Join-Path $logDir "$($service.Name).err.log"
    $command = 'cd /d "' + $repoRoot + '" && set ConnectionStrings__DefaultConnection=' + $service.Connection + ' && dotnet run --project "' + $service.Project + '" --no-build'

    Start-Process -FilePath "cmd.exe" `
        -ArgumentList "/c", $command `
        -RedirectStandardOutput $stdout `
        -RedirectStandardError $stderr `
        -WindowStyle Hidden
}

$gatewayCommand = 'cd /d "' + $repoRoot + '" && dotnet run --project "src/Gateway/QuickBite.Gateway" --no-build'
Start-Process -FilePath "cmd.exe" `
    -ArgumentList "/c", $gatewayCommand `
    -RedirectStandardOutput (Join-Path $logDir "gateway.out.log") `
    -RedirectStandardError (Join-Path $logDir "gateway.err.log") `
    -WindowStyle Hidden

if (-not $SkipFrontend) {
    $frontendCommand = 'cd /d "' + (Join-Path $repoRoot "frontend\quickbite-web") + '" && npm.cmd run dev -- --host 127.0.0.1 --port 3000'
    Start-Process -FilePath "cmd.exe" `
        -ArgumentList "/c", $frontendCommand `
        -RedirectStandardOutput (Join-Path $logDir "frontend.out.log") `
        -RedirectStandardError (Join-Path $logDir "frontend.err.log") `
        -WindowStyle Hidden
}

Write-Host "QuickBite local host mode started."
Write-Host "Frontend: http://localhost:3000"
Write-Host "Gateway:  http://localhost:8080"
Write-Host "Logs:     $logDir"
