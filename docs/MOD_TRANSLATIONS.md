# Translating other mods' text

[日本語](MOD_TRANSLATIONS.ja.md)

Written 2026-09-17. **Built on the `experimental/mod-translations` branch on 2026-09-19 (beta, experimental, off by default); not tested in the game yet. The notice on the Mods screen waits for the framework's notice area.** It started from [issue #28](https://github.com/TomXV/dragnwash-localization/issues/28): can the text of a mod that adds mechanics, UI and dialogue be translated?

## In short

- A mod's translations **ship with that mod**, in `BepInEx/plugins/<the mod>/Translations/<locale>/strings.csv`, in the same format as this mod's `strings.csv`.
- After its own language pack, Localization reads the packs of the other loaded mods. **A mod's pack cannot replace a line of the game's own pack**; a disagreement is recorded as a conflict.
- This repository does not carry other mods' translations. It would have to follow every update of every mod, which is the opposite of keeping each part small.
- **It ships as an experimental beta feature, off by default.** How it behaves with other mods is unknown territory, so players turn it on knowing that ([below](#as-an-experimental-beta-feature)).
- **Machine translation is not built in**, for the reasons [below](#why-machine-translation-is-not-built-in). A mod's author or translators may ship a translation made any way they like, marked provisional.

## What works today (checked in the code)

| Question | Today |
|---|---|
| Can another mod's UI text be translated? | **Yes, when it is shown with TextMeshPro and a row exists.** Lookup is one dictionary, "hash of the English shown → translation" (`TranslationStore`), consulted by the framework's Text library whenever a TMP `text` is assigned, `SetText(string)` is called or the text is enabled. Where the text came from is not looked at. **Legacy uGUI `Text`, text a mod draws itself with IMGUI (`GUI.Label` and so on) and the formatting overloads of `SetText` are not covered.** |
| Is it found on its own? | Through the same TMP paths, untranslated strings are recorded in `_discovered/strings.csv` and included in the F7 export. For dialogue, F6 exports **every loaded YarnProject**, so a mod with its own YarnProject is included. Dialogue a mod writes straight into TMP without Yarn is picked up as UI text. |
| When the mod changes its English | The hash changes and the line falls back to English. `line:` rows survive while the Yarn line ID stays. The "find it again by similar English" recovery (`LineResolution`) covers the game's script only. |
| Can it crash? | Lookup is a dictionary hit, so it is unlikely by design. **Not yet tried with a real mod.** |
| What is missing | Translations can only live in this mod's `Translations/`. The only way to add another mod's lines is this repository's `strings.csv`. |

## As an experimental beta feature

How another mod's text is shown differs from mod to mod, and we do not test them all. So this ships as an **experimental beta feature** that players turn on knowingly.

- Setting: `[Experimental] Translations from other mods` in **Options → Mods → Drag'n Wash Localization → Settings**. **Off by default.** While it is off, no other mod's folder is read at all.
- Its description: "Beta, experimental. Loads translations shipped by other mods. Combinations with other mods are untested: text may look wrong or translations may not match. Turn it off if something goes wrong."
- When it is on, a warning is logged once, listing the mod packs that were read.
- F1 → Translation and About say it is experimental.
- Release notes and the README describe it under an "Experimental (beta)" heading.
- Bug reports are welcome, but no combination with a particular mod is guaranteed. A mod is tested only when its author asks (see [Plan for checking](#plan-for-checking)).
- Whether to drop the experimental label is decided again once the design settles and more mods have been tried.

## Layout and format

```
BepInEx/plugins/
  DragNWashLocalization/Translations/ko/strings.csv    ← the game's text (this repository)
  SomeMod/SomeMod.dll
  SomeMod/Translations/ko/strings.csv                  ← SomeMod's text (shipped by SomeMod)
  SomeMod/Translations/ja/strings.csv
```

- The same columns: `key,section,node,order,speaker,translation` (or the shorter `key,speaker,translation` and `key,translation`). `key` is the first 16 hex digits of SHA-256 over the English; `tools/hash-strings.ps1 -Path <file>` makes it as it is.
- `line:<id>` rows work too.
- Locale folders use the names in this mod's `Translations/` (`ko`, `zh-Hans`, ...). A language this mod does not have cannot be chosen, so a folder for it alone is not read.
- No `name.txt` (this mod owns the language names).
- A file that keeps the mod's own English in a `source_en` column is accepted. This repository's rule against shipping English is about the game's script; English a mod author wrote is the author's. The authors' notes must still say plainly that **the game's own English must not be included**.

## Which folders are read

- Only the folders holding the DLLs of plugins BepInEx loaded (`Chainloader.PluginInfos`); no other folder is searched.
- Not this mod's own folder.
- A folder with several plugins is read once.
- At start and when the language changes.

## Order and precedence

1. This mod's language pack (the game's text)
2. Other mods' packs, in GUID order (the same order every time)
3. The translator's working copy (`_discovered/<locale>.working.csv`, only with developer tools on), which wins over everything, as today

Rules:

- **A row from a mod's pack never replaces a row from 1.** Same key, different translation: 1 is used and a conflict recorded (the same translation is simply ignored).
- Two mods with different translations for one key: the one read first is used and a conflict recorded.
- Conflicts go to the log, to a list in F1 → Translation, and to the Mods screen as a notice under each mod involved ("3 translations differ from Drag'n Wash Localization"). The notice needs a per-mod notice in the framework; see [decisions](#decisions).

## Keeping failures contained

- A file that cannot be read (broken CSV, read error, too large, say over 5 MB) is skipped **for that mod only**, with the reason logged once. Other mods and the game's text are not affected.
- The number of rows taken from one mod is capped too (a few times the game's line count).
- All reading happens at start and on a language change; the per-text work stays what it is (a dictionary hit).

## Tools for translators

Only with developer tools on, as today.

- **F1 → Translation** lists the loaded mod packs with the mod's name, row count and conflicts.
- **Hot reload** watches mod packs too.
- **Exports**:
  - F6's `dialogue_lines.csv` already has a `yarn_project` column, which names a mod's YarnProject; no header is added.
  - UI strings get a `mod` column in `_discovered/strings.csv` and the F7 export (`ui_texts.csv`): the name of the plugin that showed the string, found the first time each string is met while developer tools are on (see [decisions](#decisions)). When that does not tell, F7 also asks which mod the components on the label and its parents belong to. Empty means the game, or that it could not be told.
- **Hash for commit** stays for this mod's packs. A mod's pack is made with `tools/hash-strings.ps1 -Path`. Ordering by the script only knows the game's script, so a mod's rows end up together at the end.

## Steps for mod authors (draft)

1. Show your mod's text in English through TextMeshPro's `text` or `SetText(string)`. That alone makes it translatable (IMGUI and legacy uGUI `Text` are not covered).
2. Translators turn developer tools on, export with F7 (UI) and F6 (dialogue), pick your mod's rows (the `mod` column for UI text, the `yarn_project` column for dialogue) and fill in `translation`.
   Dialogue is best keyed by line ID (`line:<id>` rows): those survive when you reword the English, rows keyed by the English text do not.
3. Hash with `tools/hash-strings.ps1 -Path` and ship the result as `Translations/<locale>/strings.csv` with your mod.
4. A translation made by machine or otherwise unreviewed says `Provisional` in its header comment, like this mod's provisional packs.
5. Credits are your mod's.

## Why machine translation is not built in

- **Network**: the framework says it sends nothing but the update check. Translating at run time would send the text being played to an outside service.
- **Keys and cost**: whose API key, and who pays, has no good answer. This mod will not handle keys.
- **Content**: mods include adult content, which can break a translation service's terms.
- **Quality and responsibility**: a wrong translation would appear under this mod's name.
- **Scope**: each part stays small. How a translation is made is for the mod's author and translators to choose.

## Split with the framework

It starts inside Localization, as a folder convention, with no framework API. If another localization mod appears and wants the same convention, finding the folders and recording conflicts moves to the framework's Text library.

## Decisions

Settled on 2026-09-17.

- **Conflicts on the Mods screen: yes.** The framework first gets a small per-mod notice (on its roadmap; `GameHooks.Unavailable` means "a feature cannot work" and is not reused). Localization then leaves a notice under each mod involved in a conflict. With an older framework that has no notices, conflicts still go to the log and F1.
- **Which mod showed a UI string: told only when recording.** When developer tools are on and an untranslated string is recorded for the first time, the stack is walked once to find the first plugin assembly (as the framework's connection watch does), and its mod goes into the `mod` column. Showing text is never slowed down, and with developer tools off nothing is looked at.
- **`LineResolution` for mods' dialogue: no.** It needs script-order data per mod. Mods' dialogue is keyed by line ID instead (`line:<id>` rows keep working when the English changes), and the steps for mod authors say so. A row keyed by English text falls back to English when the English changes.
- **A language this mod does not have: not read.** The language list, names and fonts belong to this mod. A mod's folder for such a language is skipped with one line in the log. To add a language, a pack is added to this repository first.

## Plan for checking

- Other mods are not tested on our own initiative. When a mod's author asks us to check that translation works with their mod, we test that mod: does its UI text reach `_discovered/strings.csv`, does its dialogue appear in F6, does a translation row change what is shown, and does nothing crash.
- What such a test finds goes into the "What works today" table.
- Implementation happens on an `experimental/` branch, with a small test mod of our own that only shows text.
