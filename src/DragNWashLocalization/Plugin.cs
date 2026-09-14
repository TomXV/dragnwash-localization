using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DragNWashLocalization
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public partial class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.tomxv.dragnwash.localization";
        public const string PluginName = "DragNWashLocalization";
        public const string PluginVersion = "0.6.2";

        // Every visible line costs dynamic geometry each frame the menu is open,
        // and that scratch memory is what the Direct3D 12 bug chokes on at
        // present time. See CreateMenuFont.
        private const int MaxLogLines = 100;
        private const int MenuFontSize = 14;
        private static bool _menuFontBold;

        internal static ConfigEntry<string> TargetLocale;
        internal static ConfigEntry<int> FlagPanelDebug;
        internal static ConfigEntry<string> MenuFontMode;
        internal static ConfigEntry<bool> PreloadAllLocales;
        internal static ConfigEntry<bool> LogDiscoveredKeys;
        internal static ConfigEntry<bool> VerboseTextLog;
        internal static ConfigEntry<KeyboardShortcut> ToggleMenuKey;
        internal static ConfigEntry<KeyboardShortcut> DumpDialogueKey;
        internal static ConfigEntry<KeyboardShortcut> DumpUiTextKey;
        internal static ConfigEntry<int> FontAtlasPointSize;
        internal static ConfigEntry<double> LayoutRiskThreshold;
        internal static ConfigEntry<bool> HotReloadTranslations;
        internal static ConfigEntry<bool> SaveHistoryEnabled;
        internal static ConfigEntry<int> SaveHistoryKeep;
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

        private bool _showMenu;
        private Vector2 _logScroll;
        private Vector2 _localeScroll;
        private string[] _availableLocales = Array.Empty<string>();
        private Rect _windowRect = new Rect(24, 24, 780, 580);
        private int _lastLogVersion = -1;

        private void Awake()
        {
            _instance = this;
            PluginDirectory = Path.GetDirectoryName(Info.Location);

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
                "Log every text replacement, translated or not, in the activity log of the debug menu.");

            ToggleMenuKey = Config.Bind(
                "Debug",
                "ToggleMenuKey",
                new KeyboardShortcut(KeyCode.F1),
                "Shows and hides the debug menu: language switching, dialogue export and the activity log.");

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

            FontAtlasPointSize = Config.Bind(
                "Font",
                "AtlasPointSize",
                80,
                "Atlas sampling point size for the fallback fonts. Higher is sharper. Every glyph is rasterized at startup, so raising this costs a little loading time rather than performance during play.");

            // Renamed from LayoutRiskThreshold: that compared the translation's
            // character width against the source's, so 1.4 meant "40% longer".
            // This compares what the text needs against the space it has, where
            // 1.0 is exactly full, and a stale 1.4 would hide real overflow.
            LayoutRiskThreshold = Config.Bind(
                "Debug",
                "LayoutOverflowThreshold",
                1.0,
                "When a translation needs more than this multiple of the space its container gives it, the line is recorded as an overflow risk in Translations/_discovered/layout_risks.csv. 1.0 means it fits exactly.");

            SaveHistoryEnabled = Config.Bind(
                "Debug",
                "SaveHistoryEnabled",
                true,
                "Keep a copy under BepInEx/plugins/DragNWashLocalization/SaveHistory/ every time the game writes a save, so any earlier one can be restored from the Saves tab of the F1 menu.");

            SaveHistoryKeep = Config.Bind(
                "Debug",
                "SaveHistoryKeep",
                30,
                "How many generations to keep per save slot.");

            MenuFontMode = Config.Bind(
                "Debug",
                "MenuFontMode",
                "auto",
                "Font for the F1 menu: auto (OS font with fallback), builtin (Unity's built-in font), skin (leave the IMGUI skin font alone).");

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

            RightToLeft.SetLocale(TargetLocale.Value);
            TranslationStore.Load(PluginDirectory, TargetLocale.Value);
            // Fonts are prepared off any render frame. On Direct3D 12 every
            // installed language is prepared here, since a runtime atlas upload
            // crashes that renderer; elsewhere only the language in use is, and
            // the rest follow on a switch. See FontFallback.Startup.
            // Locale names first: the Options dropdown shows every one of them
            // at once, so their glyphs are prepared with the fonts.
            RefreshAvailableLocales();
            FontFallback.Startup(PluginDirectory, TargetLocale.Value, TranslationStore.TranslatedTexts,
                PreloadAllLocales != null && PreloadAllLocales.Value, LocaleNamesForFonts());
            HotReload.Track(PluginDirectory, TargetLocale.Value);
            SaveHistory.Configure(PluginDirectory, SaveHistoryKeep.Value);
            CreateMenuBackgroundTexture();
            CreateMenuFont();

            var harmony = new Harmony(PluginGuid);
            CursorUnlock.Install(harmony);
            VirtualClick.Install(harmony);
            MenuText.Install(harmony);
            OptionsLanguage.Install(harmony);
            LineIdContext.Install(harmony);
            harmony.PatchAll();

            Logger.LogInfo($"DragNWashLocalization loaded. TargetLocale={TargetLocale.Value}, loaded entries={TranslationStore.EntryCount}, ignore patterns={IgnoreRules.PatternCount}, graphics={SystemInfo.graphicsDeviceType}");

            // Glyphs are prewarmed above so this should no longer be reachable,
            // but the underlying engine bug is still there: anything else that
            // allocates textures at runtime can hit it.
            if (Application.unityVersion == "6000.3.14f1" &&
                SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Direct3D12)
            {
                Logger.LogInfo("Running on Unity 6000.3.14f1 / Direct3D12, which has a native crash in D3D12ScratchAllocator around runtime texture uploads (Unity issue UUM-140564). If the game still crashes, add -force-d3d11 to its Steam launch options and restart.");
            }
        }

        private float _nextDiscoveredFlushTime;
        private string _pendingLocale;
        // False for a switch that should not be written to the config yet: the
        // Options dropdown previews a language until Save is pressed.
        private bool _pendingLocalePersist = true;
        private Func<string, string> _displayNameFunc;
        private Action<string, bool> _requestLocaleFunc;
        private Action _commitLocaleFunc;
        private Func<string> _currentLocaleFunc;

        // Language chosen in the Options dropdown. Applied in Update like every
        // other switch; persist=false keeps it out of the config file until the
        // player presses Save.
        private void RequestLocale(string locale, bool persist)
        {
            if (string.IsNullOrEmpty(locale) || locale == TargetLocale.Value) return;
            _pendingLocale = locale;
            _pendingLocalePersist = persist;
        }

        // Save pressed in Options: write the language now in use to the config.
        private void CommitLocale()
        {
            try
            {
                Config.Save();
                Log($"Language {TargetLocale.Value} saved from the Options screen.");
            }
            catch (Exception ex)
            {
                Log($"Could not save the language setting: {ex.Message}");
            }
        }

        private IEnumerable<KeyValuePair<string, string>> LocaleNamesForFonts()
        {
            foreach (string locale in _availableLocales)
            {
                yield return new KeyValuePair<string, string>(locale, LocaleDisplayName(locale));
            }
        }
        private bool _pendingDump;
        private bool _pendingUiDump;
        private bool _pointerGrabbedByMenu;
        private bool _inputBlockingBroken;
        private bool _pendingLayoutCheck;
        private bool _pendingHashFile;
        private bool _pendingFlowDump;
        private bool _pendingWorkingCopy;
        private string _pendingRestoreSlot;
        private SaveHistory.Snapshot _pendingRestoreSnapshot;

        private bool _menuWasOpen;

        private void Update()
        {
            // Locale switching is requested from OnGUI but performed here: it
            // reads files and rasterizes glyphs, neither of which belongs in a
            // render callback.
            if (_pendingLocale != null)
            {
                string locale = _pendingLocale;
                bool persist = _pendingLocalePersist;
                _pendingLocale = null;
                _pendingLocalePersist = true;

                if (persist)
                {
                    TargetLocale.Value = locale;
                }
                else
                {
                    // Change the value in memory only; the file keeps the last
                    // confirmed language until CommitLocale.
                    bool saveOnSet = Config.SaveOnConfigSet;
                    Config.SaveOnConfigSet = false;
                    try { TargetLocale.Value = locale; }
                    finally { Config.SaveOnConfigSet = saveOnSet; }
                }
                RightToLeft.SetLocale(locale);
                TranslationStore.Load(PluginDirectory, locale);
                // On Direct3D 12 the fonts were all prepared at startup and this
                // only reorders the fallback chain. Elsewhere a language seen for
                // the first time is prepared here, in Update, in one batch.
                if (FontFallback.SwitchTo(locale, TranslationStore.TranslatedTexts))
                {
                    WarmMenuFont();
                }
                TmpTextHook.RefreshAll();
                OptionsLanguage.Sync(locale, committed: persist);
                HotReload.Track(PluginDirectory, locale);
                Log($"Switched locale to {locale}. Loaded entries={TranslationStore.EntryCount}");
            }

            if (HotReloadTranslations.Value)
            {
                HotReload.Tick(PluginDirectory, TargetLocale.Value);
            }

            OptionsLanguage.Tick(_availableLocales, _displayNameFunc ?? (_displayNameFunc = LocaleDisplayName),
                _currentLocaleFunc ?? (_currentLocaleFunc = () => TargetLocale.Value),
                _requestLocaleFunc ?? (_requestLocaleFunc = RequestLocale),
                _commitLocaleFunc ?? (_commitLocaleFunc = CommitLocale));

            if (ToggleMenuKey.Value.IsDown())
            {
                _showMenu = !_showMenu;
            }
            // The menu can also be closed from its own X button, so track the
            // state here rather than only on the key.
            if (_showMenu != _menuWasOpen)
            {
                _menuWasOpen = _showMenu;
                if (_showMenu) CursorUnlock.Hold(this); else { CursorUnlock.Release(); VirtualClick.Cancel(); }
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

            if (SaveHistoryEnabled.Value)
            {
                SaveHistory.Tick();
            }

            if (_pendingRestoreSnapshot != null)
            {
                SaveHistory.Snapshot snapshot = _pendingRestoreSnapshot;
                string slot = _pendingRestoreSlot;
                _pendingRestoreSnapshot = null;
                _pendingRestoreSlot = null;
                string result = SaveHistory.Restore(slot, snapshot);
                Log(result);
                _menuNotice = result;
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

            if (_showMenu)
            {
                CursorUnlock.Tick();
                VirtualClick.Poll();
                if (VirtualClick.UpdateDrag(ref _windowRect, 48, 58, 22))
                    _windowRect = ClampMenuRect(_windowRect, Screen.width, Screen.height);
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

            // Last, and swallowing its own failures: an exception here used to
            // abort the rest of Update, which silently disabled the export and
            // layout buttons.
            if (!_inputBlockingBroken)
            {
                try
                {
                    UpdateInputBlocking();
                }
                catch (Exception ex)
                {
                    // Give up rather than throw once per frame forever.
                    _inputBlockingBroken = true;
                    InputBlocker.SetBlocking(false);
                    Log($"[input] Input blocking disabled: {ex.Message}");
                }
            }
        }

        // Suspend the game's own input while the pointer is working the debug
        // window. Held mouse buttons keep the block even once the pointer
        // leaves, so dragging the window or its resize grip past the edge does
        // not hand the drag back to the game mid-gesture.
        //
        // Read the pointer through the Input System, not UnityEngine.Input:
        // this game has legacy input handling switched off, so every legacy
        // read throws. BepInEx's own KeyboardShortcut picks the right backend
        // for us, which is why the hotkeys work either way.
        private void UpdateInputBlocking()
        {
            if (!_showMenu)
            {
                _pointerGrabbedByMenu = false;
                InputBlocker.SetBlocking(false);
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                _pointerGrabbedByMenu = false;
                InputBlocker.SetBlocking(false);
                return;
            }

            Vector2 position = mouse.position.ReadValue();
            // The Input System measures from the bottom left, GUI from the top.
            bool over = _windowRect.Contains(new Vector2(position.x, Screen.height - position.y));

            bool held = mouse.leftButton.isPressed || mouse.rightButton.isPressed;
            if (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame)
            {
                _pointerGrabbedByMenu = over;
            }
            else if (!held)
            {
                _pointerGrabbedByMenu = false;
            }

            InputBlocker.SetBlocking(over || _pointerGrabbedByMenu);
        }

        private void OnDestroy()
        {
            // Leaving the game's input suspended would soft-lock it.
            InputBlocker.SetBlocking(false);
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
                    .Where(name => !name.StartsWith("_") && name != "en")
                    .OrderBy(name => name, StringComparer.Ordinal))
                .ToArray();
            foreach (string locale in _availableLocales)
            {
                IgnoreRules.AddExact(LocaleDisplayName(locale));
            }
        }

        private GUIStyle _windowStyle;
        private GUIStyle _labelStyle;
        private Texture2D _darkBackground;
        private Font _menuFont;
        private string _logText = string.Empty;

        // Built during Awake rather than on the first F1 press: Texture2D.Apply
        // uploads to the GPU, and doing that on the frame the menu opens is
        // exactly the runtime upload that trips the Direct3D 12 bug described
        // in FontFallback. GUIStyles still have to wait for GUI.skin.
        private void CreateMenuBackgroundTexture()
        {
            _darkBackground = new Texture2D(1, 1);
            _darkBackground.SetPixel(0, 0, new Color(0.06f, 0.06f, 0.08f, 0.95f));
            _darkBackground.Apply();
        }

        // IMGUI draws through its own dynamic font, which grows its texture the
        // first time it is asked for a character - and the log shows translated
        // text, so opening the menu would upload a texture full of freshly
        // rasterized kanji on that very frame. The crash is
        // D3D12ScratchAllocator::ReleaseExcessScratch during PresentFrame, so
        // the frame that opens the menu is exactly the wrong one to do this on.
        // Own the font instead of relying on GUI.skin's, and fill it here.
        private static bool FontRenders(Font font)
        {
            try
            {
                font.RequestCharactersInTexture("A", MenuFontSize, FontStyle.Normal);
                return font.HasCharacter('A')
                       && font.GetCharacterInfo('A', out CharacterInfo info, MenuFontSize, FontStyle.Normal)
                       && info.advance > 0;
            }
            catch
            {
                return false;
            }
        }

        // The activity log shows the current language's text through IMGUI,
        // which keeps its own font texture. With fonts loaded per language, a
        // switch brings in characters the menu font has never drawn, so warm
        // them here - from Update, never from OnGUI, where the upload would be
        // the Direct3D 12 crash.
        private void WarmMenuFont()
        {
            if (_menuFont == null) return;
            try
            {
                _menuFont.RequestCharactersInTexture(FontFallback.WarmedCharacters(), MenuFontSize, FontStyle.Normal);
            }
            catch (Exception ex)
            {
                Log($"Menu font warm-up failed: {ex.Message}");
            }
        }

        private void CreateMenuFont()
        {
            try
            {
                string mode = MenuFontMode != null ? MenuFontMode.Value : "auto";
                if (mode == "skin")
                {
                    Log("Menu font: skin (config)");
                    return;
                }
                if (mode == "builtin")
                {
                    _menuFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    Log("Menu font: built-in (config) -> " + (_menuFont != null ? _menuFont.name : "null"));
                    if (_menuFont == null) return;
                }
                // Unity hands back a Font even when the OS has no such family
                // (the menu then draws nothing, which is what happened inside
                // Steam's Linux runtime), so check that a glyph really renders
                // before trusting a candidate.
                var installed = new HashSet<string>(Font.GetOSInstalledFontNames() ?? new string[0], StringComparer.OrdinalIgnoreCase);
                foreach (string name in _menuFont != null ? new string[0] : new[] { "Yu Gothic UI", "Meiryo UI", "Hiragino Sans", "PingFang SC", "Noto Sans CJK JP", "Noto Sans CJK SC" })
                {
                    // A name the OS does not list yields a Font that measures but
                    // never draws (seen inside Steam's Linux runtime), so skip it.
                    if (installed.Count > 0 && !installed.Contains(name)) continue;
                    Font candidate = Font.CreateDynamicFontFromOSFont(name, MenuFontSize);
                    if (candidate == null) continue;
                    if (FontRenders(candidate))
                    {
                        _menuFont = candidate;
                        Log($"Menu font: {name}");
                        break;
                    }
                    Destroy(candidate);
                }
                if (_menuFont == null)
                {
                    // No OS font with CJK glyphs (Steam's Linux runtime): use the
                    // Noto Sans JP asset shipped next to the plugin.
                    Font bundled = MenuFontBundle.TryLoad(PluginDirectory);
                    if (bundled != null && FontRenders(bundled)) { _menuFont = bundled; _menuFontBold = true; }
                    else if (bundled != null) Log("Menu font: the bundled font does not render here either.");
                }
                if (_menuFont == null)
                {
                    // Unity's built-in font always renders ASCII; CJK labels in
                    // the menu will be blank on this system, but it stays usable.
                    _menuFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    Log(_menuFont != null
                        ? "Menu font: none of the OS fonts render here; using Unity's built-in font (ASCII only)."
                        : "Menu font: no usable font found; the menu will use the default skin font.");
                    if (_menuFont == null) return;
                }

                var ascii = new StringBuilder();
                for (char c = ' '; c <= '~'; c++)
                {
                    ascii.Append(c);
                }
                ascii.Append(FontFallback.WarmedCharacters());

                _menuFont.RequestCharactersInTexture(ascii.ToString(), MenuFontSize, FontStyle.Normal);
                // Locale display names may be non-Latin (name.txt is translator-set).
                foreach (string locale in _availableLocales ?? new string[0])
                {
                    _menuFont.RequestCharactersInTexture(LocaleDisplayName(locale), MenuFontSize, FontStyle.Normal);
                }
                // The flag editor shows catalog text (Japanese descriptions) in
                // this font; rasterizing those glyphs while the menu is open is
                // the D3D12 crash trigger, so they are warmed here at startup.
                var catalog = new StringBuilder();
                foreach (FlagCatalog.Entry e in FlagCatalog.Entries)
                {
                    catalog.Append(e.Id).Append(e.Group).Append(e.Description);
                }
                _menuFont.RequestCharactersInTexture(catalog.ToString(), MenuFontSize, FontStyle.Normal);

                // Diagnostics for systems where the menu draws no text.
                try
                {
                    string[] osFonts = Font.GetOSInstalledFontNames() ?? new string[0];
                    Texture tex = _menuFont.material != null ? _menuFont.material.mainTexture : null;
                    Material mat = _menuFont.material;
                    Shader textShader = Shader.Find("GUI/Text Shader");
                    Log($"Menu font check: OS reports {osFonts.Length} fonts [{string.Join(", ", osFonts)}]; dynamic={_menuFont.dynamic}; names=[{string.Join(", ", _menuFont.fontNames ?? new string[0])}]; atlas={(tex == null ? "none" : tex.width + "x" + tex.height + " " + tex.GetType().Name)}; glyph A={( _menuFont.GetCharacterInfo('A', out CharacterInfo ci, MenuFontSize, FontStyle.Normal) ? ci.advance.ToString() : "missing")}; material={(mat == null ? "none" : mat.name + " / " + (mat.shader == null ? "no shader" : mat.shader.name + (mat.shader.isSupported ? "" : " (UNSUPPORTED)")))}; GUI/Text Shader={(textShader == null ? "missing" : (textShader.isSupported ? "ok" : "unsupported"))}");
                }
                catch (Exception ex)
                {
                    Log($"Menu font check failed: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Log($"Failed to prepare the debug menu font: {ex.Message}");
                _menuFont = null;
            }
        }

    }
}
