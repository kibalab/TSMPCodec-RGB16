param(
    [Parameter(Mandatory = $true)][string]$UnityEditor,
    [Parameter(Mandatory = $true)][string]$Project,
    [Parameter(Mandatory = $true)][string]$CoreRepository,
    [Parameter(Mandatory = $true)][string]$Results,
    [Parameter(Mandatory = $true)][ValidateSet('Play', 'Build', 'Player', 'Udon')][string]$Step
)
$ErrorActionPreference = 'Stop'
$Project = (Resolve-Path -LiteralPath $Project).Path
$CoreRepository = (Resolve-Path -LiteralPath $CoreRepository).Path
$package = Get-ChildItem -LiteralPath (Join-Path (Split-Path -Parent $PSScriptRoot) 'Packages') -Directory | Where-Object Name -Like 'com.kibalab.tsmp.codec.*'
$manifest = Get-Content -LiteralPath (Join-Path $package.FullName 'package.json') -Raw | ConvertFrom-Json
$codec = $manifest.displayName.Replace('TSMP Codec ', '')
$editor = Join-Path $Project 'Assets/Validation/Editor'
New-Item -ItemType Directory -Path $editor,$Results -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Editor/CodecSupportValidation.cs') -Destination $editor -Force
if ($Step -in @('Play', 'Build')) {
    Copy-Item -LiteralPath (Join-Path $CoreRepository 'Validation~/Runtime') -Destination (Split-Path -Parent $editor) -Recurse -Force
    Copy-Item -LiteralPath (Join-Path $CoreRepository 'Validation~/Editor/UnitySupportValidation.cs') -Destination $editor -Force
}
$Results = (Resolve-Path -LiteralPath $Results).Path
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$log = Join-Path $Results "$stamp-$codec-$Step.log"
$result = Join-Path $Results "$stamp-$codec-$Step-result.txt"
$build = Join-Path $Project "Build/$codec/CodecValidation.exe"
$oldCodec = $env:TSMP_VALIDATION_CODEC
$oldResult = $env:TSMP_VALIDATION_RESULT
$oldBuild = $env:TSMP_VALIDATION_BUILD
try {
    $env:TSMP_VALIDATION_CODEC = $codec
    $env:TSMP_VALIDATION_RESULT = $result
    $env:TSMP_VALIDATION_BUILD = $build
    if ($Step -eq 'Player') {
        $process = Start-Process -FilePath $build -ArgumentList "-screen-fullscreen 0 -screen-width 640 -screen-height 360 -logFile `"$log`"" -WindowStyle Hidden -PassThru
    } else {
        New-Item -ItemType Directory -Path (Split-Path -Parent $build) -Force | Out-Null
        $arguments = "-batchmode -projectPath `"$Project`" -executeMethod CodecSupportValidation.$Step -logFile `"$log`""
        if ($Step -eq 'Build') { $arguments += ' -quit' }
        $process = Start-Process -FilePath $UnityEditor -ArgumentList $arguments -WindowStyle Hidden -PassThru
    }
    Write-Output "PID=$($process.Id) Log=$log Result=$result"
    if (!$process.WaitForExit(1200000)) { $process.Kill(); throw "Validation timed out: $log" }
    $process.Refresh()
    if ($process.ExitCode -ne 0) { throw "Validation exited $($process.ExitCode): $log" }
    if ($Step -eq 'Build') {
        $report = Get-Content -LiteralPath ([IO.Path]::ChangeExtension($build, '.build-report.txt'))
        if ($report[0] -ne 'Result=Succeeded') { throw 'Build did not succeed' }
        $report
    } else {
        if (!(Test-Path -LiteralPath $result) -or (Get-Content -LiteralPath $result -First 1) -ne 'PASS') { throw "No PASS result: $log" }
        Get-Content -LiteralPath $result
    }
} finally {
    $env:TSMP_VALIDATION_CODEC = $oldCodec
    $env:TSMP_VALIDATION_RESULT = $oldResult
    $env:TSMP_VALIDATION_BUILD = $oldBuild
}
