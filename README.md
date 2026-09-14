# Drag'n Wash Localization

[日本語](README.ja.md)

An unofficial BepInEx-based multilingual localization mod for [Drag'n Wash](https://store.steampowered.com/app/4739660/).

It plays the game in many languages (see [Language packs](#language-packs)), and anyone can add or improve a language by editing CSV files, without writing code.

See [docs/PLAN.md](docs/PLAN.md) for the technical research and implementation plan.

> [!WARNING]
> ## ⚠️ SPOILER WARNING ⚠️
> **The CSV files under `Translations/` contain every conversation in the game, in story order.**
> Opening them will spoil the story. Play through the game a few times first!

## Installation

### Quick install (recommended)

Installing is really easy.

1. Download the zip from the [Releases page](https://github.com/TomXV/dragnwash-localization/releases) and extract it anywhere.
2. Double-click **`Install.exe`**.
3. Pick a language and **click Install / Update**.

> [!TIP]
> The same steps are also on Steam as a guide: [English](https://steamcommunity.com/sharedfiles/filedetails/?id=3801420947) / [日本語](https://steamcommunity.com/sharedfiles/filedetails/?id=3801418794). Drag'n Wash has no Steam Workshop, so the mod itself is downloaded from GitHub Releases.

## See? EASY. ( ･´ｰ･｀) HEH! YIP!

The installer finds the game through Steam on its own (or lets you pick the folder). If BepInEx is not installed yet, it downloads the official 5.4.23.5 release, verifies its SHA-256, and unpacks it for you. Then just start the game from Steam.

Languages: 日本語 / 简体中文 / English (no translation), plus provisional packs for Traditional Chinese, German, French, Spanish, Brazilian Portuguese, Korean, Russian, Polish and Hebrew, and for fun Esperanto and Toki Pona (see [Language packs](#language-packs)). The same window has an **Uninstall** button; save-history snapshots are kept by default, and BepInEx is removed together with the mod only when the installer put it there and no other plugin uses it.

If you prefer to do it by hand, follow the manual steps below.

### How to install (video)

![Install guide video](docs/media/install-guide-full-en.gif)


> [!NOTE]
> **If nothing happens when you run `Install.exe`, or Windows says "Windows protected your PC"**
> `Install.exe` is a small unsigned program, so Windows SmartScreen may stop it the first time.
> - If the warning appears, click **More info → Run anyway**.
> - If no window appears at all, double-click **`Install.cmd`** in the same folder instead. A console flashes for a moment and the same installer window opens.
> - Failing that, right-click `Install.exe` → Properties → tick **Unblock** → OK, then double-click it again.

> [!WARNING]
> **If Windows Security (Microsoft Defender) detects `Install.exe` as "Trojan:Script/Wacatac.B!ml", or `Install.exe` is missing from the folder you extracted**
> This is a false positive. The `!ml` suffix means a machine-learning model guessed the file looks suspicious, not that it matched known malware. `Install.exe` is a small unsigned program that only starts the installer window (a PowerShell script) without a console window, and that way of starting a script resembles what malware does. Its source is public: [`installer/Launcher.cs`](installer/Launcher.cs) and [`installer/Installer.ps1`](installer/Installer.ps1).
>
> When the file is quarantined automatically no threat name is shown, and `Install.exe` simply looks missing from the extracted folder. Windows Security → **Protection history** shows what was removed.
> - First make sure the zip you downloaded is genuine. In PowerShell, run `(Get-FileHash "<path to the zip>").Hash -eq ("<the sha256 from Releases>" -replace '^sha256:')`. It prints `True` when the file is the one published here; if it prints `False`, delete the file and do not use it.
> - The `sha256` is shown under `DragNWashLocalization-<version>.zip` on the [Releases](https://github.com/TomXV/dragnwash-localization/releases) page. The two automatic "Source code" rows have no hash, so do not use those.
> - A match confirms the file is the one published here. It is not by itself proof that the file is safe, which is what the source linked above is for.
> - If it matches, open the detection in Windows Security → **Protection history** and choose **Actions → Allow on device**. Allow that one file only: there is no need to add a folder exclusion or to turn Windows Security off.
> - **`Install.cmd`** in the same folder opens the same installer window without `Install.exe`. Whether it avoids the detection is untested, but it is worth a try.
> - If you would rather not allow anything, use the manual installation steps below instead.
> - Do not use copies from anywhere other than this repository's Releases page.

### Windows on ARM (verified)

On ARM Windows PCs such as Snapdragon X laptops, install with the same `Install.exe` steps; the mod works (the game itself runs as x64 under emulation).

> [!IMPORTANT]
> **The game does not render correctly on DirectX 12 there, so add `-force-d3d11` to its Steam launch options.** This happens without the mod too; it is a problem in the game, not in this mod.
> - With an older GPU driver the game crashes right after the splash screen.
> - With the latest driver it no longer crashes, but 3D is not drawn.
>
> In Steam, right-click the game → **Properties** → **Launch Options**, enter `-force-d3d11`, and the game starts on DirectX 11 and plays normally.
> Verified on an ASUS ProArt PZ13 (Snapdragon X Plus / Adreno X1-45).

### Steam Deck / Linux (verified)

> [!IMPORTANT]
> Steam Deck support requires **v0.3.0 or later**. Earlier versions run on the Deck but the F1 menu cannot be operated there.

Works with the native Linux build of the game and the Linux build of BepInEx. `Install.exe` is for Windows; on the Deck use the install script instead.

**Install script (recommended)**, in Desktop Mode:

1. Download the zip from the [Releases page](https://github.com/TomXV/dragnwash-localization/releases) and extract it (right-click → Extract).
2. Open the extracted folder, right-click an empty spot and choose **Open Terminal Here**.
3. Type the following and press Enter:

   ```bash
   bash install-steamdeck.sh
   ```

4. Choose **Install / Update**, then pick a language. Steam has to close for a moment so the launch option can be set; the script asks first and starts Steam again.
5. Go back to Gaming Mode and start the game. Change language later in **Options → Language (Mod)**.

The script finds the game in your Steam libraries (including an SD card), downloads the official Linux BepInEx 5.4.23.5 and checks its SHA-256, sets `executable_name="DragNWash"` in `run_bepinex.sh`, copies the mod, and adds `./run_bepinex.sh %command%` to the game's launch options while keeping any options you already had. To update or remove the mod, run the same command again and choose **Install / Update** or **Uninstall**. Uninstalling keeps your save history, takes `./run_bepinex.sh` back out of the launch options when no other BepInEx mod needs it, and offers to remove BepInEx as well. `--install` and `--uninstall` skip the question.

Steam rewrites launch options while it is running, so when the launch option has to change the script closes Steam, edits it, and starts Steam again (it asks first; `--close-steam` skips that question). If a step could not be done, the final dialog says so and tells you what to change by hand. Each run is logged to `~/.local/state/dragnwash-localization/installer.log`.

<details>
<summary>Manual installation on the Deck</summary>

1. Extract [BepInEx_linux_x64_5.4.23.5.zip](https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.5/BepInEx_linux_x64_5.4.23.5.zip) into the game folder (`~/.local/share/Steam/steamapps/common/Drag'n Wash/`).
2. Merge this mod's `BepInEx/` folder into the same place.
3. Open `run_bepinex.sh`, set `executable_name="DragNWash"`, save, and run `chmod +x run_bepinex.sh`.
4. In Steam, game properties → Launch options: `./run_bepinex.sh %command%`
5. Start the game. Change the language in **Options → Language (Mod)**.

</details>

Fonts need no extra setup: the game text uses SteamOS's Noto Sans CJK straight from the font file (Japanese, Chinese and Korean faces), and the F1 menu draws with a bundled Noto Sans JP (`dragnwash-menufont.bundle`), because Steam's Linux runtime exposes no CJK font to Unity's menu system. That menu font has no Hangul or Hebrew, so on the Deck those languages show their locale code on the F1 language buttons; the game itself displays them normally.

To change language on the Deck, use **Options → Language (Mod)** with the controller; no F1 key is needed. For the translator tools in the F1 menu (Gaming Mode):

- Bind **F1** to a button with Steam Input to open it.
- Point with the right trackpad or the touchscreen. **A**, **R2**, or a trackpad click presses the button under the pointer (Steam Input sends the trackpad click as a stick press, not a mouse click, so the mod handles it).
- Hold one of those buttons on the title bar to move the window, or on the bottom-right corner to resize it.
- The sticks and the d-pad scroll whichever list the pointer is over.

> [!WARNING]
> **macOS does not work at the moment.** Drag'n Wash is built with Unity 6.3, and the Doorstop loader that BepInEx 5.4.23.5 uses on macOS cannot hook Unity 6.3 games yet ([NeighTools/UnityDoorstop#108](https://github.com/NeighTools/UnityDoorstop/issues/108)). Doorstop loads into the game, but BepInEx never starts: no `BepInEx/LogOutput.log` and no `BepInEx/config` appear, and the game runs in English. This was checked on an Apple M3 Pro with macOS 26.6, both natively and under Rosetta. It is a BepInEx-side problem, so nothing in this mod can work around it; once a BepInEx release carries the fix, macOS will be tested again.
>
> An **experimental** macOS install script is kept in the repository for that day: [`installer/experimental/install-macos.sh`](installer/experimental/install-macos.sh). It does the same job as the Steam Deck script (BepInEx for macOS, the mod, the language, and the Steam launch option), plus a **Check** mode that tells whether the mod loaded after you start the game. It has not been run on a Mac yet, warns about the issue above before installing, and is not in the release zip.

### Manual installation

### What you need

- The Windows Steam version of Drag'n Wash
- [BepInEx 5 for 64-bit Windows (Mono)](https://github.com/BepInEx/BepInEx/releases)
- The latest `DragNWashLocalization-<version>.zip` from this repository's [Releases page](https://github.com/TomXV/dragnwash-localization/releases)

> [!IMPORTANT]
> Download the file named `DragNWashLocalization-<version>.zip` from the release assets. GitHub's automatically generated **Source code** archives are not installable mod packages. If the Releases page does not contain a mod ZIP yet, an installable build has not been published.

### 1. Open the game folder

In Steam, right-click **Drag'n Wash**, then select **Manage → Browse local files**. This opens the game root: the folder containing the game's `.exe`.

### 2. Install BepInEx

Download the BepInEx 5 archive for **Windows x64 (Mono)** and extract it directly into the game root.

After extraction, `winhttp.dll`, `doorstop_config.ini`, and the `BepInEx` folder should be next to the game's executable. If they are inside another nested folder, move them up to the game root.

Launch the game once, wait until the title screen appears, and close it. BepInEx will create its configuration and log files. Confirm that `BepInEx/LogOutput.log` now exists before continuing.

### 3. Install Drag'n Wash Localization

Download `DragNWashLocalization-<version>.zip` from [Releases](https://github.com/TomXV/dragnwash-localization/releases) and extract it into the **same game root**. Allow your archive tool to merge the included `BepInEx` folder.

The plugin DLL should end up at:

```text
<Drag'n Wash folder>/BepInEx/plugins/DragNWashLocalization/DragNWashLocalization.dll
```

Do not leave the ZIP itself or an extra `DragNWashLocalization-<version>` directory between `plugins` and the DLL.

### 4. Launch and verify

Start Drag'n Wash. A manual install starts in Japanese (the installer uses the language you picked).

To change language, open **Options** and use **Language (Mod)** at the end of the Gameplay section. Picking a language switches the game to it right away; press **Save** to keep it, or **Back** to return to the saved language. It works with a mouse or a gamepad, including on the Steam Deck. The **F1** menu's **Tools** tab can also switch language, and saves the choice at once.

A successful installation also produces a `DragNWashLocalization` startup entry in `BepInEx/LogOutput.log`.

To change the default language manually, close the game and edit:

```text
BepInEx/config/com.tomxv.dragnwash.localization.cfg
```

Set `TargetLocale` under `[General]` to an installed locale such as `ja` or `zh-Hans`, then start the game again. `en` keeps the game's original English text (the mod stays installed but translates nothing); the installer, Options → Language (Mod) and the F1 menu offer the same choice.

### If the mod does not load

- Confirm that both BepInEx and the mod were extracted into the folder containing the game executable.
- Confirm the exact DLL path shown above.
- Open `BepInEx/LogOutput.log`. If the file does not exist, BepInEx itself is not loading. If it exists, search it for `DragNWashLocalization` and review the nearby error.
- If opening Options causes a Direct3D 12 crash, use the Windows workaround described in [Crash when opening Options on Windows](#crash-when-opening-options-on-windows).
- If you previously installed a translation that overwrites the game's files (for example files copied into `DragNWash_Data`), the game's English is gone and this mod finds nothing to translate. Restore the original files first: right-click the game in Steam → **Properties** → **Installed Files** → **Verify integrity of game files**, then install this mod again. Such translations also stop working, or break, when the game updates; this mod does not change any game files.

## Language packs

The translation files were written by TomXV and ship in the same zip; contributors who improve a pack are credited in its row below (see [Credits for contributors](CONTRIBUTING.md#credits-for-contributors)). The installer and the F1 menu list every folder under `Translations/`.

| Locale | Language | Status |
| --- | --- | --- |
| `ja` | 日本語 | Supervised by the author |
| `zh-Hans` | 简体中文 | Supervised by the author |
| `zh-Hant` | 繁體中文 | Provisional, converted from the supervised Simplified Chinese with Taiwan wording |
| `de` | Deutsch | Provisional |
| `fr` | Français | Provisional |
| `es` | Español | Provisional |
| `pt-BR` | Português (Brasil) | Provisional |
| `ko` | 한국어 | Provisional |
| `ru` | Русский | Provisional |
| `pl` | Polski | Provisional |
| `he` | עברית | Provisional, drawn right to left |
| `eo` | Esperanto | Provisional, just for fun |
| `tok` | toki pona | Provisional, just for fun (a 137-word language, so expect it to be loose) |
| `en` | English | The game's original text (no translation) |

> [!NOTE]
> **Provisional** packs were not reviewed by a native speaker. They are complete and playable, but some lines may read unnaturally or miss a joke. Only Japanese and Simplified Chinese were supervised by the author. If you are a native speaker, corrections are very welcome as pull requests (see [CONTRIBUTING.md](CONTRIBUTING.md)); every `strings.csv` starts with a comment saying the same.

## For translators

You can add a translation by editing `Translations/<locale>/strings.csv`. The published file has the columns `key,section,node,order,speaker,translation`: `key` is a hash of the English line, `section`/`node`/`order` say where in the game it is played (level and conversation, in play order), and `speaker` says who says it. Lines starting with `#` are section headers such as `# ===== Level 1: Ryan (Sunny) =====`, so the file reads like a script from top to bottom. The recommended way to work is:

1. In the game, open **F1 → Tools → Export working copy**. This writes `Translations/_discovered/<locale>.working.csv` with the English text beside every line (`key,section,node,order,speaker,source_en,translation`), in the order the lines are played, with the same section headers.
2. Edit the `translation` column. Saving the file hot-reloads it into the running game.
3. Before committing, press **F1 → Tools → Hash for commit** (or run `tools/hash-strings.ps1`). This regenerates `strings.csv` without any English text.

Each language folder also holds a one-line `name.txt` with the language's display name (for example `日本語`), shown in the installer and the in-game menu.

The game's English script is intentionally not included in this repository. This ensures that **only people who own the full game can create translations**. You do not need to know Unity's internal keys or write any code. See [CONTRIBUTING.md](CONTRIBUTING.md) for detailed instructions.

If the source text contains formatting tags such as `<size=70%>`, preserve the tag structure and translate only the text inside it.

### Exporting all dialogue for context

After installing the plugin, load a save and press **F6** in the game. The plugin exports all dialogue to:

`BepInEx/plugins/DragNWashLocalization/Translations/_discovered/dialogue_lines.csv`

The export has been verified with 1,839 lines on an actual game installation.

Lines appear in the order in which they are played in the game. The `node` column identifies each conversation and uses names such as `Alexander_2_intro`, following the pattern "character name_occurrence_scene." The `order` column gives the line's position within that conversation. The `kind` column distinguishes character dialogue (`line`) from player choices (`option`). This context makes it easier to understand who is speaking and what each response refers to. The three dragons in the game are Conrad, Ryan, and Alexander.

Copy the lines you want to translate into `Translations/<locale>/strings.csv`, fill in the `translation` column, and submit a pull request. Extra columns such as `node` and `key` may be left in place; the plugin will still load the file correctly.

Already translated lines are exported with their translations filled in, so exporting again will not discard your work.

### Previewing edits without restarting the game

When you save `Translations/<current-language>/strings.csv` or the working copy `_discovered/<locale>.working.csv` while the game is running, the plugin reloads it automatically after about two seconds and immediately updates text that is currently visible. Each changed line is listed in the F1 Activity log.

This lets you edit a translation and check it in the game repeatedly without restarting. You can disable this behavior with `[Debug] HotReloadTranslations`. Successful reloads appear as `[reload]` entries in the F1 Activity log.

### Exporting all UI text

Press **F7** to export all loaded UI text to:

`Translations/_discovered/ui_texts.csv`

The export includes **hidden menus**, so you can collect every UI string in the current scene without opening the pause menu or confirmation dialogs. Its columns are `key`, `source_en`, `translation` (the existing translation, if any), and `object_path` (where the text appears in the UI).

Press F7 once on the title screen and once during gameplay to collect nearly all UI text.

Untranslated UI text is also recorded automatically during gameplay in `Translations/_discovered/strings.csv`. This file is cleaned up each time the game starts: translated entries and duplicates are removed, leaving an up-to-date list of remaining work.

### Strings that do not need translation

Slider values, resolutions such as `1920 x 1080 @ 164.995Hz`, build numbers, and similar strings are excluded from discovery by default.

You can add exclusion patterns to [Translations/ignore.txt](Translations/ignore.txt). The file uses regular expressions and includes examples.

Exclusions affect discovery only. Translation lookup happens first, so any entry present in `strings.csv` will always be translated even if it matches an exclusion pattern.

### In-game debug menu

Press **F1** to toggle the debug window. The key is configurable. Drag the title bar to move the window, and drag the lower-right corner to resize it.

- **Activity log:** Displays translation results and processing logs. `Follow: ON/OFF` controls automatic scrolling to the latest entry; scrolling manually disables following. `Clear log` clears the display and resets duplicate suppression. The log keeps the 100 most recent entries.
- **Tools:** Switch the language without restarting (the buttons show each language's name from `name.txt`; **English** turns translation off). Export dialogue (`Export loaded dialogue`), UI text (`Export UI text`), or the working copy with English beside each line (`Export working copy`), rebuild the published file (`Hash for commit`), and run the layout check.
- **Saves:** Restore an earlier save, step the level index, or toggle save flags. See below.
- **About:** The version and build the mod is running, who made it, the license, and what this session loaded. Useful to quote in a bug report.

The **Check translation layout** button exports strings at risk of overflowing their layout to `Translations/_discovered/layout_risks.csv`. Configure the threshold with `BepInEx/config/.../LayoutOverflowThreshold`; the default is `1.0`, meaning an exact fit.

### Restoring the previous save for translation testing

Whenever the game writes a save, the plugin stores a versioned copy in:

`BepInEx/plugins/DragNWashLocalization/SaveHistory/<slot>/`

It keeps 30 versions per slot by default. You can change this with `[Debug] SaveHistoryKeep`.

Open **F1 → Saves**, select a slot, and click **Restore** on the version you want. Then return to the title screen and load that slot for the restored save to take effect. Saving again during gameplay will overwrite the active save as usual.

The plugin automatically preserves the state from immediately before a restore, so you can recover if you go back too far.

Use this feature to revisit the same scene while comparing revisions of a dialogue translation. Restore replaces the game's own save file without editing flags or variables.

The same tab also has a **PROGRESS** editor: step the level index back or forward with **-** / **+** and press **Apply**. Moving forward asks for confirmation because it can spoil content you have not seen. **Flags...** lists every event flag the game is known to use, grouped (level flow, story, romance, scene triggers, scene watched, items) with a short description, whether or not the save has set it yet. Click a value to cycle unset → true → false, type in the search box to filter, and use **Reset all to false...** to wipe every flag (the level index is kept). The list comes from `FlagCatalog.csv` next to the plugin DLL, so you can add rows for flags found later. Every edit snapshots the save first.

## Crash when opening Options on Windows

A crash in `D3D12ScratchAllocator::DestroyScratch` has been observed when opening Options with Unity 6000.3.14f1 and DirectX 12. Unity has an official issue report with the same stack trace: [UUM-140564](https://issuetracker.unity.com/issues/10698). Because this is a native rendering bug, it cannot be prevented by catching exceptions in the translation hook.

The bug is triggered when textures are allocated or uploaded at runtime. On Direct3D 12 the plugin therefore prepares the fonts of every installed language at startup, so neither gameplay nor switching language from Options or the F1 menu adds anything to a font atlas. Each character is rasterized only into the one font its language uses, which keeps that startup work small. This has been verified on an actual system, including repeated language switches. No configuration is required.

Other graphics APIs (Direct3D 11, Vulkan on the Steam Deck) handle runtime uploads, so there only the language in use is prepared and the others load when you pick them. Set `[Font] PreloadAllLocales = true` to prepare everything at startup there too.

If the game still crashes, open Steam and go to **Drag'n Wash → Properties → General → Launch Options**, add `-force-d3d11`, and restart the game. This bypasses the issue by switching the graphics API. The option is part of [Unity's standard command-line arguments](https://docs.unity3d.com/6000.3/Documentation/Manual/PlayerCommandLineArguments.html), and it does not modify the game's DLLs or save data.

The plugin startup entry in `BepInEx/LogOutput.log` reports the graphics API currently in use as `graphics=...`.

Lowering `[Font] AtlasPointSize` in `BepInEx/config/com.tomxv.dragnwash.localization.cfg` reduces the number of font atlases. Raising it produces sharper text. The default is 80.

## Current status

Released as v0.6.1 (fixes names and other untranslated text showing backwards in Hebrew). v0.6.0 added per-line translations (English said by several characters can be translated differently for each of them; checked with the game update of September 14, 2026). v0.5.0 added changing language from the game's own Options screen; v0.4.0 brought thirteen languages, per-language fonts, an About tab and an installer in English, Japanese and Chinese; v0.3.0 added Steam Deck support. Windows on ARM has been verified too (the game itself needs `-force-d3d11` there). macOS does not work at the moment because of a known BepInEx-side issue (see the note under [Steam Deck / Linux](#steam-deck--linux-verified)). The BepInEx plugin skeleton, Japanese and Chinese replacement of UI and dialogue text, CJK font rendering, bulk dialogue and UI export, in-game debug menu, layout overflow detection, translator documentation, and release workflow have all been implemented and tested in the game.

See [docs/PLAN.md](docs/PLAN.md) for details.

## Roadmap: v1.0.0 and Drag'n Wash ModFramework

The next major version, **v1.0.0**, will rebuild this mod on top of a new prerequisite mod, **Drag'n Wash ModFramework** (working name).

- **What the framework is for.** Much of what this mod does to hook into the game is useful to other mods as well: adding settings to the game's Options screen, an in-game menu, rewriting text before it is shown, dialogue and choice events, loading assets safely on Direct3D 12, and an installer. The framework will offer these to any mod through an API, so each mod does not have to patch the game on its own.
- **Why.** When the game updates, only the framework has to follow the change, and the mods built on it keep working. The update of September 14, 2026 is a good example of the kind of change that would be absorbed in one place.
- **This mod becomes its first user.** Drag'n Wash Localization will be moved onto the framework piece by piece. Because the inner workings change so much, that release will be v1.0.0.
- **For translators.** The goal is to keep the CSV format and the translation tools as they are, so existing packs and contributions carry over.
- **Until then.** Fixes and translation updates continue as 0.6.x releases.

There is no release date yet. If you make mods for Drag'n Wash and have ideas for what the framework should provide, please open an issue.

## Contributing translations

No code is required. Edit `Translations/<locale>/strings.csv` to contribute a translation.

See [CONTRIBUTING.md](CONTRIBUTING.md) for the workflow, file format, and instructions for finding untranslated strings.

## Distribution and releases

See [docs/RELEASING.md](docs/RELEASING.md) for instructions on building and distributing the release ZIP.

Because the game-derived reference assemblies cannot be committed, releases are built locally and uploaded to GitHub Releases.

## A note to the developers

This is an unofficial fan project and is not affiliated with Gator Dragon Games. It contains no game assets and no script text: English lines are stored only as SHA-256 hashes, and the game's files are never modified (BepInEx loads the plugin at runtime). If you are a member of the development team and have any concerns, please open an issue on this repository or contact the maintainer, and the project will be adjusted or taken down as you prefer.

## License

See [LICENSE](LICENSE) for the plugin's code license. This repository does not include assets or code from the game. Translations are treated as contributions from their respective translators.
