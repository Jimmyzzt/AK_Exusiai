param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('public', 'public-beta')]
    [string]$Branch,

    [string]$GameDataDir
)

$ErrorActionPreference = 'Stop'
$projectDir = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$project = Join-Path $projectDir 'AK_Exusiai.csproj'

if (-not $GameDataDir) {
    [xml]$local = Get-Content -LiteralPath (Join-Path $projectDir 'local.props')
    $GameDataDir = Join-Path ([string]$local.Project.PropertyGroup.Sts2Dir) 'data_sts2_windows_x86_64'
}
$GameDataDir = (Resolve-Path -LiteralPath $GameDataDir).Path
$releaseInfoPath = Join-Path $GameDataDir 'release_info.json'
if (-not (Test-Path -LiteralPath $releaseInfoPath)) {
    $releaseInfoPath = Join-Path (Split-Path -Parent $GameDataDir) 'release_info.json'
}
if (-not (Test-Path -LiteralPath $releaseInfoPath)) {
    throw "Missing release_info.json for $GameDataDir. Provide a game installation or a snapshot with this file."
}

$gameVersion = ([string]((Get-Content -LiteralPath $releaseInfoPath -Raw | ConvertFrom-Json).version)).TrimStart('v')
$api = if ($Branch -eq 'public') { '0.111.0' } else { '0.113.0' }
$versionPrefix = if ($Branch -eq 'public') { '0.111.' } else { '0.113.' }
if (-not $gameVersion.StartsWith($versionPrefix, [StringComparison]::Ordinal)) {
    throw "The $Branch build needs game $versionPrefix*, but $releaseInfoPath reports $gameVersion."
}

$outputDir = Join-Path $projectDir "dist\workshop\$Branch"
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
$pckPath = Join-Path $outputDir 'AK_Exusiai.pck'

Push-Location $projectDir
try {
    & dotnet build $project `
        "/p:GameApiTarget=$api" `
        '/p:VariantBuild=true' `
        "/p:Sts2DataDir=$GameDataDir" `
        "/p:ModOutputDir=$outputDir" `
        "/p:ModPckPath=$pckPath" `
        '/p:CopyModOnBuild=true' `
        '/p:RunPckExport=true'
    if ($LASTEXITCODE -ne 0) { throw "Build failed for $Branch." }
}
finally {
    Pop-Location
}

$manifestPath = Join-Path $outputDir 'AK_Exusiai.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$manifest.min_game_version = if ($Branch -eq 'public') { '0.111.1' } else { '0.113.0' }
$manifest | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $manifestPath -Encoding utf8

$expected = @('AK_Exusiai.dll', 'AK_Exusiai.json', 'AK_Exusiai.pck')
$actual = @(Get-ChildItem -LiteralPath $outputDir -File | Select-Object -ExpandProperty Name)
$extra = @($actual | Where-Object { $_ -notin $expected })
$missing = @($expected | Where-Object { $_ -notin $actual })
if ($extra.Count -or $missing.Count) {
    throw "Unexpected release directory contents. Missing: $($missing -join ', '); extra: $($extra -join ', ')"
}

Write-Host "Ready: $outputDir (game $gameVersion, API $api)"
Get-ChildItem -LiteralPath $outputDir -File | Get-FileHash -Algorithm SHA256 | Format-Table Path, Hash -AutoSize
