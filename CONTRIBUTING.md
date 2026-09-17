# Contributing

[日本語](CONTRIBUTING.ja.md)

Drag'n Wash Localization lets you contribute a translation by **editing CSV files only**, with no code. This guide is for translators. Build and release steps for the plugin itself are in [docs/RELEASING.md](docs/RELEASING.md).

## What you need

- Drag'n Wash (Steam) with this mod installed (BepInEx)
- Any editor that can handle CSV (Excel, LibreOffice, VS Code, ...)

You do not need to know Unity's internal keys or how to program. While working you write the **exact English text shown in the game** in a `source_en` column, and convert it to a hash before committing (see "Hash before committing" below).

### Language display name (`name.txt`)

Whatever you put in `Translations/<locale>/name.txt` is shown as the language's name in the game's **Options → Language (Mod)** list, on the language buttons in the F1 menu, and in the installers (Windows and Steam Deck) (for example `ja/name.txt` → `日本語`, `zh-Hans/name.txt` → `简体中文`). One line, UTF-8. Without the file the folder name is shown. Adding a language is just a folder, a `strings.csv` and a `name.txt`.

## Basic flow

1. Fork this repository.
2. Add or fix translations in `Translations/<locale>/strings.csv`.
3. Commit and open a pull request (see [Writing the pull request](#writing-the-pull-request) for the title and description).

## File format

> [!IMPORTANT]
> **The CSV notation changed in v0.6.0.** Files from earlier versions still load, but note the differences when you work on a current file:
> - **Line-ID rows:** besides the 16-digit hash, the `key` column can hold a Yarn line ID such as `line:6046bedf`. Such a row translates that one line only (see [Lines said by more than one character](#lines-said-by-more-than-one-character)).
> - **Several speakers:** the `speaker` column of a line said by more than one character lists all of them, separated by `/` (`Ryan/Alexander`).
> - **Working copy:** it contains extra line-ID rows with an empty translation at every shared line. Leave them empty unless you want that line to differ.
> - **`order`:** it now counts every line in a conversation, so the numbers differ from files made before v0.6.0. The column is only for reading; nothing to fix.
>
> Use the v0.6.0 plugin or later and the tools in this repository for translation work. The v0.5.0 tools do not know line-ID rows and turn them into broken hash rows. Players on older versions are not affected: they simply ignore line-ID rows.

`Translations/<locale>/strings.csv` is the published file. Rows are ordered **the way the game plays them** and grouped under `#` headers. While working, rows with the English text may sit **in the same file**.

```csv
key,section,node,order,speaker,translation

# ===== Level 1: Ryan (Sunny) | sets level_1 | ends level_1_complete =====
# --- intro: Ryan_1_intro ---
5d0a…,L01 Ryan,Ryan_1_intro,1,Ryan,Hi there. Is this the cleaning place?
# --- phone: Ryan_1_PhoneTutorial | if $has_talked_to_ryan ---
…
# ===== UI and other text (not part of the dialogue script) =====
d0db8b5e364b6989,UI,,,UI,Options
```

- `section` … level number and dragon such as `L01 Ryan`, or `Cutscene` / `Reaction` / `Unused` / `UI`
- `node` / `order` … the Yarn conversation node and the line's position in it. Branch nodes come right after their parent
- `#` lines … headers. They are ignored when loading, so leave them in. `| if $variable` is a hint about the branch condition
- The order comes from `data/script_order.csv` (node names, line ids, hashes and speakers only, no English) and is regenerated in-game with **Export game flow** (the maintainer does this when the game updates)

```csv
source_en,translation
Options,Optionen
```

- `key` … the first 16 hex digits of the SHA-256 of the source text. **Only this form is committed.** The repository never carries the game's English script, so nobody without the game can read the script or translate without the source in front of them.
- `speaker` … who says the line (Conrad / Ryan / Alexander / Kobold = the player's choice / Phone / UI). Filled in automatically from the script structure; English said by several characters lists all of them (`Ryan/Alexander`).
- A `key` can also be a Yarn line ID such as `line:6046bedf`. Such a row translates that one line only; see [Lines said by more than one character](#lines-said-by-more-than-one-character).
- `source_en` … the exact English text shown in the game (matched exactly). **Use this while working**: saving the file hot-reloads it into the running game.
- `translation` … your text.

The plugin hashes the English text it is about to show and looks it up, so both row forms behave the same. The F6 / F7 exports carry both `key` and `source_en`, which is how you can map one to the other.

### Starting a new language

1. Create `Translations/<locale>/` (for example `ko`) and write the display name into `name.txt` (for example `한국어`).
2. Turn on the developer tools: **Options → Mods → Drag'n Wash ModFramework → Developer tools** (they are off by default, so players never see the F1 window, the exports or the `_discovered` folder). Start the game and pick the new language in **Options → Language (Mod)** or under F1 → Translation (the screen stays English, there are no translations yet).
3. Press **F1 → Translation → Export working copy**. Even without a `strings.csv` you get an empty working copy, `_discovered/<locale>.working.csv`, listing every line the game has loaded with its English text.
4. Continue as in "Working with the English beside each line". Lines show up in the game as you translate them.
5. Translate the Options row label `Language (Mod)` too (key `e3becbaee46cc0df`). Keep "(Mod)" or your language's equivalent so players can tell it is this mod's setting, not the game's. It appears in the working copy and in the F7 export once you have opened Options.

### Working with the English beside each line (recommended)

**F1 → Translation → Export working copy** expands the published `strings.csv` into `Translations/_discovered/<locale>.working.csv`:

```csv
key,section,node,order,speaker,source_en,translation
5d0a…,L01 Ryan,Ryan_1_intro,1,Ryan,Hey. This the cleaning place?,…
```

Conversations are in **play order** and the `speaker` column says **who is talking** (Conrad / Ryan / Alexander, Kobold for the player's choices, Phone for calls from head office, UI for interface text), which helps keep each character's voice consistent. `source_en` is filled from the script and UI the game currently has loaded (load a save first so all dialogue is present). Edit and save this file and hot reload shows the result immediately. It lives under `_discovered/`, so it never goes into the repository.

Running inside the game is the ownership check; there is no separate login.

### Lines said by more than one character

A row keyed by hash translates **every** line with that English. A few short lines are said by different characters: `Wonderful!` is Ryan's in level 1 and Alexander's in level 5. Such a row lists every speaker, for example `Ryan/Alexander`, and one translation then has to fit all of them.

When it cannot, give a line its own translation. The working copy puts an empty row keyed by the Yarn **line ID** at every place a shared line is spoken:

```csv
key,section,node,order,speaker,source_en,translation
84f325bca745e504,L01 Ryan,Ryan_1_intro,9,Ryan/Alexander,Wonderful!,Wonderful translation for everyone
line:6046bedf,L01 Ryan,Ryan_1_intro,9,Ryan,Wonderful!,Ryan's own translation
line:ab423ac7,L15 Alexander,Alexander_5_required,19,Alexander,Wonderful!,
```

- Fill in only the line rows you want to differ. A line row with a translation wins for that one line; every other place keeps the hash row.
- Empty line rows are fine to leave: *Hash for commit* publishes only the ones you filled in, at their place in the script.
- Line IDs come from the game's script. If a game update changes one, that line falls back to the hash row. If an update edits a line's English instead, the line ID still finds the row (experimental, not yet in a release): the translation keeps showing, and the line is listed for review in the log and the F1 window, because the English it was written for has changed.
- Line rows work for dialogue and options, not for UI text.

### Hash before committing

Rebuild the published `strings.csv` before opening a pull request. It is generated from the working copy (`_discovered/<locale>.working.csv`) when one exists, otherwise from the `source_en` rows in `strings.csv` itself. Two ways:

- In the game: **F1 → Translation → Hash for commit** (rewrites the current language's file)
- `tools/hash-strings.ps1` with no arguments (all languages)

`-Path` is different: it converts exactly the files given, in place, and does **not** look for a working copy. Use it on a published `strings.csv`; passing a working copy overwrites it with the published form, losing its `source_en` column and every untranslated row.

**A `strings.csv` that still contains English is not accepted.** Every pull request is checked automatically, and when the format is wrong a comment explains why in English. Push a fix and the same comment is updated.

Wrap fields containing commas, quotes or line breaks in `"` (escape quotes as `""`), per [RFC 4180](https://datatracker.ietf.org/doc/html/rfc4180).

### Formatting tags

When the source contains TextMeshPro tags such as `<size=70%>`, `<gradient="gold">` or `<i>`, **keep the tag structure and translate only the text inside**. Broken tags break the display.

```csv
"<gradient=""gold""><b> ...English... </b></gradient><size=70%> (hint)","<gradient=""gold""><b> ...translation... </b></gradient><size=70%> (translated hint)"
```

## Finding untranslated text

Three ways to collect source text while playing. All write CSV files under `Translations/_discovered/`.

| How | File | Content |
|---|---|---|
| **F6** | `_discovered/dialogue_lines.csv` | Every dialogue line in **play order** (`yarn_project,node,order,kind,speaker,line_id,key,source_en,translation,tags`). Press it after loading a save |
| **F7** | `_discovered/ui_texts.csv` | Every UI text, hidden menus included (`key,source_en,translation,object_path`) |
| automatic | `_discovered/strings.csv` | Untranslated text seen while playing (`source_en,translation`) |

`_discovered/` contains the game's own copyrighted text, so **never commit it** (it is in `.gitignore`). Everyone generates it locally.

- Copy the rows you want from `dialogue_lines.csv` into `strings.csv` and fill in `translation`; extra columns such as `node` and `line_id` are fine.
- `node` is "character_visit_scene" (for example `Conrad_1_intro`), `order` is the position in that conversation, and `kind` is `line` (dialogue) or `option` (the player's choice). Translating one conversation at a time keeps tone and context consistent. The dragons are Conrad, Ryan and Alexander; suffixed names such as `RyanMuddy` are the same character in a different state.
- Lines already translated are exported with their translation filled in, so re-exporting never loses work.
- `object_path` in `ui_texts.csv` tells you where on screen a string lives. F7 once on the title screen and once in a level covers nearly all UI.

## Text that does not need translating

Slider values, resolutions (`1920 x 1080 @ 164.995Hz`), build numbers and the like are excluded from discovery from the start. Add more exclusions as regular expressions in `Translations/ignore.txt` (examples inside).

Exclusions only affect discovery. Lookup happens first, so a row in `strings.csv` is **always translated** even when it matches an exclusion pattern.

## Adding a locale

Add a folder under `Translations/<locale>/` with a `strings.csv` (and a `name.txt`). Use a BCP 47 style name like the existing ones (`ja`, `zh-Hans`, `zh-Hant`, `pt-BR`, `ko`). The plugin detects folders automatically.

Fonts are chosen from the characters in your file, so most scripts need nothing extra: Japanese, Chinese (Simplified and Traditional), Korean, Cyrillic, accented Latin and Hebrew all have a font on Windows and the Steam Deck. Right-to-left languages (`he`, `ar`, `fa`, `ur`, `yi`) are drawn right to left automatically; keep the file in normal typing order and avoid Latin words or digits inside a line, because those come out reversed. For a script the plugin has no font for, put a `.ttf`/`.otf` in a `fonts/` folder next to the plugin DLL.

## Improving a provisional language

Every pack except Japanese and Simplified Chinese is provisional: complete, but not reviewed by a native speaker. If you speak one of them, a review is the most valuable contribution there is. Fix lines in a pull request; once a whole pack has been read through by a native speaker, update the comment at the top of its `strings.csv` and its row in the README's language table in the same pull request. If that feels like too much, just say in the pull request that the whole pack was reviewed, and the maintainer will update both.

If you only change the `translation` column of the published `strings.csv` and leave the other columns as they are, the file stays hashed and needs no *Hash for commit*. To see the English next to each line while you review, use the working copy described in [Working with the English beside each line](#working-with-the-english-beside-each-line-recommended).

## Checking your work

- Switch languages in **Options → Language (Mod)** (Save keeps the choice, Back returns to the saved language) or on the **Translation** tab of the **F1** debug window; the screen updates without a restart.
- **Check translation layout** on the Translation tab writes strings at risk of overflowing to `_discovered/layout_risks.csv` (`source_en,translation,axis,required_px,available_px,ratio,object_path`). A larger `ratio` means more overflow; shorten the translation or rephrase.

## Before opening a pull request

- `source_en` matches the on-screen English **exactly** (case, surrounding spaces, formatting tags).
- **Hashed**: `strings.csv` has the columns `key,section,node,order,speaker,translation` and no `source_en` rows remain.
- Tag structure matches the source.
- No duplicate rows, no rows with an empty `translation`.
- One language and a coherent scope per pull request.

## Writing the pull request

- **Title:** start with the locale code in brackets, then say what changed. For example `[ko] Fix the Korean translation`, `[ko] Native review of levels 1-3`, or `[de] Translate the Options row`. English, Japanese or your own language are all fine.
- **Description:** the pull request template fills in by itself. Under *What*, write the language and which part you changed (levels, scenes such as `Conrad_1_intro`, or UI), then tick the checklist items that apply and leave the rest unticked. Under *Credit*, say whether you want to be credited and, if so, the name (and optional link) to show.
- **After opening it:** the automatic translation check runs. If it fails, a comment in English lists the reasons with file and line numbers, and it updates itself when you push a fix. The maintainer then reviews the pull request. Questions are welcome in the pull request, in English or Japanese. If the check fails, see [If the automatic check fails](#if-the-automatic-check-fails).

## If the automatic check fails

Every pull request runs `tools/check-translations.py`. When it finds a problem, the pull request gets a comment in English that explains the reasons, with a **Full report** listing each problem as `file:line: message`. The line number is the line in the file as you see it in an editor. Push a fix to the same branch and the check runs again; the comment is updated, not duplicated.

You can run the same check before pushing (Python 3.9 or later):

```bash
python tools/check-translations.py
```

It prints `translations OK` when everything passes.

| Message in the report | What it means | How to fix it |
|---|---|---|
| `header is [...]; the published file must be ...` | The file is still a working copy (it has a `source_en` column) | Run **F1 → Translation → Hash for commit** or `tools/hash-strings.ps1`, then commit the rebuilt `strings.csv` |
| `key is not 16 lowercase hex digits or a line ID` | A key is neither a hash nor a line ID: English text was put in the key column, or the key was edited | Rebuild with *Hash for commit*. Never edit the `key` column by hand |
| `must not be committed (contains source text)` | A file from `Translations/_discovered/` or a `strings.local.csv` is in the pull request | Remove it from the pull request with `git rm --cached <file>` and commit; keep the file locally if you still need it |
| `duplicate key (see line N)` | The same line appears twice | Keep one row per key and delete the other |
| `empty translation` | A row has an empty `translation` | Fill it in, or delete the row so the game shows the English |
| `expected 6 fields, got N` | The row has the wrong number of columns | Wrap values that contain `,`, a line break or `"` in double quotes, and write `"` inside them as `""` |
| `section does not look like an identifier` / `node does not look like an identifier` | These columns were edited, or the columns shifted | Put back the values from `main` and change only `translation` |
| `no strings.csv` | A language folder has no `strings.csv` | Add the file, or remove the empty folder |
| `empty file` | `strings.csv` has no header line | Start the file with `key,section,node,order,speaker,translation` |

The check does not compare formatting tags with the source; reviewers look at those.

**Spreadsheet apps can break the file on save.** Excel may turn keys that look like numbers (for example `12345e6789012345`) into scientific notation, change the encoding, or change quoting. Prefer a text editor such as VS Code, or LibreOffice with every column set to *Text*, and save as UTF-8 CSV.

## Translated pictures

Some text is in pictures (menu buttons, signs). A translated picture goes in `Translations/<locale>/textures/<game texture name>.png`, with a row in `textures/credits.csv` (`file,author,note`). Draw it by hand, or change the game's picture; never commit the game's picture unchanged ([content policy](https://github.com/TomXV/dragnwash-modframework/blob/main/docs/CONTENT_POLICY.md)). The steps are in [docs/TRANSLATED_TEXTURES.md](docs/TRANSLATED_TEXTURES.md#for-translators).

## Rules

- Never commit the game's assets or code unchanged ([content policy](https://github.com/TomXV/dragnwash-modframework/blob/main/docs/CONTENT_POLICY.md)). Pictures you drew or changed are welcome (see above).
- Never commit `Translations/_discovered/`.
- Translations are credited to their translators (see [LICENSE](LICENSE)).

## Credits for contributors

Say in the pull request's *Credit* section whether you want to be credited, and under which name. If you do, the maintainer adds the credit in a separate commit after merging, so you do not need to change anything for it. The credit appears in:

- the language's row in the README's language pack table
- the comment at the top of the pack's `strings.csv`
- the notes of the release that includes your change
- the About screens of the in-game F1 menu and the installer (from the next release on)

How the pack's status is written (for example whether a partial review changes "provisional") is decided per pull request. If you would rather not be credited, nothing is added; your commits still show in the repository history.
