# Full Power Mode

Windows GUI toggle app for the provided power-boost PowerShell behavior.

## Build App

Run:

```powershell
.\build.ps1
```

Output:

```text
dist\FullPowerMode.exe
```

This is an intermediate app build used by the installer.

## Build Installer

Run:

```powershell
.\build-installer.ps1
```

Output:

```text
InstallFullPowerMode.exe
```

Double-click `InstallFullPowerMode.exe` to install the app to `C:\Program Files\FullPowerMode` and create Desktop plus Start Menu shortcuts. The setup window also includes an Uninstall button.

## Behavior

- Starts with a UAC administrator prompt through `app.manifest`.
- Runs as a real WinForms app with no PowerShell console.
- Toggle ON enables Ultimate Performance, CPU min/max 100%, power throttling off, app responsiveness tweaks, foreground app priority, DirectX GPU preference key creation, and working-set trimming.
- Toggle OFF restores the previously active power scheme and the registry values changed by the app.
- Closing the window hides it to the tray; use the tray menu to open, toggle, restore, or exit.
