$packageName = "servant"
$installerType = "msi" 
$url = "https://github.com/jhovgaard/servant/releases/download/1.0.14-client/Servant.Client.1.0.14.0.msi"
$validExitCodes = @(0,3010)
$packageParameters = $env:chocolateyPackageParameters;
if(!$packageParameters) {
  Write-ChocolateyFailure "servant" "Missing key"
  return;
}

$silentArgs = "key=" + $packageParameters + " /qn";
Install-ChocolateyPackage "$packageName" "$installerType" "$silentArgs" "$url" -validExitCodes $validExitCodes