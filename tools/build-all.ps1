[CmdletBinding()]
param(
    [ValidateRange(30, 7200)]
    [int]$TimeoutSeconds = 1800
)

$ErrorActionPreference = 'Stop'
$projectDirectory = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$workDirectory = Join-Path $projectDirectory 'work'
$requestPath = Join-Path $workDirectory 'unity-request.json'
$receiptDirectory = Join-Path $workDirectory 'unity-requests'
[System.IO.Directory]::CreateDirectory($workDirectory) | Out-Null

function Get-RequestHash([string]$Value) {
    $hashAlgorithm = [System.Security.Cryptography.SHA256]::Create()
    try {
        return [System.BitConverter]::ToString($hashAlgorithm.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($Value))).Replace('-', '').ToLowerInvariant()
    }
    finally { $hashAlgorithm.Dispose() }
}

function Invoke-UnityCommand {
    param([ValidateSet('compile', 'build-windows', 'build-web')][string]$Command)
    $requestId = [guid]::NewGuid().ToString('N')
    $temporaryPath = Join-Path $workDirectory ('unity-request.' + $requestId + '.tmp')
    $requestJson = @{ id = $requestId; command = $Command } | ConvertTo-Json -Compress
    [System.IO.File]::WriteAllText($temporaryPath, $requestJson, [System.Text.UTF8Encoding]::new($false))
    if ([System.IO.File]::Exists($requestPath)) { [System.IO.File]::Replace($temporaryPath, $requestPath, [NullString]::Value) }
    else { [System.IO.File]::Move($temporaryPath, $requestPath) }

    $receiptPath = Join-Path $receiptDirectory ((Get-RequestHash $requestId) + '.json')
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    Write-Host ('Unity: ' + $Command + ' [' + $requestId + ']')
    while ([DateTime]::UtcNow -lt $deadline) {
        if ([System.IO.File]::Exists($receiptPath)) {
            $receipt = [System.IO.File]::ReadAllText($receiptPath) | ConvertFrom-Json
            if ($receipt.id -ne $requestId) { throw 'Unity receipt id does not match the request.' }
            if ($receipt.status -eq 'failed') {
                throw ('Unity ' + $Command + ' failed: ' + ($receipt.errors -join [Environment]::NewLine))
            }
            if ($receipt.status -eq 'succeeded' -or ($Command -eq 'compile' -and $receipt.status -eq 'accepted')) {
                Write-Host ('Unity: ' + $Command + ' ' + $receipt.status)
                return $receipt
            }
        }
        Start-Sleep -Milliseconds 500
    }
    throw ('Timed out waiting for Unity ' + $Command + '. Open this project in Unity Editor and leave Play Mode. The request was not retried.')
}

# A separate request lets the editor finish deferred compilation and release Bee files.
# Compile acceptance is not completion: the bridge queues the next build until Unity is idle.
$mutexName = 'Local\SkeletonDefenderBuild-' + (Get-RequestHash $projectDirectory)
$buildMutex = [System.Threading.Mutex]::new($false, $mutexName)
$ownsMutex = $false
try {
    try { $ownsMutex = $buildMutex.WaitOne(0) }
    catch [System.Threading.AbandonedMutexException] { $ownsMutex = $true }
    if (-not $ownsMutex) { throw 'Another build-all script is already running for this project.' }
    Invoke-UnityCommand 'compile' | Out-Null
    Invoke-UnityCommand 'build-windows' | Out-Null
    Invoke-UnityCommand 'compile' | Out-Null
    Invoke-UnityCommand 'build-web' | Out-Null
    Write-Host 'Windows and Web builds completed successfully.'
}
finally {
    if ($ownsMutex) { $buildMutex.ReleaseMutex() }
    $buildMutex.Dispose()
}
