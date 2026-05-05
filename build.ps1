$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$dist = Join-Path $root "dist"
$source = Join-Path $root "Program.cs"
$manifest = Join-Path $root "app.manifest"
$exe = Join-Path $dist "FullPowerMode.exe"
$appLogo = Join-Path $root "logo\logo.png"
$whatsappLogo = Join-Path $root "logo\whatsapp.jpg"
$appIcon = Join-Path $dist "FullPowerMode.ico"

New-Item -ItemType Directory -Force -Path $dist | Out-Null

if (-not (Test-Path $appLogo)) {
    throw "Missing app logo: $appLogo"
}

if (-not (Test-Path $whatsappLogo)) {
    throw "Missing WhatsApp logo: $whatsappLogo"
}

Add-Type -AssemblyName System.Drawing
$sourceBitmap = [System.Drawing.Bitmap]::FromFile($appLogo)
try {
    $iconBitmap = New-Object System.Drawing.Bitmap 256, 256
    $graphics = [System.Drawing.Graphics]::FromImage($iconBitmap)
    try {
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality

        $scale = [Math]::Min(224 / $sourceBitmap.Width, 224 / $sourceBitmap.Height)
        $width = [int]($sourceBitmap.Width * $scale)
        $height = [int]($sourceBitmap.Height * $scale)
        $x = [int]((256 - $width) / 2)
        $y = [int]((256 - $height) / 2)
        $graphics.DrawImage($sourceBitmap, $x, $y, $width, $height)

        $handle = $iconBitmap.GetHicon()
        try {
            $icon = [System.Drawing.Icon]::FromHandle($handle)
            $stream = [System.IO.File]::Create($appIcon)
            try { $icon.Save($stream) }
            finally { $stream.Dispose(); $icon.Dispose() }
        }
        finally {
            if ($handle -ne [IntPtr]::Zero) {
                Add-Type -Namespace Win32 -Name NativeMethods -MemberDefinition '[System.Runtime.InteropServices.DllImport("user32.dll")] public static extern bool DestroyIcon(System.IntPtr handle);' -ErrorAction SilentlyContinue
                [Win32.NativeMethods]::DestroyIcon($handle) | Out-Null
            }
        }
    }
    finally {
        $graphics.Dispose()
        $iconBitmap.Dispose()
    }
}
finally {
    $sourceBitmap.Dispose()
}

$cscCandidates = @(
    "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)

$csc = $cscCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $csc) {
    throw "Could not find the .NET Framework C# compiler."
}

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
    /out:"$exe" `
    $appLogoResourceArg `
    $whatsappLogoResourceArg `
    /reference:System.dll `
    /reference:System.Core.dll `
    /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll `
    "$source"

if ($LASTEXITCODE -ne 0) {
    throw "Build failed with compiler exit code $LASTEXITCODE."
}

Write-Host "Built $exe"
