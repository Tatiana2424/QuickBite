[CmdletBinding()]
param()

$ports = 3000, 5001, 5002, 5003, 5004, 5005, 8080

$processIds = Get-NetTCPConnection -State Listen -LocalPort $ports -ErrorAction SilentlyContinue |
    Select-Object -ExpandProperty OwningProcess -Unique

if (-not $processIds) {
    Write-Host "No QuickBite local processes were found on the expected ports."
    exit 0
}

$processes = $processIds |
    ForEach-Object { Get-Process -Id $_ -ErrorAction SilentlyContinue } |
    Where-Object { $_ -and ($_.ProcessName -in @("dotnet", "node") -or $_.ProcessName -like "QuickBite.*") }

if (-not $processes) {
    Write-Host "No QuickBite dotnet/node processes were found on the expected ports."
    exit 0
}

$processes | Stop-Process -Force

Write-Host "Stopped QuickBite local processes."
