# Full Power Mode

Windows GUI toggle app for the provided power-boost PowerShell behavior.

## Download

Download the latest installer from GitHub Releases:

[Download InstallFullPowerMode.exe](https://github.com/faisalqadri7/FullPowerMode/releases/latest/download/InstallFullPowerMode.exe)

After downloading, run `InstallFullPowerMode.exe` and approve the Windows administrator prompt.

## Latest Release

Version `v1.0.3` includes a fresh single-file installer built from the latest app code:

- Embeds the current `FullPowerMode.exe` inside `InstallFullPowerMode.exe`.
- Displays `1.0.3` in the app and setup window metadata.
- Rolls back backed-up power settings if enable fails partway through.
- Restores power settings before uninstall removes the app.
- Backs up and restores CPU processor min/max plan values.
- Prevents setup from closing while install or uninstall is still running.

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

## Behavior

- Starts with a UAC administrator prompt through `app.manifest`.
- Runs as a real WinForms app with no PowerShell console.
- Toggle ON enables Ultimate Performance, CPU min/max 100%, power throttling off, app responsiveness tweaks, foreground app priority, DirectX GPU preference key creation, and working-set trimming.
- Toggle OFF restores the previously active power scheme and the registry values changed by the app.
- Closing the window hides it to the tray; use the tray menu to open, toggle, restore, or exit.
