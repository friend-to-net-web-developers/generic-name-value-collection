$ErrorActionPreference = 'Stop'

$packageName = 'pvm'
$toolsDir = "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)"
$fileLocation = Join-Path $toolsDir 'pvm.exe'

# Install-ChocolateyPath $toolsDir 'Machine'
# Chocolatey automatically shims executables in the tools directory, 
# so pvm.exe will be available in the path via pvm.exe shim.

Write-Host "pvm has been installed. You may need to restart your shell to use it."
