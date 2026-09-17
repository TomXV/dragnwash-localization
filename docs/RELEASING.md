# Releasing

[日本語](RELEASING.ja.md)

This document describes how to distribute the mod. Building the plugin requires game-derived reference assemblies in `libs/`. They cannot be committed to this repository for copyright reasons. As a result, **the plugin cannot be built in CI; releases must be built locally on a computer with the game installed** and uploaded to GitHub Releases.

Translation-only changes do not require a build. Translators should see [CONTRIBUTING.md](../CONTRIBUTING.md).

## Prerequisites

- The Steam version of Drag'n Wash is installed.
- All game-derived reference assemblies are present in `src/DragNWashLocalization/libs/`. See the comments in the `.csproj` file for the complete list.
- The .NET SDK and PowerShell 7 (`pwsh`) are installed.
- [Drag'n Wash ModFramework](https://github.com/TomXV/dragnwash-modframework) is checked out next to this repository (or pass `-FrameworkPath` to `pack.ps1`), with its own `libs/` copied by its `tools/copy-libs.ps1`. `pack.ps1` builds it and ships the core, the libraries and the preloader patcher in the zip.
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
BepInEx/patchers/DragNWash.ModFramework.Preloader.dll
BepInEx/plugins/DragNWash.ModFramework/DragNWash.ModFramework.dll
BepInEx/plugins/DragNWash.ModFramework/LICENSE.txt
BepInEx/plugins/DragNWash.ModFramework/icon.png
BepInEx/plugins/DragNWash.ModFramework/ModsButton0.png
BepInEx/plugins/DragNWash.ModFramework/ModsButton1.png
BepInEx/plugins/DragNWash.ModFramework.Text/DragNWash.ModFramework.Text.dll
BepInEx/plugins/DragNWash.ModFramework.Dialogue/DragNWash.ModFramework.Dialogue.dll
BepInEx/plugins/DragNWash.ModFramework.ToolWindow/DragNWash.ModFramework.ToolWindow.dll
BepInEx/plugins/DragNWash.ModFramework.Assets/DragNWash.ModFramework.Assets.dll
BepInEx/plugins/DragNWash.ModFramework.Saves/DragNWash.ModFramework.Saves.dll
BepInEx/plugins/DragNWashLocalization/DragNWashLocalization.dll
BepInEx/plugins/DragNWashLocalization/icon.png
BepInEx/plugins/DragNWashLocalization/Translations/<locale>/strings.csv
BepInEx/plugins/DragNWashLocalization/Translations/ignore.txt
BepInEx/plugins/DragNWashLocalization/Translations/<locale>/name.txt
BepInEx/plugins/DragNWashLocalization/FlagCatalog.csv
BepInEx/plugins/DragNWashLocalization/dragnwash-menufont.bundle
BepInEx/plugins/DragNWashLocalization/dragnwash-menufont-LICENSE.txt
BepInEx/plugins/DragNWashLocalization/data/script_order.csv
BepInEx/plugins/DragNWashLocalization/data/level_flow.csv
Install.exe
install-steamdeck.sh
mod-install.json
README.md
README.ja.md
```

Four of those are conditional: `pack.ps1` copies `BepInEx/plugins/DragNWash.ModFramework/LICENSE.txt` only when the framework checkout has a `LICENSE` at its root, and `icon.png`, `ModsButton0.png` and `ModsButton1.png` only when `src/DragNWash.ModFramework/` in that checkout has them. Everything else in the list is always written.

`Install.exe` and `install-steamdeck.sh` are Drag'n Wash ModFramework's shared installers, which `pack.ps1` builds and copies from the framework checkout ([docs/INSTALLER.md](https://github.com/TomXV/dragnwash-modframework/blob/main/docs/INSTALLER.md) there). `pack.ps1` also writes `mod-install.json`: this mod's folder, the player's data to keep, its config file, and the language question with every shipped pack. The `Install.exe` build is deterministic, so antivirus reputation is not reset with each release; `pack.ps1` prints its SHA-256, which should match the previous release while the framework's `installer/` is unchanged. Users double-click it to install, update, or uninstall. Extracting the `BepInEx/` directory into the game folder by hand still works. The experimental macOS script in `installer/experimental/` is not packaged.

To install it, extract the archive into the game directory and merge the included `BepInEx/` directory.

### 2b. Or let GitHub build it

The **Build** workflow (Actions) does step 2 on a Windows runner: on a push to `main`, on a `v*` tag, or by hand (with a framework branch or tag to ship). It checks out Drag'n Wash ModFramework, fetches the reference assemblies from the private repository `TomXV/dragnwash-libs` with the `LIBS_TOKEN` secret, runs `tools/pack.ps1 -FrameworkPath`, and uploads the zip as a workflow artifact. A tag also creates a **draft** release with the zip attached, so the flow is: bump the version, commit, push the tag, wait for the workflow, then write the notes on the draft and publish it. The workflow never runs for pull requests. After a game update, refresh the private repository with `tools/copy-libs.ps1` (from both repositories' game installs). Local `pack.ps1` stays as the fallback.

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

> Since 2026-09-16 they can be: see [2b](#2b-or-let-github-build-it). The reference assemblies live in a private repository that only the Build workflow reads; this public repository still never contains them.

The game DLLs required for compilation, including `UnityEngine.CoreModule.dll` and `YarnSpinner.dll`, cannot be included in the repository. GitHub Actions therefore cannot compile the plugin. Builds are created locally, and only the resulting ZIP is attached to a release.

- After a game update, add the new build's files to `ci/game-fingerprints.json` with `tools/game-fingerprints.py`, on Windows and on the Steam Deck, and copy the file to Drag'n Wash ModFramework too. CI compares every file in the repository with it and refuses copies of the game's files.
- Also after a game update (experimental): press F6 and F7 in the game to refresh `_discovered/`, run `python tools/rekey.py replay --discovered <that folder>` to see which lines the update changed, then copy the new `script_order.csv` into `data/` and run `python tools/rekey.py augment --discovered <that folder>` so it carries the resolver's keys. `tools/linekeys.py` must stay identical to the framework's copy; CI checks it against `ci/linekey-vectors.json`.
