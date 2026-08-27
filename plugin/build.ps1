param(
    [string]$GameDirectory = $env:SEPHIRIA_GAME_DIR,
    [switch]$Deploy
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($GameDirectory)) {
    throw 'Specify -GameDirectory or set SEPHIRIA_GAME_DIR to your Sephiria installation directory.'
}
$repositoryDirectory = Split-Path -Parent $PSScriptRoot
$projectDirectory = $PSScriptRoot
$outputDirectory = Join-Path $repositoryDirectory 'bin'
$outputFile = Join-Path $outputDirectory 'SephiriaDpsMeter.dll'
$managedDirectory = Join-Path $GameDirectory 'Sephiria_Data\Managed'
$bepInExCore = Join-Path $GameDirectory 'BepInEx\core'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'

if (-not (Test-Path -LiteralPath $compiler)) {
    throw "C# compiler not found: $compiler"
}
if (-not (Test-Path -LiteralPath (Join-Path $managedDirectory 'Assembly-CSharp.dll'))) {
    throw "Sephiria managed assemblies not found under: $managedDirectory"
}

New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

$references = @(
    (Join-Path $bepInExCore 'BepInEx.dll'),
    (Join-Path $bepInExCore '0Harmony.dll'),
    (Join-Path $managedDirectory 'Assembly-CSharp.dll'),
    (Join-Path $managedDirectory 'Mirror.dll'),
    (Join-Path $managedDirectory 'netstandard.dll'),
    (Join-Path $managedDirectory 'UnityEngine.dll'),
    (Join-Path $managedDirectory 'UnityEngine.CoreModule.dll'),
    (Join-Path $managedDirectory 'UnityEngine.IMGUIModule.dll'),
    (Join-Path $managedDirectory 'Unity.InputSystem.dll'),
    (Join-Path $managedDirectory 'UnityEngine.TextRenderingModule.dll')
)

$arguments = @('/nologo', '/target:library', '/optimize+', "/out:$outputFile")
foreach ($reference in $references) {
    $arguments += "/reference:$reference"
}
$arguments += (Join-Path $projectDirectory 'Plugin.cs')
$arguments += (Join-Path $projectDirectory 'RoomScope.cs')
$arguments += (Join-Path $projectDirectory 'MeterLocalization.cs')

& $compiler $arguments
if ($LASTEXITCODE -ne 0) {
    throw "Compilation failed with exit code $LASTEXITCODE"
}

Write-Output "Built: $outputFile"

if ($Deploy) {
    $pluginDirectory = Join-Path $GameDirectory 'BepInEx\plugins'
    if (-not (Test-Path -LiteralPath $pluginDirectory)) {
        throw "BepInEx plugin directory not found: $pluginDirectory"
    }
    $destination = Join-Path $pluginDirectory 'SephiriaDpsMeter.dll'
    Copy-Item -LiteralPath $outputFile -Destination $destination -Force
    Write-Output "Deployed: $destination"
}
