param(
    [string]$Configuration = "Release",
    [string]$OutputDirectory = "artifacts/migrations"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Push-Location $repoRoot

try {
    dotnet tool restore

    $services = @(
        @{ Name = "identity"; Context = "IdentityDbContext"; Project = "src/Services/Identity/QuickBite.Identity.Infrastructure/QuickBite.Identity.Infrastructure.csproj" },
        @{ Name = "catalog"; Context = "CatalogDbContext"; Project = "src/Services/Catalog/QuickBite.Catalog.Infrastructure/QuickBite.Catalog.Infrastructure.csproj" },
        @{ Name = "orders"; Context = "OrdersDbContext"; Project = "src/Services/Orders/QuickBite.Orders.Infrastructure/QuickBite.Orders.Infrastructure.csproj" },
        @{ Name = "payments"; Context = "PaymentsDbContext"; Project = "src/Services/Payments/QuickBite.Payments.Infrastructure/QuickBite.Payments.Infrastructure.csproj" },
        @{ Name = "delivery"; Context = "DeliveryDbContext"; Project = "src/Services/Delivery/QuickBite.Delivery.Infrastructure/QuickBite.Delivery.Infrastructure.csproj" }
    )

    New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

    foreach ($service in $services) {
        $outputPath = Join-Path $OutputDirectory "quickbite-$($service.Name)-migrate"
        Write-Host "Building migration bundle for $($service.Name)..."
        dotnet ef migrations bundle `
            --project $service.Project `
            --context $service.Context `
            --configuration $Configuration `
            --output $outputPath

        if ($LASTEXITCODE -ne 0) {
            throw "Migration bundle generation failed for $($service.Name)."
        }
    }
}
finally {
    Pop-Location
}
