param([switch]$SmokeTest)
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
[xml]$props = Get-Content -Raw (Join-Path $projectRoot 'local.props')
$godotExe = [string]$props.Project.PropertyGroup.GodotExe
if (!(Test-Path -LiteralPath $godotExe)) { throw 'Configure GodotExe in local.props first.' }
$godotArguments = @('--path', $projectRoot)
if ($SmokeTest) { $godotArguments += '--headless' }
$godotArguments += 'res://tools/card_effect_manager/card_effect_manager.tscn'
if ($SmokeTest) { $godotArguments += @('--', '--effect-manager-smoke') }
& $godotExe @godotArguments
exit $LASTEXITCODE
