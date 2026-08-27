$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$testDirectory = Join-Path $PSScriptRoot 'bin\dps-tests'
New-Item -ItemType Directory -Path $testDirectory -Force | Out-Null
$testProgram = Join-Path $testDirectory 'DpsLocalizationTests.exe'
& $compiler /nologo /target:exe "/out:$testProgram" (Join-Path $PSScriptRoot 'SephiriaDpsMeter\MeterLocalization.cs') (Join-Path $PSScriptRoot 'tests\DpsLocalizationTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Localization test compilation failed.' }
& $testProgram
if ($LASTEXITCODE -ne 0) { throw 'Localization tests failed.' }

$source = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'SephiriaDpsMeter\Plugin.cs') -Raw
if ($source -match '[\u4e00-\u9fff]') { throw 'Hardcoded Chinese remains outside the localization catalog.' }
if ($source -notmatch 'text\.SetLanguage\(LocalizationManager\.Instance\.CurrentLanguage\)') {
    throw 'The panel must read the current game language.'
}
Write-Output 'PASS: UI labels centralized; language is read from the game.'
