$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$referenceFile = Join-Path $projectRoot 'work/current-compile.rsp'
$responseFile = Join-Path $projectRoot 'work/v06-compile.rsp'
$compileLines = @(Get-Content -LiteralPath $referenceFile | Where-Object { $_.StartsWith('-') -and -not $_.StartsWith('-out:') })
$compileLines += '-out:"' + (Join-Path $projectRoot 'work/V06CompileCheck.dll') + '"'
$compileLines += @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets') -Filter '*.cs' -Recurse | ForEach-Object { '"' + $_.FullName + '"' })
[System.IO.File]::WriteAllLines($responseFile, $compileLines)
& 'C:\Users\Life\Unity\Editors\6000.0.83f1\Editor\Data\NetCoreRuntime\dotnet.exe' 'C:\Users\Life\Unity\Editors\6000.0.83f1\Editor\Data\DotNetSdkRoslyn\csc.dll' ('@' + $responseFile)
exit $LASTEXITCODE
