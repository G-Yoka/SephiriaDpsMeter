param(
    [string]$GameDirectory = $env:SEPHIRIA_GAME_DIR
)

$ErrorActionPreference = 'Stop'
$sourcePath = Join-Path $PSScriptRoot 'SephiriaDpsMeter\Plugin.cs'
$versionMatch = [regex]::Match((Get-Content -LiteralPath $sourcePath -Raw), 'PluginVersion\s*=\s*"([^"]+)"')
if (-not $versionMatch.Success) { throw 'Plugin version was not found.' }
$version = $versionMatch.Groups[1].Value
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Unexpected plugin version format.' }

$distDirectory = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Path $distDirectory -Force | Out-Null
$zipPath = Join-Path $distDirectory "SephiriaDpsMeter-v$version.zip"
if (Test-Path -LiteralPath $zipPath) { throw "Package already exists: $zipPath" }

& (Join-Path $PSScriptRoot 'build.ps1') -GameDirectory $GameDirectory
$dllPath = Join-Path $PSScriptRoot 'bin\SephiriaDpsMeter.dll'

# Only explicit plugin/documentation files enter the archive. No game DLLs or configs.
$entries = @(
    @{ Source = $dllPath; Entry = 'BepInEx/plugins/SephiriaDpsMeter.dll' },
    @{ Source = (Join-Path $PSScriptRoot 'INSTALL.md'); Entry = 'INSTALL.md' },
    @{ Source = (Join-Path $PSScriptRoot 'README.md'); Entry = 'README.md' },
    @{ Source = (Join-Path $PSScriptRoot 'README.en.md'); Entry = 'README.en.md' },
    @{ Source = (Join-Path $PSScriptRoot 'CHANGELOG.md'); Entry = 'CHANGELOG.md' },
    @{ Source = (Join-Path $PSScriptRoot 'screenshots\dps-panel.png'); Entry = 'screenshots/dps-panel.png' },
    @{ Source = (Join-Path $PSScriptRoot 'screenshots\dps-panel-recording.png'); Entry = 'screenshots/dps-panel-recording.png' },
    @{ Source = (Join-Path $PSScriptRoot 'screenshots\settings.png'); Entry = 'screenshots/settings.png' },
    @{ Source = (Join-Path $PSScriptRoot 'screenshots\native-settings.png'); Entry = 'screenshots/native-settings.png' }
)
foreach ($entry in $entries) {
    if (-not (Test-Path -LiteralPath $entry.Source -PathType Leaf)) { throw "Missing package file: $($entry.Source)" }
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($entry in $entries) {
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
            $archive, $entry.Source, $entry.Entry, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
}
finally { $archive.Dispose() }

$standaloneDll = Join-Path $distDirectory 'SephiriaDpsMeter.dll'
Copy-Item -LiteralPath $dllPath -Destination $standaloneDll
$checksumLines = @(
    ((Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + [System.IO.Path]::GetFileName($zipPath)),
    ((Get-FileHash -LiteralPath $standaloneDll -Algorithm SHA256).Hash.ToLowerInvariant() + '  SephiriaDpsMeter.dll')
)
[System.IO.File]::WriteAllLines((Join-Path $distDirectory 'SHA256SUMS.txt'), $checksumLines, [System.Text.Encoding]::ASCII)
Write-Output "Packaged: $zipPath"
Write-Output "Release assets: $distDirectory"
