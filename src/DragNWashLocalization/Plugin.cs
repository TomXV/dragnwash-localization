using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using DragNWash.ModFramework;
using DragNWash.ModFramework.Assets;
using DragNWash.ModFramework.Saves;
using DragNWash.ModFramework.ToolWindow;
using HarmonyLib;
using UnityEngine;

namespace DragNWashLocalization
{
    // Since v1.0.0 the mod runs on Drag'n Wash ModFramework: the framework core
    // adds the Options language row and lists the mod on the Mods screen, and its
    // libraries hook text and dialogue, draw the F1 tool window, prepare fonts
    // and keep save history. What is left here is translation itself.
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    // Core 1.1.0 for ModInfo.UpdateRepository, 1.2.0 for ModInfo.Reloadable and
    // DeveloperTools.Enabled - the higher of the two.
    [BepInDependency(ModFramework.Guid, "1.2.0")]
    // Text 1.0.0: every GameText member this mod calls (AddRewriter, IsAvailable,
    // TryGetSource, RefreshAll, TextContext.IsRefresh) was already there.
    [BepInDependency(DragNWash.ModFramework.Text.GameText.Guid, "1.0.0")]
    // Dialogue 1.1.0 for LineKey and LineResolver (ScriptOrder, LineResolution).
    [BepInDependency(DragNWash.ModFramework.Dialogue.GameDialogue.Guid, "1.1.0")]
    // ToolWindow 1.1.0 for the Console tab's "tl" command: AddCommand's
    // completion overload and Drawable (Plugin.ImGui.cs).
    [BepInDependency(ToolWindow.Guid, "1.1.0")]
    // Assets 1.0.0: GameFonts is used unconditionally and every member this mod
    // calls was already there. Translated pictures need Assets 1.2.0
    // (AssetReplacements), but that is caught and skipped below, not required.
    [BepInDependency(GameFonts.Guid, "1.0.0")]
    // Saves 1.0.0: every GameSaves and GameFlags member this mod calls was
    // already there.
    [BepInDependency(GameSaves.Guid, "1.0.0")]
    public partial class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.tomxv.dragnwash.localization";
        public const string PluginName = "DragNWashLocalization";
        public const string PluginVersion = "1.4.0";

        // Every visible line costs dynamic geometry each frame the window is
        // open, and that scratch memory is what the Direct3D 12 bug chokes on at
        // present time.
        private const int MaxLogLines = 100;

        internal static ConfigEntry<string> TargetLocale;
        internal static ConfigEntry<int> FlagPanelDebug;
        internal static ConfigEntry<bool> PreloadAllLocales;
        internal static ConfigEntry<bool> LogDiscoveredKeys;
        internal static ConfigEntry<bool> VerboseTextLog;
        internal static ConfigEntry<KeyboardShortcut> DumpDialogueKey;
        internal static ConfigEntry<KeyboardShortcut> DumpUiTextKey;
        internal static ConfigEntry<double> LayoutRiskThreshold;
        internal static ConfigEntry<bool> HotReloadTranslations;
        internal static ConfigEntry<bool> TranslatePictures;
        internal static ConfigEntry<bool> TranslationsFromOtherMods;
        internal static string PluginDirectory;

        private static Plugin _instance;
        private static readonly List<string> LogBuffer = new List<string>();
        private static string _lastLogMessage;

        // Count alone stops changing once the buffer is full, which would freeze
        // the rendered log. Bumped on every mutation instead.
        private static int _logVersion;

        internal static void Log(string message)
        {
            // Logging failures must not propagate into the game's text updates.
            try
            {
                _instance?.Logger.LogInfo(message);

                lock (LogBuffer)
                {
                    if (message == _lastLogMessage)
                    {
                        return;
                    }
                    _lastLogMessage = message;

                    LogBuffer.Add(message);
                    if (LogBuffer.Count > MaxLogLines)
                    {
                        LogBuffer.RemoveAt(0);
                    }
                    _logVersion++;
                }
            }
            catch
            {
                // Swallow - see comment above.
            }
        }

        private Vector2 _logScroll;
        private Vector2 _localeScroll;
        private string[] _availableLocales = Array.Empty<string>();
        private int _lastLogVersion = -1;

        // The language last saved (config file), as opposed to one being
        // previewed from the Options dropdown.
        private string _committedLocale;
        private bool _preloadedEverything;
        private bool _saidToolsOff;

        // Unity logs an exception thrown from Awake and then calls Update every
        // frame anyway. Update dereferences the config entries Awake binds, so
        // a single failure during startup turns into a NullReferenceException
        // on every frame for the rest of the session. Guard it instead.
        private bool _ready;

        private void Awake()
        {
            try
            {
                Initialise();
                _ready = true;
            }
            catch (Exception ex)
            {
                // The tabs are added at the very end of Initialise, so reaching
                // here normally means none exist; take them back anyway for the
                // case where the failure came after they were added. A tab that
                // is listed but belongs to a plugin that never finished
                // starting looks alive and does nothing.
                RemoveToolTabs();
                Logger.LogError($"DragNWashLocalization failed to start, so it will stay idle this session: {ex}");
            }
        }

        private void Initialise()
        {
            _instance = this;
            PluginDirectory = Path.GetDirectoryName(Info.Location);
            ModFrameworkInfo.Register();

            TargetLocale = Config.Bind(
                "General",
                "TargetLocale",
                "ja",
                "Loads the files under Translations/<TargetLocale>/. For example ja, zh-Hans. Set en to leave the game in its original English: the mod stays installed but translates nothing.");

            LogDiscoveredKeys = Config.Bind(
                "Debug",
                "LogDiscoveredKeys",
                true,
                "Record text that has no translation yet in Translations/_discovered/strings.csv.");

            VerboseTextLog = Config.Bind(
                "Debug",
                "VerboseTextLog",
                true,
                "Log every text replacement, translated or not, in the activity log of the tool window (F1).");

            DumpDialogueKey = Config.Bind(
                "Debug",
                "DumpDialogueKey",
                new KeyboardShortcut(KeyCode.F6),
                "Writes every dialogue line the game has loaded to Translations/_discovered/dialogue_lines.csv.");

            DumpUiTextKey = Config.Bind(
                "Debug",
                "DumpUiTextKey",
                new KeyboardShortcut(KeyCode.F7),
                "Writes every UI string the game has loaded, hidden menus included, to Translations/_discovered/ui_texts.csv.");

            // Renamed from LayoutRiskThreshold: that compared the translation's
            // character width against the source's, so 1.4 meant "40% longer".
            // This compares what the text needs against the space it has, where
            // 1.0 is exactly full, and a stale 1.4 would hide real overflow.
            LayoutRiskThreshold = Config.Bind(
                "Debug",
                "LayoutOverflowThreshold",
                1.0,
                "When a translation needs more than this multiple of the space its container gives it, the line is recorded as an overflow risk in Translations/_discovered/layout_risks.csv. 1.0 means it fits exactly.");

            FlagPanelDebug = Config.Bind(
                "Debug",
                "FlagPanelDebug",
                0,
                "Troubleshooting only. Bit 1: no search box. Bit 2: no descriptions. Bit 4: no flag rows. Bit 8: no group headers.");

            PreloadAllLocales = Config.Bind(
                "Font",
                "PreloadAllLocales",
                false,
                "Prepare the fonts of every installed language at startup rather than only the one in use. On Direct3D 12 this always happens, because loading a font while the game runs crashes that renderer; turning it on elsewhere makes the first switch to each language instant at the cost of a slower start.");

            HotReloadTranslations = Config.Bind(
                "Debug",
                "HotReloadTranslations",
                true,
                "Reload the current language's strings.csv when it is saved and apply it on screen without restarting the game.");

            TranslatePictures = Config.Bind(
                "General",
                "TranslatePictures",
                true,
                new ConfigDescription(
                    "Shows pictures with text (menu buttons, signs) in the chosen language, where the language has them. On Direct3D 12 the pictures change the next time the game starts after a language change.",
                    null, new SettingMeta { DisplayName = "Translate pictures" }));

            TranslationsFromOtherMods = Config.Bind(
                "Experimental",
                "TranslationsFromOtherMods",
                false,
                new ConfigDescription(
                    "Beta, experimental. Reads the translations other mods ship for their own text (<mod folder>/Translations/<language>/strings.csv). How this works with each mod is untested: text may look wrong or not match; turn it off if something is off. On Direct3D 12, characters only a mod's translation uses need a restart after turning it on.",
                    null,
                    new SettingMeta { DisplayName = "[Experimental] Translations from other mods" },
                    new SectionMeta { DisplayName = "Experimental", Description = "Beta features, off by default." }));
            TranslationsFromOtherMods.SettingChanged += (sender, args) => _pendingReload = true;

            _committedLocale = TargetLocale.Value;
            RightToLeft.SetLocale(TargetLocale.Value);
            TranslationStore.Load(PluginDirectory, TargetLocale.Value);
            RefreshAvailableLocales();
            PrepareFonts();
            SetUpPictures();
            HotReload.Track(PluginDirectory, TargetLocale.Value);

            // Save snapshots used to live next to the plugin; the saves library
            // keeps them in BepInEx/SaveHistory now.
            GameSaves.ImportHistory(Path.Combine(PluginDirectory, "SaveHistory"));
            GameFlags.AddCatalog(Path.Combine(PluginDirectory, "FlagCatalog.csv"));

            PrepareWindowCharacters();
            GameFonts.CharactersPrepared += () => ToolWindow.PrepareCharacters(GameFonts.PreparedCharacters);

            var harmony = new Harmony(PluginGuid);
            harmony.PatchAll();
            TmpTextHook.Install();

            // Last, so that a failure anywhere above leaves nothing for the
            // player to press. The Options row in particular cannot be taken
            // back: the framework has no API to remove a choice once added.
            AddToolTabs();
            OptionsLanguage.Register(_availableLocales, LocaleDisplayName, () => _committedLocale, RequestLocale, CommitLocale);

            Logger.LogInfo($"DragNWashLocalization loaded. TargetLocale={TargetLocale.Value}, loaded entries={TranslationStore.EntryCount}, ignore patterns={IgnoreRules.PatternCount}, graphics={SystemInfo.graphicsDeviceType}");
        }

        // Fonts are prepared off any render frame. On Direct3D 12 every installed
        // language is prepared here, since a runtime atlas upload crashes that
        // renderer; elsewhere only the language in use is, and the rest follow on
        // a switch. Locale names first: the Options dropdown shows every one of
        // them at once, whichever language is in use.
        private void PrepareFonts()
        {
            GameFonts.AddFontFolder(Path.Combine(PluginDirectory, "fonts"));
            GameFonts.SetLanguage(TargetLocale.Value);
            foreach (string locale in _availableLocales)
            {
                GameFonts.Prepare(locale, new[] { LocaleDisplayName(locale) });
            }

            _preloadedEverything = (PreloadAllLocales != null && PreloadAllLocales.Value) || !GameFonts.RuntimeUploadsAreSafe;
            if (_preloadedEverything)
            {
                Log(GameFonts.RuntimeUploadsAreSafe
                    ? "[font] Preparing every installed language at startup ([Font] PreloadAllLocales)."
                    : "[font] Direct3D 12: preparing every installed language at startup, because loading a font mid-game crashes this renderer.");
                foreach (KeyValuePair<string, List<string>> kv in TranslationStore.CollectTextsByLocale(PluginDirectory))
                {
                    GameFonts.Prepare(kv.Key, kv.Value);
                }
            }
            else
            {
                Log("[font] Preparing only the current language; others load when selected.");
            }
            // The current locale last, so its texts also cover anything a
            // translator has in memory that is not in the file on disk yet.
            GameFonts.Prepare(TargetLocale.Value, TranslationStore.TranslatedTexts);
        }

        // Translated pictures: Translations/<locale>/textures/<game texture>.png,
        // applied by the framework's Assets library (1.2.0 and later) for the
        // language in use. An older Assets library has no such call; the mod
        // then runs without pictures.
        private void SetUpPictures()
        {
            try
            {
                AddPictureFolder();
                TranslatePictures.SettingChanged += (sender, args) => SetPicturesOn(TranslatePictures.Value);
            }
            catch (MissingMethodException)
            {
                Log("[pictures] The framework's Assets library is older than 1.2.0; translated pictures are not shown.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Translated pictures could not be set up: {ex}");
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void AddPictureFolder()
        {
            AssetReplacements.AddLanguageFolder(PluginGuid, Path.Combine(PluginDirectory, "Translations"), "textures");
            if (!TranslatePictures.Value)
            {
                AssetReplacements.SetLanguageFoldersEnabled(PluginGuid, false);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void SetPicturesOn(bool on)
        {
            AssetReplacements.SetLanguageFoldersEnabled(PluginGuid, on);
            NotePendingPictures();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void NotePendingPictures()
        {
            string pending = AssetReplacements.PendingLanguage;
            if (pending != null)
            {
                Log($"[pictures] Pictures for {pending} are shown after the game restarts (Direct3D 12 cannot load them while the game runs).");
            }
        }

        // The tool window draws the activity log (translated text), the language
        // names and the flag catalog with its own font; rasterizing those glyphs
        // while the window is open is the Direct3D 12 crash, so do it now.
        private void PrepareWindowCharacters()
        {
            var text = new StringBuilder(GameFonts.PreparedCharacters);
            // Three things the window shows that the mod does not get to choose:
            // where the game was installed, what the folders under Translations
            // are called, and what the config file says the language is. The
            // About tab prints all three, and the activity log reaches the same
            // font with the install path whenever a line names a written file.
            text.Append(PluginDirectory);
            text.Append(TargetLocale.Value);
            foreach (string locale in _availableLocales)
            {
                text.Append(locale);
                text.Append(LocaleDisplayName(locale));
            }
            foreach (FlagInfo flag in GameFlags.Catalog)
            {
                text.Append(flag.Id).Append(flag.Group).Append(flag.Description);
            }
            ToolWindow.PrepareCharacters(text.ToString());
        }

        private string _pendingLocale;
        // False for a switch that should not be written to the config yet: the
        // Options dropdown previews a language until Save is pressed.
        private bool _pendingLocalePersist = true;

        // A language switch, from the tool window (persist) or the Options
        // dropdown (a preview until Save). Applied in Update.
        private void RequestLocale(string locale, bool persist)
        {
            if (string.IsNullOrEmpty(locale))
            {
                return;
            }
            if (locale == TargetLocale.Value)
            {
                if (persist && locale != _committedLocale)
                {
                    CommitLocale(locale);
                }
                return;
            }
            _pendingLocale = locale;
            _pendingLocalePersist = persist;
        }

        // Save pressed in Options: write the language now in use to the config.
        private void CommitLocale(string locale)
        {
            try
            {
                SetLocaleValue(locale, save: true);
                _committedLocale = locale;
                Log($"Language {locale} saved from the Options screen.");
            }
            catch (Exception ex)
            {
                Log($"Could not save the language setting: {ex.Message}");
            }
        }

        private void SetLocaleValue(string locale, bool save)
        {
            if (save)
            {
                TargetLocale.Value = locale;
                Config.Save();
                return;
            }
            // In memory only; the file keeps the last saved language.
            bool saveOnSet = Config.SaveOnConfigSet;
            Config.SaveOnConfigSet = false;
            try
            {
                TargetLocale.Value = locale;
            }
            finally
            {
                Config.SaveOnConfigSet = saveOnSet;
            }
        }

        private float _nextDiscoveredFlushTime;
        private bool _pendingReload;
        private bool _pendingDump;
        private bool _pendingUiDump;
        private bool _pendingLayoutCheck;
        private bool _pendingHashFile;
        private bool _pendingFlowDump;
        private bool _pendingWorkingCopy;
        private string _pendingRestoreSlot;
        private SaveSnapshot _pendingRestoreSnapshot;

        private void Update()
        {
            if (!_ready)
            {
                return;
            }

            // Locale switching is requested from OnGUI but performed here: it
            // reads files and rasterizes glyphs, neither of which belongs in a
            // render callback.
            // A language switch, or the same language read again (the setting
            // for other mods' translations changed).
            if (_pendingLocale != null || _pendingReload)
            {
                bool reloadOnly = _pendingLocale == null;
                string locale = _pendingLocale ?? TargetLocale.Value;
                bool persist = !reloadOnly && _pendingLocalePersist;
                _pendingLocale = null;
                _pendingLocalePersist = true;
                _pendingReload = false;

                SetLocaleValue(locale, persist);
                if (persist)
                {
                    _committedLocale = locale;
                }
                RightToLeft.SetLocale(locale);
                TranslationStore.Load(PluginDirectory, locale);
                GameFonts.SetLanguage(locale);
                try
                {
                    NotePendingPictures();
                }
                catch (MissingMethodException)
                {
                }
                // On Direct3D 12 the fonts were all prepared at startup and this
                // only reorders the fallback chain. Elsewhere a language seen for
                // the first time is prepared here, in Update, in one batch.
                if (!_preloadedEverything)
                {
                    GameFonts.Prepare(locale, TranslationStore.TranslatedTexts);
                }
                else
                {
                    // A language folder added after startup. Preparing it now would
                    // be the crash this mode exists to avoid.
                    int unprepared = GameFonts.CountUnprepared(TranslationStore.TranslatedTexts);
                    if (unprepared > 0)
                    {
                        Log(reloadOnly
                            ? $"[font] Other mods' translations use {unprepared} character(s) no font was prepared for at startup; restart the game to prepare them."
                            : $"[font] {locale} was installed after startup and has {unprepared} character(s) no font was prepared for; restart the game to prepare them.");
                    }
                }
                TmpTextHook.RefreshAll();
                // A preview from the dropdown already shows itself there.
                if (persist)
                {
                    OptionsLanguage.Refresh();
                }
                HotReload.Track(PluginDirectory, locale);
                Log(reloadOnly
                    ? $"Reloaded {locale} ({(ModTranslations.Enabled ? $"with {ModTranslations.Packs.Count} other mod(s)" : "other mods' translations off")}). Loaded entries={TranslationStore.EntryCount}"
                    : $"Switched locale to {locale}. Loaded entries={TranslationStore.EntryCount}");
            }

            // Everything from here on is for translators and mod makers; a
            // player who only installed the mod has the framework's developer
            // tools off and gets none of it (no exports, no folder of the
            // game's text, no file watching).
            if (!DeveloperTools.Enabled)
            {
                if ((DumpDialogueKey.Value.IsDown() || DumpUiTextKey.Value.IsDown()) && !_saidToolsOff)
                {
                    _saidToolsOff = true;
                    Log("[tools] Exports and hot reload are part of the developer tools, which are off. Turn them on in Options > Mods > Drag'n Wash ModFramework > Developer tools.");
                }
                return;
            }

            if (HotReloadTranslations.Value)
            {
                HotReload.Tick(PluginDirectory, TargetLocale.Value);
            }

            if (DumpDialogueKey.Value.IsDown() || _pendingDump)
            {
                _pendingDump = false;
                DialogueDumper.DumpAll(PluginDirectory);
            }

            if (DumpUiTextKey.Value.IsDown() || _pendingUiDump)
            {
                _pendingUiDump = false;
                UiTextDumper.DumpAll(PluginDirectory);
            }

            if (_pendingRestoreSnapshot != null)
            {
                SaveSnapshot snapshot = _pendingRestoreSnapshot;
                string slot = _pendingRestoreSlot;
                _pendingRestoreSnapshot = null;
                _pendingRestoreSlot = null;
                string result = GameSaves.Restore(slot, snapshot);
                // The Saves tab cached the slot before the restore: the button
                // asked for a refresh, but a repaint in that same frame can run
                // it before this line does the work. Ask again now.
                _savesRefreshAt = 0;
                Log("[saves] " + result);
                ToolWindow.ShowNotice(result);
            }

            if (_pendingWorkingCopy)
            {
                _pendingWorkingCopy = false;
                Log(WorkingCopy.Export(PluginDirectory, TargetLocale.Value));
            }

            if (_pendingHashFile)
            {
                _pendingHashFile = false;
                // Rewrites the file; hot reload then re-reads it, which is a
                // no-op for the table since every row resolves to the same key.
                Log(TranslationStore.HashFileInPlace(PluginDirectory, TargetLocale.Value));
            }

            if (_pendingFlowDump)
            {
                _pendingFlowDump = false;
                Log(FlowDumper.Export(PluginDirectory));
            }

            if (_pendingLayoutCheck)
            {
                _pendingLayoutCheck = false;
                LayoutChecker.Report(PluginDirectory, LayoutRiskThreshold.Value);
            }

            // Periodic, main-thread, low-frequency: see the comment on
            // TranslationStore.NoteDiscoveredText for why this isn't done inline.
            if (Time.unscaledTime >= _nextDiscoveredFlushTime)
            {
                _nextDiscoveredFlushTime = Time.unscaledTime + 2f;
                TranslationStore.FlushDiscoveredToDisk();
            }
        }

        private void RefreshAvailableLocales()
        {
            string translationsDir = Path.Combine(PluginDirectory, "Translations");
            if (!Directory.Exists(translationsDir))
            {
                _availableLocales = Array.Empty<string>();
                return;
            }

            // "en" is always offered: it has no translation folder and means
            // "leave the game's own English text alone".
            _localeNames.Clear();
            _availableLocales = new[] { "en" }
                .Concat(Directory.GetDirectories(translationsDir)
                    .Select(Path.GetFileName)
                    .Where(name => !name.StartsWith("_", StringComparison.Ordinal) && name != "en")
                    .OrderBy(name => name, StringComparer.Ordinal))
                .ToArray();
            foreach (string locale in _availableLocales)
            {
                IgnoreRules.AddExact(LocaleDisplayName(locale));
            }
        }

        private string _logText = string.Empty;
    }
}
