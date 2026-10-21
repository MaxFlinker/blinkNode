# Windows setup installer

`build-setup.ps1` publishes blinkNode Light as a self-contained Windows x64 application and builds the standard setup wizard with Inno Setup 6. The setup installs for all users under Program Files, creates a Start Menu shortcut, and offers an optional desktop shortcut. It includes the .NET runtime, so users do not need to install it separately.

Run from the repository root:

```powershell
.\installer\build-setup.ps1
```

If Inno Setup is installed in a custom location, pass its compiler path with `-Iscc`. The installer is written to `publish\blinkNode-Light-Setup-0.1.0-win-x64.exe`.
