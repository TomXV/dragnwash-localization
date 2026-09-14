# Releasing

[日本語](RELEASING.ja.md)

This document describes how to distribute the mod. Building the plugin requires game-derived reference assemblies in `libs/`. They cannot be committed to this repository for copyright reasons. As a result, **the plugin cannot be built in CI; releases must be built locally on a computer with the game installed** and uploaded to GitHub Releases.

Translation-only changes do not require a build. Translators should see [CONTRIBUTING.md](../CONTRIBUTING.md).

## Prerequisites

- The Steam version of Drag'n Wash is installed.
- All game-derived reference assemblies are present in `src/DragNWashLocalization/libs/`. See the comments in the `.csproj` file for the complete list.
- The .NET SDK and PowerShell 7 (`pwsh`) are installed.
- To create GitHub Releases from the command line, install [`gh`](https://cli.github.com/).

## Procedure

### 1. Update the version

Update `PluginVersion` in `src/DragNWashLocalization/Plugin.cs` and `<Version>` / `<FileVersion>` in the `.csproj` (the installer shows the file version).

The value is used in the BepInEx plugin ID string and must use the `x.y.z` format, for example `0.2.0`.

Update the status in `docs/PLAN.md` and `README.md` as needed.

### 2. Commit, tag, then build the ZIP

Commit the version bump and create the tag **before** building. The DLL's informational version embeds the commit hash of the checkout it was built from (visible as `0.1.1+<hash>` in the file properties), so building first would stamp the previous commit. The build is a clean one so the hash is refreshed:

```powershell
git commit -am "Release 0.2.0"
git tag -a v0.2.0 -m "v0.2.0"
Remove-Item -Recurse -Force src/DragNWashLocalization/obj, src/DragNWashLocalization/bin
pwsh tools/pack.ps1
```

This creates `release/DragNWashLocalization-<version>.zip` with the following structure:

```text
BepInEx/plugins/DragNWashLocalization/DragNWashLocalization.dll
BepInEx/plugins/DragNWashLocalization/Translations/<locale>/strings.csv
BepInEx/plugins/DragNWashLocalization/Translations/ignore.txt
BepInEx/plugins/DragNWashLocalization/Translations/<locale>/name.txt
BepInEx/plugins/DragNWashLocalization/FlagCatalog.csv
BepInEx/plugins/DragNWashLocalization/dragnwash-menufont.bundle
BepInEx/plugins/DragNWashLocalization/dragnwash-menufont-LICENSE.txt
BepInEx/plugins/DragNWashLocalization/data/script_order.csv
BepInEx/plugins/DragNWashLocalization/data/level_flow.csv
Install.exe
Install.cmd
install-steamdeck.sh
installer/Installer.ps1
README.md
README.ja.md
```

`Install.exe` is a small console-less launcher compiled by `pack.ps1` with the C# compiler that ships with .NET Framework 4 (`%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe`); nothing extra needs to be installed. Users double-click it to install, update, or uninstall. Extracting the `BepInEx/` directory into the game folder by hand still works. `Install.cmd` opens the same installer window on machines where SmartScreen or a policy blocks the unsigned `Install.exe`. `install-steamdeck.sh` is the Steam Deck / Linux installer and must keep LF line endings, and `Install.cmd` must keep CRLF (both enforced by `.gitattributes`). The experimental macOS script in `installer/experimental/` is not packaged.

To install it, extract the archive into the game directory and merge the included `BepInEx/` directory.

### 3. Validate the package

- Confirm that the Release build produces no warnings or errors.
- Extract the ZIP and confirm that the DLL and `Translations/` directory are in the correct locations.
- If possible, launch the game once with a clean BepInEx installation and verify the F1 menu and language switching, including **Options → Language (Mod)** (pick, Save, Back).
- When the installers changed, run `Install.exe` (install and uninstall) on Windows, and `install-steamdeck.sh` on a Steam Deck.

### 4. Create the GitHub Release

```powershell
gh release create v0.2.0 release/DragNWashLocalization-0.2.0.zip `
  --title "v0.2.0" `
  --notes "Describe the changes here"
```

Use tags prefixed with `v`, such as `v0.2.0`. You may also use the GitHub web interface: open Releases, draft a new release, create the tag, and upload the ZIP.

The release notes should say that `Install.exe` and `install-steamdeck.sh` download BepInEx automatically, while a manual installation needs BepInEx 5 separately, and should list the supported platforms. See the [README](../README.md).

## Rebuilding a published release

Do not move a tag that has already been pushed. If a published release has to be rebuilt (for example to add a file to its zip), delete the GitHub release and its tag first, then tag the new commit, build, and create the release again as above. Deleting the release resets its download count. If the release was a pre-release, decide before publishing whether the new one should be the latest release.

## Why releases are not built in CI

The game DLLs required for compilation, including `UnityEngine.CoreModule.dll` and `YarnSpinner.dll`, cannot be included in the repository. GitHub Actions therefore cannot compile the plugin. Builds are created locally, and only the resulting ZIP is attached to a release.
