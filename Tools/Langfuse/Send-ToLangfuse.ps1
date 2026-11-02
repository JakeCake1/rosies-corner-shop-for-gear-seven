<#
.SYNOPSIS
    Sends the agent usage report (otel/batch_*.json, made by usage_report.py) to Langfuse over OpenTelemetry.

.DESCRIPTION
    Langfuse ingests traces through its OTLP/HTTP endpoint /api/public/otel/v1/traces (ingestion v4); the legacy
    /api/public/ingestion endpoint no longer accepts traces. Reads LANGFUSE_PUBLIC_KEY, LANGFUSE_SECRET_KEY and
    LANGFUSE_HOST from the .env file at the project root. Span ids are deterministic, so
    sending again updates the same trace.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\Langfuse\Send-ToLangfuse.ps1
#>
param(
    [string]$BatchDir = (Join-Path $PSScriptRoot 'otel'),
    [string]$EnvFile = (Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) '.env')
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $EnvFile)) { throw "No .env at $EnvFile. Create it with NAME=value lines for the Langfuse keys." }
$cfg = @{}
foreach ($line in Get-Content $EnvFile) {
    if ($line -match '^\s*([A-Z_]+)\s*=\s*(.*?)\s*$') { $cfg[$Matches[1]] = $Matches[2].Trim('"', "'") }
}
foreach ($k in 'LANGFUSE_PUBLIC_KEY', 'LANGFUSE_SECRET_KEY') {
    if (-not $cfg[$k]) { throw "$k is missing in $EnvFile" }
}
$hostUrl = if ($cfg['LANGFUSE_HOST']) { $cfg['LANGFUSE_HOST'].TrimEnd('/') } else { 'https://cloud.langfuse.com' }
$uri = "$hostUrl/api/public/otel/v1/traces"

$pair = "$($cfg['LANGFUSE_PUBLIC_KEY']):$($cfg['LANGFUSE_SECRET_KEY'])"
$headers = @{
    Authorization                  = 'Basic ' + [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($pair))
    'x-langfuse-ingestion-version' = '4'
}

$files = @(Get-ChildItem -Path $BatchDir -Filter 'batch_*.json' | Sort-Object Name)
if ($files.Count -eq 0) { throw "No batch_*.json in $BatchDir. Run: python3 Tools/Langfuse/usage_report.py report" }

$spans = 0; $rejected = 0
foreach ($file in $files) {
    $bytes = [IO.File]::ReadAllBytes($file.FullName)
    try {
        $resp = Invoke-RestMethod -Method Post -Uri $uri -Headers $headers `
            -ContentType 'application/json' -Body $bytes
    }
    catch {
        $detail = $_.ErrorDetails.Message
        throw "$($file.Name): $($_.Exception.Message) $detail"
    }
    $count = ([Text.Encoding]::UTF8.GetString($bytes) | Select-String -Pattern '"spanId"' -AllMatches).Matches.Count
    $spans += $count
    if ($resp -and $resp.partialSuccess -and $resp.partialSuccess.rejectedSpans) {
        $rejected += [int]$resp.partialSuccess.rejectedSpans
        Write-Warning "$($file.Name): $($resp.partialSuccess.errorMessage)"
    }
    Write-Host "$($file.Name): $count spans sent"
}
Write-Host "Langfuse ($uri): $($spans - $rejected) spans accepted, $rejected rejected."
if ($rejected -gt 0) { exit 1 }
