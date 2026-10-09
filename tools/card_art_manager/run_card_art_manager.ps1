param(
    [switch]$SmokeTest
)

$ErrorActionPreference = 'Stop'

$projectRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$localPropsPath = Join-Path $projectRoot 'local.props'

if (-not (Test-Path -LiteralPath $localPropsPath)) {
    throw "Missing local.props. Copy local.props.template and configure GodotExe first."
}

[xml]$localProps = Get-Content -Raw -LiteralPath $localPropsPath
$godotExe = [string]$localProps.Project.PropertyGroup.GodotExe
if ([string]::IsNullOrWhiteSpace($godotExe) -or -not (Test-Path -LiteralPath $godotExe)) {
    throw "GodotExe in local.props does not point to an existing executable."
}

$godotArguments = @()
if ($SmokeTest) {
	$godotArguments += '--headless'
}
$godotArguments += @('--path', [string]$projectRoot, 'res://tools/card_art_manager/card_art_manager.tscn')
if ($SmokeTest) {
	$godotArguments += @('--', '--card-art-smoke-test')
}

& $godotExe @godotArguments
exit $LASTEXITCODE
