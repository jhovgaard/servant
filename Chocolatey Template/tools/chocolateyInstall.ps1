$packageName = 'Servant' # arbitrary name for the package, used in messages
$url = 'http://storage.servant.io/files/servant-1.0.5.zip' # download url
Install-ChocolateyZipPackage "$packageName" "$url" "$(Split-Path -parent $MyInvocation.MyCommand.Definition)" "$url"
$servantExe = Join-Path "$(Split-Path -parent $MyInvocation.MyCommand.Definition)" 'servant.server.exe'
Start-ChocolateyProcessAsAdmin "install" $servantExe -minimized