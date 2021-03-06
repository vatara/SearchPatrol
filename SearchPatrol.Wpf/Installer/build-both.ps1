Push-Location $PSScriptRoot
msbuild .. -p:Configuration=Release /p:Platform=x64
# compress-archive makes zips that don't work many places because it uses \ instead of / for folders
.\7z u SearchPatrol-standalone.zip ../bin/x64/Release/*
iscc SearchPatrolSetup.iss
Pop-Location
