Push-Location $PSScriptRoot
msbuild .. -p:Configuration=Release /p:Platform=x64
iscc SearchPatrolSetup.iss
Pop-Location
