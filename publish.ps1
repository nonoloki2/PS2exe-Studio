$ErrorActionPreference = 'Stop'
dotnet publish "$PSScriptRoot/src/PS2EXE.Studio.App/PS2EXE.Studio.App.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o "$PSScriptRoot/publish/win-x64"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
