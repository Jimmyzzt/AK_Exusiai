param([switch]$SmokeTest, [switch]$ContractTest, [string]$BridgeDirectory)
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
[xml]$props = Get-Content -Raw (Join-Path $projectRoot 'local.props')
$godotExe = [string]$props.Project.PropertyGroup.GodotExe
if (!(Test-Path -LiteralPath $godotExe)) { throw 'Configure GodotExe in local.props first.' }
$godotArguments = @('--path', $projectRoot)
if ($SmokeTest -or $ContractTest) { $godotArguments += @('--headless', '--quit-after', '300') }
$godotArguments += 'res://tools/card_effect_manager/card_effect_manager.tscn'
if ($SmokeTest -or $ContractTest) { $godotArguments += @('--', '--effect-manager-smoke') }
elseif ($BridgeDirectory) { $godotArguments += @('--', "--bridge-dir=$BridgeDirectory") }
if ($SmokeTest -or $ContractTest) {
    $checkOutput = & $godotExe @godotArguments 2>&1
    $checkExit = $LASTEXITCODE
    $checkOutput | Write-Output
    if ($checkExit -ne 0 -or !($checkOutput -match 'EFFECT_MANAGER_SMOKE: PASS')) { exit 1 }
    if ($ContractTest) {
        $checkOutput = & $godotExe --headless --path $projectRoot res://tools/card_effect_manager/card_effect_checks.tscn --quit-after 300 2>&1
        $checkExit = $LASTEXITCODE
        $checkOutput | Write-Output
        if ($checkExit -ne 0 -or !($checkOutput -match 'EFFECT_CONTRACT: PASS')) { exit 1 }
    }
    exit 0
}
& $godotExe @godotArguments
exit $LASTEXITCODE
