param([string]$IlspyExe, [switch]$UseExistingSource)
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
[xml]$props = Get-Content -Raw (Join-Path $projectRoot 'local.props')
$gameDir = [string]$props.Project.PropertyGroup.Sts2Dir
$assemblyPath = Join-Path $gameDir 'data_sts2_windows_x86_64/sts2.dll'
$sourceDir = Join-Path $projectRoot 'tmp/card-effects-decompiled'
$assemblyHash = (Get-FileHash -Algorithm SHA256 $assemblyPath).Hash.ToLowerInvariant()
if (!$UseExistingSource) {
    if (!$IlspyExe) { $IlspyExe = (Get-Command ilspycmd -ErrorAction SilentlyContinue).Source }
    if (!$IlspyExe) { throw 'Pass -IlspyExe pointing to ilspycmd.exe.' }
    # A fingerprinted fresh folder prevents removed classes surviving a game update.
    $sourceDir = Join-Path $projectRoot "tmp/card-effects-$assemblyHash"
    $env:DOTNET_ROLL_FORWARD = 'Major'
    & $IlspyExe -p -o $sourceDir --disable-updatecheck $assemblyPath
    if ($LASTEXITCODE -ne 0) { throw 'ILSpy export failed.' }
    [System.IO.File]::WriteAllText((Join-Path $sourceDir 'catalog-source.sha256'), $assemblyHash)
}
& node (Join-Path $PSScriptRoot 'build_catalog.mjs') --source $sourceDir --game $gameDir
if ($LASTEXITCODE -ne 0) { throw 'Catalog generation failed.' }
