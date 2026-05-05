$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$dist = Join-Path $root "dist"
$source = Join-Path $root "Installer.cs"
$manifest = Join-Path $root "installer.manifest"
$embeddedApp = Join-Path $dist "FullPowerMode.exe"
$installer = Join-Path $root "InstallFullPowerMode.exe"
$distInstaller = Join-Path $dist "InstallFullPowerMode.exe"
$appLogo = Join-Path $root "logo\logo.png"
$whatsappLogo = Join-Path $root "logo\whatsapp.jpg"
$appIcon = Join-Path $dist "FullPowerMode.ico"

& (Join-Path $root "build.ps1")

if (-not (Test-Path $embeddedApp)) {
    throw "App exe is missing: $embeddedApp"
}

$cscCandidates = @(
    "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)

$csc = $cscCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $csc) {
    throw "Could not find the .NET Framework C# compiler."
}

$resourceArg = '/resource:"' + $embeddedApp + '",FullPowerMode.exe'
$iconArg = '/win32icon:"' + $appIcon + '"'
$appLogoResourceArg = '/resource:"' + $appLogo + '",AppLogo.png'
$whatsappLogoResourceArg = '/resource:"' + $whatsappLogo + '",WhatsAppLogo.jpg'

& $csc `
    /nologo `
    /target:winexe `
    /platform:anycpu `
    /optimize+ `
    /win32manifest:"$manifest" `
    $iconArg `
    /out:"$installer" `
    $resourceArg `
    $appLogoResourceArg `
    $whatsappLogoResourceArg `
    /reference:System.dll `
    /reference:System.Core.dll `
    /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll `
    "$source"

if ($LASTEXITCODE -ne 0) {
    throw "Installer build failed with compiler exit code $LASTEXITCODE."
}

Write-Host "Built $installer"

if (Test-Path $distInstaller) {
    Remove-Item -LiteralPath $distInstaller -Force
}
