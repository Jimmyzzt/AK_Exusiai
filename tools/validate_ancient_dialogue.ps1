param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$PckPath
)

$ErrorActionPreference = 'Stop'
$character = 'AK_EXUSIAI_CHARACTER_EXUSIAI'
$ancients = @('NEOW', 'DARV', 'NONUPEIPE', 'OROBAS', 'PAEL', 'TANX', 'TEZCATARA', 'VAKUU', 'THE_ARCHITECT')
$tables = @{}
$sourceBytes = @{}

function Assert-Dialogue([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

foreach ($language in @('zhs', 'eng')) {
    $path = Join-Path $RepositoryRoot "AK_Exusiai/localization/$language/ancients.json"
    $bytes = [IO.File]::ReadAllBytes($path)
    $document = [System.Text.Json.JsonDocument]::Parse([Text.Encoding]::UTF8.GetString($bytes))
    $table = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::Ordinal)
    try {
        foreach ($property in $document.RootElement.EnumerateObject()) {
            Assert-Dialogue (-not $table.ContainsKey($property.Name)) "Duplicate key: $($property.Name)"
            Assert-Dialogue ($property.Value.ValueKind -eq 'String') "Non-string value: $($property.Name)"
            $table.Add($property.Name, $property.Value.GetString())
        }
    } finally { $document.Dispose() }
    $used = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $lineCount = 0
    foreach ($ancient in $ancients) {
        $prefix = "$ancient.talk.$character."
        for ($sequence = 0; $sequence -lt 3; $sequence++) {
            $repeat = if ($sequence -eq 1 -or ($ancient -eq 'THE_ARCHITECT' -and $sequence -eq 2)) { 'r' } else { '' }
            $lines = @($table.Keys | Where-Object { $_ -match "^$([regex]::Escape($prefix))$sequence-\d+$repeat\.(ancient|char)$" })
            Assert-Dialogue ($lines.Count -ge 2) "Missing dialogue: $prefix$sequence"
            for ($line = 0; $line -lt $lines.Count; $line++) {
                $stem = "$prefix$sequence-$line$repeat"
                $speakers = @('ancient', 'char' | Where-Object { $table.ContainsKey("$stem.$_") })
                Assert-Dialogue ($speakers.Count -eq 1) "Missing/ambiguous speaker: $stem"
                $key = "$stem.$($speakers[0])"
                Assert-Dialogue (-not [string]::IsNullOrWhiteSpace($table[$key])) "Empty text: $key"
                Assert-Dialogue ($table[$key] -notmatch '[{}]') "Unexpected SmartFormat variable: $key"
                $tags = [Collections.Generic.Stack[string]]::new()
                foreach ($tag in [regex]::Matches($table[$key], '\[(/?)([a-z_]+)(?:=[^\]]+)?\]')) {
                    if ($tag.Groups[1].Value -eq '/') {
                        Assert-Dialogue ($tags.Count -gt 0) "Unexpected closing tag: $key"
                        Assert-Dialogue ($tags.Pop() -ceq $tag.Groups[2].Value) "Mismatched tag: $key"
                    } else { $tags.Push($tag.Groups[2].Value) }
                }
                Assert-Dialogue ($tags.Count -eq 0) "Unclosed tag: $key"
                $null = $used.Add($key)
                if ($line -lt $lines.Count - 1) {
                    Assert-Dialogue ($table.ContainsKey("$stem.next")) "Missing button: $stem"
                    Assert-Dialogue (-not [string]::IsNullOrWhiteSpace($table["$stem.next"])) "Empty button: $stem"
                    $null = $used.Add("$stem.next")
                }
                $lineCount++
            }
            if ($ancient -eq 'THE_ARCHITECT') {
                $key = "$prefix$sequence-endattack"
                Assert-Dialogue ($table.ContainsKey($key) -and $table[$key] -ceq 'Both') "Wrong Architect ending: $key"
                $null = $used.Add($key)
            }
        }
    }
    Assert-Dialogue ($used.Count -eq $table.Count) "Unexpected or unreachable keys in $language"
    $tables[$language] = $table
    $sourceBytes["AK_Exusiai/localization/$language/ancients.json"] = $bytes
    Write-Output "PASS $language`: $($ancients.Count) events, 27 dialogues, $lineCount lines, $($table.Count) keys"
}
Assert-Dialogue ($tables.zhs.Count -eq $tables.eng.Count) 'Locale key counts differ'
foreach ($key in $tables.zhs.Keys) {
    Assert-Dialogue ($tables.eng.ContainsKey($key)) "English key missing: $key"
}

# Godot 4.5 writes a v3 PCK with its directory offset at byte 32.
if ($PckPath) {
    $reader = [IO.BinaryReader]::new([IO.File]::OpenRead((Resolve-Path -LiteralPath $PckPath).Path))
    try {
        Assert-Dialogue ($reader.ReadUInt32() -eq 0x43504447) 'Not a Godot PCK'
        Assert-Dialogue ($reader.ReadUInt32() -eq 3) 'Expected PCK version 3'
        $reader.BaseStream.Position = 20
        $flags = $reader.ReadUInt32()
        Assert-Dialogue (($flags -band 1) -eq 0) 'Encrypted PCK not supported'
        $fileBase = $reader.ReadUInt64()
        $directoryOffset = $reader.ReadUInt64()
        $reader.BaseStream.Position = $directoryOffset
        $count = $reader.ReadUInt32()
        $found = 0
        for ($entry = 0; $entry -lt $count; $entry++) {
            $name = [Text.Encoding]::UTF8.GetString($reader.ReadBytes($reader.ReadInt32())).TrimEnd([char]0)
            $offset = $reader.ReadUInt64()
            $length = $reader.ReadUInt64()
            $null = $reader.ReadBytes(16)
            $entryFlags = $reader.ReadUInt32()
            if ($sourceBytes.ContainsKey($name)) {
                Assert-Dialogue ($entryFlags -eq 0) "Unsupported PCK entry flags: $name"
                $position = $reader.BaseStream.Position
                $reader.BaseStream.Position = $fileBase + $offset
                $packed = $reader.ReadBytes([int]$length)
                $reader.BaseStream.Position = $position
                Assert-Dialogue ([Convert]::ToBase64String($packed) -ceq [Convert]::ToBase64String($sourceBytes[$name])) "PCK/source mismatch: $name"
                $found++
            }
        }
        Assert-Dialogue ($found -eq 2) 'PCK must contain both dialogue tables'
        Write-Output 'PASS PCK: both dialogue tables match source byte-for-byte'
    } finally { $reader.Dispose() }
}
