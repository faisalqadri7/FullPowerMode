# Full Power Mode

Windows GUI toggle app for the provided power-boost PowerShell behavior.

## Download

Download the latest installer from GitHub Releases:

[Download InstallFullPowerMode.exe](https://github.com/faisalqadri7/FullPowerMode/releases/latest/download/InstallFullPowerMode.exe)

After downloading, run `InstallFullPowerMode.exe` and approve the Windows administrator prompt.


## Behavior

- Starts with a UAC administrator prompt through `app.manifest`.
- Runs as a real WinForms app with no PowerShell console.
- Toggle ON enables Ultimate Performance, CPU min/max 100%, power throttling off, app responsiveness tweaks, foreground app priority, DirectX GPU preference key creation, and working-set trimming.
- Toggle OFF restores the previously active power scheme and the registry values changed by the app.
- Closing the window hides it to the tray; use the tray menu to open, toggle, restore, or exit.
