$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$testDirectory = Join-Path $PSScriptRoot 'bin\dps-tests'
New-Item -ItemType Directory -Path $testDirectory -Force | Out-Null
$testProgram = Join-Path $testDirectory 'DpsRoomScopeTests.exe'
& $compiler /nologo /target:exe "/out:$testProgram" (Join-Path $PSScriptRoot 'SephiriaDpsMeter\RoomScope.cs') (Join-Path $PSScriptRoot 'tests\DpsRoomScopeTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Room scope test compilation failed.' }
& $testProgram
if ($LASTEXITCODE -ne 0) { throw 'Room scope tests failed.' }
