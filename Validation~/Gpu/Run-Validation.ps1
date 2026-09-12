param(
    [Parameter(Mandatory = $true)][string]$UnityPath,
    [Parameter(Mandatory = $true)][string]$ProjectPath,
    [Parameter(Mandatory = $true)][string]$ResultsDirectory,
    [ValidateSet('G02', 'G03')][string]$Mode = 'G02',
    [ValidateSet('RGB16', 'RGB20')][string]$Codec = 'RGB16'
)

$ErrorActionPreference = 'Stop'
$project = (Resolve-Path -LiteralPath $ProjectPath).Path
$unity = (Resolve-Path -LiteralPath $UnityPath).Path
if (-not (Test-Path -LiteralPath (Join-Path $project 'ProjectSettings/ProjectVersion.txt'))) {
    throw 'ProjectPath must be a dedicated Unity validation project.'
}

$scripts = Join-Path $project 'Assets/GpuShaderValidation'
[System.IO.Directory]::CreateDirectory($scripts) | Out-Null
[System.IO.Directory]::CreateDirectory($ResultsDirectory) | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'GpuShaderValidation.cs') -Destination $scripts
$results = (Resolve-Path -LiteralPath $ResultsDirectory).Path
$name = "$($Mode.ToLowerInvariant())-$($Codec.ToLowerInvariant())"
$log = Join-Path $results "$name.log"
$env:TSMP_GPU_MODE = $Mode
$env:TSMP_GPU_CODEC = $Codec
$baselineFolder = if ($Mode -eq 'G02') { 'Baselines' } else { 'SymbolBaselines' }
$env:TSMP_GPU_BASELINES = Join-Path $PSScriptRoot $baselineFolder
$env:TSMP_GPU_RESULTS = Join-Path $results "$name.txt"
$arguments = "-batchmode -force-d3d11 -projectPath `"$project`" -executeMethod GpuShaderValidation.Run -logFile `"$log`""
$process = Start-Process -FilePath $unity -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
if ($process.ExitCode -ne 0) {
    throw "Unity validation failed with exit code $($process.ExitCode). See $log"
}
if ((Get-Content -LiteralPath $env:TSMP_GPU_RESULTS -TotalCount 1) -ne 'PASS') {
    throw "GPU validation did not pass. See $env:TSMP_GPU_RESULTS"
}
Get-Content -LiteralPath $env:TSMP_GPU_RESULTS
