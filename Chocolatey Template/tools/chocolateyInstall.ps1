$packageName = "servant"
$installerType = "msi" 
$url = "https://github.com/jhovgaard/servant/releases/download/1.0.14-client/Servant.Client.1.0.14.0.msi"
$validExitCodes = @(0,3010)
$packageParameters = $env:chocolateyPackageParameters;
if(!$packageParameters) {
  Write-ChocolateyFailure "servant" "Missing key. To install Servant you need to provide your key as the packageParameters.`r`nUsage example: cinst servant -packageParameters ""111111de-11b1-1111-b11b-11111d1a1db1"""
  return;
}

$silentArgs = "key=" + $packageParameters + " /qn";
Install-ChocolateyPackage "$packageName" "$installerType" "$silentArgs" "$url" -validExitCodes $validExitCodes