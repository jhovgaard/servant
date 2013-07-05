$packageName = 'Servant' # arbitrary name for the package, used in messages
$servantExe = Join-Path "$(Split-Path -parent $MyInvocation.MyCommand.Definition)" 'servant.server.exe'
Start-ChocolateyProcessAsAdmin "uninstall" $servantExe -minimized