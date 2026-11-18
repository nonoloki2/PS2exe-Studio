$ErrorActionPreference = 'Stop'
dotnet build "$PSScriptRoot/src/PS2EXE.Studio.App/PS2EXE.Studio.App.csproj" -c Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
