$args="/x {4cc767f1-7837-45a3-882b-5beda6f6e641} /qn"
Start-ChocolateyProcessAsAdmin "$args" 'msiexec' -validExitCodes @(0)
