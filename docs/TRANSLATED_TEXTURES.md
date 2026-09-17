# Translated textures

[日本語](TRANSLATED_TEXTURES.ja.md)

Written 2026-09-18 on the `experimental/translated-textures` branch. **Built on that branch (framework Assets 1.2.0 on `experimental/language-textures`), not tested in the game yet.** From [#4](https://github.com/TomXV/dragnwash-localization/issues/4): some of the game's text is in pictures (menu buttons, the loading screen's door sign, the signs on the walls).

## What changed

Under Drag'n Wash ModFramework's [content policy](https://github.com/TomXV/dragnwash-modframework/blob/main/docs/CONTENT_POLICY.md), this repository may carry textures **drawn by hand or changed from the game's**, such as a sign painted over in another language. The game's own images, unchanged, are never included.

## For players

- Pictures with text show in the chosen language, where a language has them. Otherwise the game's picture stays.
- **Options → Mods → Drag'n Wash Localization → Settings → Translate pictures** (on by default) turns them off.
- Changing the language changes the pictures at once. On **Direct3D 12**, loading a picture while the game runs can crash it (Unity UUM-140564), so there the pictures change the next time the game starts. The setting's description says so, and the log notes it on each change.

## In the repository

```
Translations/
  ja/
    strings.csv
    textures/
      MenuButtons0001.png      ← named after the game texture it replaces
      Reception.png
      credits.csv              ← who made each picture
```

- **Name.** The file is named after the game texture, as the framework's Assets tab lists it. The size should match the original (a sprite keeps its rect).
- **credits.csv.** `file,author,note`, one row per PNG. `note` says what was done (`drawn from scratch`, `game texture repainted`). Every PNG needs a row.
- **Format.** PNG, at most 4096×4096 and 8 MB each.
- **Not allowed.** A PNG that is the game's texture unchanged.
- **Checks in CI** (`tools/check-translations.py`). Only PNGs and credits.csv in `textures/`; each PNG is a real PNG within the size limits; every PNG has a row in credits.csv with an author and a note, and every row has a PNG. Once the framework's asset fingerprints exist, a PNG identical to an untouched export is refused too. Whether a name matches a game texture is not checked here: the Assets tab lists a picture that applied nowhere.
- **Release.** `tools/pack.ps1` copies `textures/` with each locale.
- **Credits.** The README's credits name the artists per language, as for translators.

## In the framework (Assets library)

The framework replaces textures by name from `<mod>/assets/textures/`, for every language alike. Translated pictures need a set per language. New:

- `AssetReplacements.AddLanguageFolder(string guid, string root, string subfolder)`: `root/<language>/<subfolder>/*.png` are replacements that apply only while `GameFonts.Language` is that language. Localization calls `AddLanguageFolder(guid, <plugin>/Translations, "textures")`.
- `AssetReplacements.SetLanguageFoldersEnabled(string guid, bool on)` for the setting.
- **Only the language in use is loaded.** At startup, the current language's files. On a language change:
  - where runtime uploads are safe, the new language's files are loaded, the previous language's replacements are taken back, and the new ones applied;
  - on Direct3D 12, nothing is loaded; `AssetReplacements.PendingLanguage` names the language that will apply after a restart, for the mod to show.
- **Taking back.** Today a replacement never needs undoing. Switching languages and the setting do, so the library remembers, for each material property and each sprite user it changed, what was there before, and puts it back. The original textures stay referenced while they are replaced, so Unity cannot unload them.
- **Clashes.** A language picture and another mod's plain replacement of the same texture: the language picture applies, and both are named in the log and in the Assets tab, as for any clash.
- **Assets tab.** Replacements show their language; **Reload files** also reloads the current language's pictures (not on Direct3D 12, as today).
- **Game updates.** A replacement whose name matched nothing since the game started is listed as unused, so a renamed texture is noticed.

## For translators

1. Turn on developer tools. In the Assets tab, find the texture (the filter box, or **Inspect** from the Inspector).
2. Get the picture to work on (the framework's export, when it exists; until then any tool that reads the game's textures on your own machine) and draw the translated version.
3. Save it as `BepInEx/plugins/DragNWashLocalization/Translations/<locale>/textures/<name>.png`, press **Reload files** (Direct3D 11 or Vulkan; add `-force-d3d11` on Windows), and check it in the game.
4. Add a row to `credits.csv`, and open a pull request with the PNG and the row.

## Order of work

1. Framework: language folders, taking back, pending language, Assets tab column (Assets library, minor version).
2. Localization: the setting (`[General] TranslatePictures`), `AddLanguageFolder`, the note on Direct3D 12, pack.ps1, CI checks, CONTRIBUTING section. With an Assets library older than 1.2.0 the mod runs without pictures.
3. Test with one picture on Direct3D 11, Vulkan (Steam Deck) and Direct3D 12 (restart path).
4. First pictures, from contributors.
