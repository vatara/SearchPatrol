Push-Location $PSScriptRoot

# compress-archive makes zips that don't work many places because it uses \ instead of / for folders
.\7z u SearchPatrol-standalone.zip ../bin/x64/Release/*

Pop-Location