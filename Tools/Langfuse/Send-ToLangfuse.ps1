<#
.SYNOPSIS
    Sends langfuse_batch.json (made by usage_report.py) to Langfuse through the public ingestion API.

.DESCRIPTION
    Reads LANGFUSE_PUBLIC_KEY, LANGFUSE_SECRET_KEY and LANGFUSE_HOST from the .env file at the project
    root (see .env.example). The batch is split into chunks so each request stays well under the API's
    size limit. Event ids are deterministic per trace/phase/reply, so re-sending updates the same trace.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\Langfuse\Send-ToLangfuse.ps1
#>
param(
    [string]$BatchFile = (Join-Path $PSScriptRoot 'langfuse_batch.json'),
    [string]$EnvFile = (Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) '.env'),
    [int]$ChunkSize = 200
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $EnvFile)) { throw "No .env at $EnvFile. Copy .env.example to .env and fill in the Langfuse keys." }
$cfg = @{}
foreach ($line in Get-Content $EnvFile) {
    if ($line -match '^\s*([A-Z_]+)\s*=\s*(.*)\s*$') { $cfg[$Matches[1]] = $Matches[2].Trim('"', "'") }
}
foreach ($k in 'LANGFUSE_PUBLIC_KEY', 'LANGFUSE_SECRET_KEY') {
    if (-not $cfg[$k]) { throw "$k is missing in $EnvFile" }
}
$hostUrl = if ($cfg['LANGFUSE_HOST']) { $cfg['LANGFUSE_HOST'].TrimEnd('/') } else { 'https://cloud.langfuse.com' }

$pair = "$($cfg['LANGFUSE_PUBLIC_KEY']):$($cfg['LANGFUSE_SECRET_KEY'])"
$auth = 'Basic ' + [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($pair))
$events = (Get-Content $BatchFile -Raw -Encoding UTF8 | ConvertFrom-Json).batch

$sent = 0; $failed = 0
for ($i = 0; $i -lt $events.Count; $i += $ChunkSize) {
    $chunk = $events[$i..([Math]::Min($i + $ChunkSize, $events.Count) - 1)]
    $body = @{ batch = $chunk } | ConvertTo-Json -Depth 20 -Compress
    $resp = Invoke-RestMethod -Method Post -Uri "$hostUrl/api/public/ingestion" `
        -Headers @{ Authorization = $auth } -ContentType 'application/json; charset=utf-8' `
        -Body ([Text.Encoding]::UTF8.GetBytes($body))
    $sent += @($resp.successes).Count
    $failed += @($resp.errors).Count
    foreach ($e in @($resp.errors)) { Write-Warning "event $($e.id): $($e.status) $($e.message)" }
}
Write-Host "Langfuse ($hostUrl): $sent events accepted, $failed rejected."
if ($failed -gt 0) { exit 1 }
