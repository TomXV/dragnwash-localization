# Security policy

[日本語](SECURITY.ja.md)

Drag'n Wash Localization is an unofficial mod for a game, but it installs files
on other people's computers and it ships data written by contributors from all
over. Reports about that are welcome and taken seriously.

## Reporting a vulnerability

**Please do not open a public issue for a security problem.**

Use GitHub's private reporting instead:
[**Report a vulnerability**](https://github.com/TomXV/dragnwash-localization/security/advisories/new)
(the repository's **Security** tab → **Report a vulnerability**). Only you and
the maintainer can see the report, and it stays private until a fix is out.

Helpful things to put in it:

- What an attacker can do, and what they need in order to do it (a language
  pack the player installs? a file they downloaded? the installer?)
- The steps to reproduce it, with the version of the mod, the game and BepInEx
- The log, if there is one: `BepInEx/LogOutput.log`

This is a hobby project run by one person. You should get a first reply within
a week. If a week goes by with nothing, a ping on the same advisory thread is
welcome — the report was not ignored on purpose.

When a report is confirmed, the fix goes into the next release, the advisory is
published with it, and you are credited in it by whatever name you ask for (or
not credited at all, if you prefer). Please give the fix a chance to reach
players before writing about the problem in public.

**One thing that is urgent but not a vulnerability:** if a translation in a
published language pack is abusive, hateful, or slips in something that does
not belong in the game, that is not a security report — but do tell the
maintainer right away. A regular issue, or the private advisory form if you
would rather not make it public, both reach the same person.

## What is covered

The latest release is the one that gets fixes. Older versions do not get
back-ported patches; the answer to "is it fixed in 1.0.x?" is "update".

| Version | Supported |
|---|---|
| The latest release ([Releases](https://github.com/TomXV/dragnwash-localization/releases)) | Yes |
| Anything older | No — please update first |

In scope, everything this repository ships:

- The plugin ([`src/`](src/)) — how it reads language packs, fonts and images
  and puts their text into the game
- The published language packs under [`Translations/`](Translations/) and the
  images under [`assets/`](assets/), as files the plugin loads
- The helper scripts in [`tools/`](tools/) and the workflows in
  [`.github/workflows/`](.github/workflows/)

The places worth looking hardest at:

- **Loading a language pack.** A pack is plain data: CSV, a `name.txt`, fonts
  and images, read from the mod's own folder. A crafted file that reads or
  writes outside that folder, that makes the plugin load code, or that lets a
  pack reach the rest of the player's disk, is a real bug.
- **Text going into the game.** Translations end up in Unity's UI, where a
  formatting tag is just a tag — but if a translation can do more than style
  text, say so.
- **The installer.** `Install.exe` is Drag'n Wash ModFramework's shared
  installer; report problems in it
  [there](https://github.com/TomXV/dragnwash-modframework/security/advisories/new),
  and anything about how this mod is packed for it here.
- **Translations for other mods** (experimental, off by default), which reads
  another mod's files to translate it. It should never write to them.

## What is not covered

- **A mod can do anything the game can do.** Mods are plain .NET assemblies
  loaded by BepInEx into the game's process. This one is not a sandbox and does
  not claim to be. Only install mods you trust.
- **The game itself.** Bugs in Drag'n Wash belong to its developer, not here.
  Do not send them a crash that only happens with mods installed.
- **BepInEx.** Report those to [the BepInEx project](https://github.com/BepInEx/BepInEx).
- **Wrong, clumsy or missing translations.** Those are ordinary issues, and
  there is a template for them — thank you for reporting them there.
- **Spoilers.** The CSV files contain the whole story on purpose, and the README
  says so at the top. That is the design, not a leak.
- **Antivirus false positives on `Install.exe`.** An unsigned installer gets
  flagged; that is a detection quirk, not a vulnerability. What to do about it
  is in the [README](README.md), and the installer's source is public. A regular
  issue is fine for those.
- **Copies from anywhere but [Releases](https://github.com/TomXV/dragnwash-localization/releases).**
  Each release lists the zip's SHA-256. If a file you got elsewhere does
  something nasty, that is the file, not this project — though a report telling
  us where you found it is still appreciated.
