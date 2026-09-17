using System.IO;
using System.Collections.Generic;
using System;
using UnityEngine;
using DragNWash.ModFramework.Saves;
using DragNWash.ModFramework.ToolWindow;

namespace DragNWashLocalization
{
    public partial class Plugin
    {
        private const float RowHeight = ToolWindow.RowHeight;
        private static ToolWindowStyles S => ToolWindow.Styles;
        private int _editLevel;
        private int _editLevelBase = -2;
        private string _editLevelSlot;
        private bool _progressConfirm;
        private bool _newestMatchesSave;
        private bool _showFlags;
        private List<SaveFlag> _savesFlags = new List<SaveFlag>();
        private string _flagFilter = "";
        private bool _flagClearConfirm;
        private bool _showOnceLines;

        // One row of the flag editor: catalog entry (may be null) + save state.
        private sealed class FlagRow
        {
            public string Id;
            public string Group;
            public string Description;
            public bool? Value;   // null = never set in this save
            public bool IsHeader;
        }
        private List<FlagRow> _flagRows = new List<FlagRow>();

        private void RebuildFlagRows()
        {
            _flagRows.Clear();
            var inSave = new Dictionary<string, bool>(StringComparer.Ordinal);
            foreach (SaveFlag f in _savesFlags)
            {
                inSave[f.Id] = f.Value;
            }

            string filter = (_flagFilter ?? "").Trim();
            bool Match(string id, string desc) =>
                filter.Length == 0 ||
                id.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                (desc != null && desc.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);

            var groups = new List<string>();
            var byGroup = new Dictionary<string, List<FlagRow>>(StringComparer.Ordinal);
            void Add(string group, FlagRow row)
            {
                if (!byGroup.TryGetValue(group, out List<FlagRow> list))
                {
                    list = new List<FlagRow>();
                    byGroup[group] = list;
                    groups.Add(group);
                }
                list.Add(row);
            }

            var known = new HashSet<string>(StringComparer.Ordinal);
            foreach (FlagInfo e in GameFlags.Catalog)
            {
                known.Add(e.Id);
                if (!Match(e.Id, e.Description)) continue;
                Add(e.Group, new FlagRow
                {
                    Id = e.Id, Group = e.Group, Description = e.Description,
                    Value = inSave.TryGetValue(e.Id, out bool v) ? v : (bool?)null,
                });
            }
            foreach (SaveFlag f in _savesFlags)
            {
                if (known.Contains(f.Id) || !Match(f.Id, null)) continue;
                bool once = f.Id.StartsWith("Yarn.Internal.Once.", StringComparison.Ordinal);
                if (once && !_showOnceLines) continue;
                Add(once ? "Once-only dialogue lines" : "Other (in save, not in catalog)",
                    new FlagRow { Id = f.Id, Value = f.Value, Description = once ? "one-time line already said" : "" });
            }

            foreach (string g in groups)
            {
                _flagRows.Add(new FlagRow { IsHeader = true, Id = g, Group = g });
                _flagRows.AddRange(byGroup[g]);
            }
        }

        private float FlagRowsHeight() => 40 + 34 + _flagRows.Count * 30 + (_flagClearConfirm ? 70 : 0) + 8;

        // AddTab hands back the handle that removes the tab again; Awake uses it
        // to take the tabs down if startup fails after this point.
        private readonly List<IDisposable> _toolTabs = new List<IDisposable>();

        private void AddToolTabs()
        {
            _toolTabs.Add(ToolWindow.AddTab(PluginGuid, "Activity log", area => DrawWithStatus(area, DrawActivityLog), 100));
            _toolTabs.Add(ToolWindow.AddTab(PluginGuid, "Translation", area => DrawWithStatus(area, DrawTools), 101));
            _toolTabs.Add(ToolWindow.AddTab(PluginGuid, "Saves", DrawSaves, 102));
            _toolTabs.Add(ToolWindow.AddTab(PluginGuid, "About", DrawAbout, 103));
            ToolWindow.AddCommand(PluginGuid, "tl", "tl status | tl reload | tl find <text> | tl review", ConsoleCommand,
                args => args.Length == 1 ? new[] { "status", "reload", "find", "review" } : new string[0]);
        }

        // The console's "tl" command (experimental; the framework's Console tab).
        private string ConsoleCommand(string[] args)
        {
            string what = args.Length > 0 ? args[0].ToLowerInvariant() : "";
            switch (what)
            {
                case "status":
                    return $"Language {TargetLocale.Value}, {TranslationStore.EntryCount} entries loaded, {LineResolution.RecordCount} line records, {LineResolution.ReviewCount} line(s) to review, graphics {SystemInfo.graphicsDeviceType}.";
                case "reload":
                    TranslationStore.Load(PluginDirectory, TargetLocale.Value);
                    TmpTextHook.RefreshAll();
                    return $"Reloaded {TargetLocale.Value}: {TranslationStore.EntryCount} entries.";
                case "find":
                {
                    if (args.Length < 2)
                    {
                        return "tl find <text>: rows whose translation contains the text";
                    }
                    string needle = string.Join(" ", args, 1, args.Length - 1);
                    var lines = new List<string>();
                    foreach (KeyValuePair<string, string> kv in TranslationStore.Entries)
                    {
                        if (kv.Value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            lines.Add($"{kv.Key}  {kv.Value}");
                            if (lines.Count >= 20)
                            {
                                lines.Add("(more; narrow the text)");
                                break;
                            }
                        }
                    }
                    return lines.Count == 0 ? "No translation contains that." : string.Join("\n", lines);
                }
                case "review":
                {
                    List<LineResolution.Review> reviews = LineResolution.ReviewList;
                    if (reviews.Count == 0)
                    {
                        return "No line needs review: every line shown so far matched its exact English.";
                    }
                    var lines = new List<string>();
                    foreach (LineResolution.Review r in reviews)
                    {
                        lines.Add($"{r.LineId ?? r.Key}  {r.Node} / {r.Speaker}  matched by {r.Layer}");
                    }
                    return string.Join("\n", lines);
                }
                default:
                    return "tl status | tl reload | tl find <text> | tl review";
            }
        }

        // Reloaded by the framework (or unloaded at quit): the tabs and the
        // console command go with this build. Hot reload polls from Update, so
        // it stops with the component.
        private void OnDestroy()
        {
            RemoveToolTabs();
        }

        private void RemoveToolTabs()
        {
            foreach (IDisposable tab in _toolTabs)
            {
                // Called from the failure path of Awake; it must not throw a
                // second exception over the one being reported.
                try { tab.Dispose(); } catch { }
            }
            _toolTabs.Clear();
        }

        private bool _followLog = true;
        private bool _logNeedsScroll;
        private float _logContentHeight;
        private float _logContentWidth = -1;

        // The language and entry count above the translation tabs.
        private void DrawWithStatus(Rect area, Action<Rect> draw)
        {
            string localeStatus = _pendingLocale == null ? TargetLocale.Value : TargetLocale.Value + " -> " + _pendingLocale;
            GUI.Label(new Rect(area.x, area.y, area.width, 24),
                $"Locale: {localeStatus}    |    Entries: {TranslationStore.EntryCount}" + (LineResolution.ReviewCount > 0 ? $"    |    Review: {LineResolution.ReviewCount} line(s)" : ""), S.MutedLabel);
            draw(new Rect(area.x, area.y + 32, area.width, Mathf.Max(40, area.height - 32)));
        }

        private Vector2 _aboutScroll;

        // Every literal here is ASCII on purpose. Rasterizing a glyph the menu
        // font has not seen yet uploads a texture, and on Direct3D 12 an upload
        // while the menu is open is what crashes the game (Unity UUM-140564).
        // The values that are not ours to choose - the install path, the folder
        // names under Translations and the configured locale - cannot be kept
        // ASCII, so Plugin.PrepareWindowCharacters feeds them to the font at
        // startup instead.
        private void DrawAbout(Rect area)
        {
            ToolWindow.Fill(area, ToolWindow.InsetColor);
            float innerWidth = Mathf.Max(100, area.width - 36);

            string version = PluginVersion;
            string build = BuildId();

            var lines = new List<KeyValuePair<GUIStyle, string>>();
            void Head(string t) => lines.Add(new KeyValuePair<GUIStyle, string>(S.Label, t));
            void Body(string t) => lines.Add(new KeyValuePair<GUIStyle, string>(S.WrappedLabel, t));

            Head("DRAG'N WASH LOCALIZATION");
            Body($"Version {version}" + (string.IsNullOrEmpty(build) ? "" : $"   (build {build})"));
            Body("An unofficial fan-made multilingual localization mod. It is not affiliated with, endorsed by, or supported by the developers of Drag'n Wash.");
            Body("It changes no game files: the game's text is replaced as it is shown, so a game update never breaks your install, and uninstalling leaves the game as it was.");
            Body("");

            Head("BUILT ON DRAG'N WASH MODFRAMEWORK");
            Body($"Core {DragNWash.ModFramework.ModFramework.Version}. A small shared base for Drag'n Wash mods: the Mods screen in Options, update notices, one installer every mod can ship, and libraries for text, dialogue, assets, saves and this tool window. Its first rule is that every mod runs safely together.");
            Body("The framework is a separate open project. Anyone can build a mod on it: github.com/TomXV/dragnwash-modframework");
            Body("");

            Head("TOOLS IN THIS WINDOW");
            Body("For translators and mod makers, not needed for playing: the Translation tab (working copies, exports, hot reload), the Saves tab, the framework's Assets tab (see what is loaded, replace textures) and Console (the log with levels, and commands).");
            Body("What people make with these tools is their own work and their own responsibility. Nothing here exports or ships the game's files as part of this mod.");
            Body("");

            Head("CREDITS");
            Body("Created by TomXV. Translation files by TomXV, with corrections from contributors credited in the README and in each language file.");
            Body("Logo by Mister ERIO, who also drew the framework's Mods button.");
            Body("Source, issues and translation contributions: github.com/TomXV/dragnwash-localization");
            Body("");

            Head("LANGUAGES");
            Body("Supervised by the author: Japanese (ja), Simplified Chinese (zh-Hans).");
            Body("Converted from the supervised Simplified Chinese: Traditional Chinese (zh-Hant).");
            Body("Proofread by a native speaker: Korean (ko), by Hotcake.");
            Body("Provisional, not reviewed by native speakers: German (de), French (fr), Spanish (es), Brazilian Portuguese (pt-BR), Russian (ru), Polish (pl), Hebrew (he), Ukrainian (uk), Thai (th), Vietnamese (vi).");
            Body("Just for fun: Esperanto (eo), Toki Pona (tok).");
            Body("Provisional lines may read unnaturally. Native speakers: corrections are very welcome as pull requests.");
            Body("Installed in this copy: " + string.Join(", ", _availableLocales ?? new string[0]));
            Body("");

            Head("LICENSE");
            Body("The mod is MIT licensed (see LICENSE in the repository).");
            Body("The bundled menu font is Noto Sans JP, (c) 2014-2021 Adobe, with Reserved Font Name 'Source', under the SIL Open Font License 1.1. Its full text ships next to the plugin as dragnwash-menufont-LICENSE.txt.");
            Body("");

            Head("THIS SESSION");
            Body($"Language: {TargetLocale.Value}    Entries loaded: {TranslationStore.EntryCount}" + (LineResolution.ReviewCount > 0 ? $"    Lines to review: {LineResolution.ReviewCount}" : ""));
            Body($"Game: Unity {Application.unityVersion}    Graphics: {SystemInfo.graphicsDeviceType}");
            Body($"Platform: {Application.platform}");
            Body($"Plugin folder: {PluginDirectory}");

            float contentHeight = 12;
            var heights = new float[lines.Count];
            for (int i = 0; i < lines.Count; i++)
            {
                heights[i] = string.IsNullOrEmpty(lines[i].Value)
                    ? 10
                    : lines[i].Key.CalcHeight(new GUIContent(lines[i].Value), innerWidth - 24);
                contentHeight += heights[i] + 4;
            }
            contentHeight += RowHeight + 16;

            ToolWindow.ApplyScroll(area, ref _aboutScroll);
            _aboutScroll = GUI.BeginScrollView(area, _aboutScroll,
                new Rect(0, 0, innerWidth, Mathf.Max(area.height, contentHeight)), false, false);
            float y = 12;
            for (int i = 0; i < lines.Count; i++)
            {
                if (!string.IsNullOrEmpty(lines[i].Value))
                {
                    GUI.Label(new Rect(12, y, innerWidth - 24, heights[i]), lines[i].Value, lines[i].Key);
                }
                y += heights[i] + 4;
            }
            y += 6;
            if (GUI.Button(new Rect(12, y, 260, RowHeight), "Copy the repository address", S.Button))
            {
                GUIUtility.systemCopyBuffer = "https://github.com/TomXV/dragnwash-localization";
                ToolWindow.ShowNotice("Repository address copied to the clipboard.");
            }
            GUI.EndScrollView();
        }

        // "0.3.1+<commit>" is stamped into the assembly at build time; show the
        // commit so a bug report says exactly which build is running.
        private static string BuildId()
        {
            try
            {
                var attr = (System.Reflection.AssemblyInformationalVersionAttribute)Attribute.GetCustomAttribute(
                    typeof(Plugin).Assembly, typeof(System.Reflection.AssemblyInformationalVersionAttribute));
                string v = attr != null ? attr.InformationalVersion : null;
                int plus = v != null ? v.IndexOf('+') : -1;
                if (plus < 0) return string.Empty;
                string commit = v.Substring(plus + 1);
                return commit.Length > 7 ? commit.Substring(0, 7) : commit;
            }
            catch
            {
                return string.Empty;
            }
        }

        private void DrawActivityLog(Rect area)
        {
            if (GUI.Button(new Rect(area.x, area.y, 122, RowHeight), _followLog ? "Follow: ON" : "Follow: OFF",
                _followLog ? S.SelectedButton : S.Button))
            {
                _followLog = !_followLog;
                _logNeedsScroll = _followLog;
            }
            if (GUI.Button(new Rect(area.x + 130, area.y, 110, RowHeight), "Clear log", S.Button))
            {
                lock (LogBuffer)
                {
                    LogBuffer.Clear();
                    _lastLogMessage = null;
                    _logVersion++;
                }
                TranslationStore.ResetAppliedOnceTracking();
                _logScroll = Vector2.zero;
                ToolWindow.ShowNotice("Log cleared.");
            }

            bool changed = false;
            int count;
            lock (LogBuffer)
            {
                count = LogBuffer.Count;
                if (_lastLogVersion != _logVersion)
                {
                    _lastLogVersion = _logVersion;
                    _logText = string.Join("\n", LogBuffer.ToArray());
                    changed = true;
                }
            }

            var viewport = new Rect(area.x, area.y + 40, area.width, Mathf.Max(20, area.height - 40));
            ToolWindow.Fill(viewport, ToolWindow.InsetColor);
            float contentWidth = Mathf.Max(40, viewport.width - 24);
            if (changed || !Mathf.Approximately(_logContentWidth, contentWidth))
            {
                _logContentWidth = contentWidth;
                _logContentHeight = string.IsNullOrEmpty(_logText) ? 0 :
                    S.LogLabel.CalcHeight(new GUIContent(_logText), contentWidth);
                if (_followLog) _logNeedsScroll = true;
            }

            Event current = Event.current;
            if (viewport.Contains(current.mousePosition) &&
                (current.type == EventType.ScrollWheel ||
                 (current.type == EventType.MouseDown && current.mousePosition.x >= viewport.xMax - 20)))
            {
                _followLog = false;
                _logNeedsScroll = false;
            }
            float maxScroll = Mathf.Max(0, _logContentHeight - viewport.height);
            _logScroll.y = _logNeedsScroll ? maxScroll : Mathf.Clamp(_logScroll.y, 0, maxScroll);
            _logNeedsScroll = false;

            if (ToolWindow.ApplyScroll(viewport, ref _logScroll)) { _followLog = false; _logNeedsScroll = false; }
            _logScroll = GUI.BeginScrollView(viewport, _logScroll,
                new Rect(0, 0, contentWidth, Mathf.Max(viewport.height - 1, _logContentHeight)), false, true);
            if (count == 0)
                GUI.Label(new Rect(12, 12, contentWidth - 24, 64),
                    "No activity yet.\nOpen a game menu or dialogue to capture text.", S.WrappedLabel);
            else
                GUI.Label(new Rect(0, 0, contentWidth, _logContentHeight), _logText, S.LogLabel);
            GUI.EndScrollView();

            GUI.Label(new Rect(area.x + 250, area.y, Mathf.Max(0, area.width - 250), RowHeight),
                $"{count} / {MaxLogLines}", S.MutedLabel);
        }

        private void DrawTools(Rect area)
        {
            ToolWindow.Fill(area, ToolWindow.InsetColor);
            float innerWidth = Mathf.Max(100, area.width - 36);
            const float contentHeight = 394;
            ToolWindow.ApplyScroll(area, ref _localeScroll);
            _localeScroll = GUI.BeginScrollView(area, _localeScroll,
                new Rect(0, 0, innerWidth, contentHeight + Mathf.Ceil(_availableLocales.Length / 3f) * 38), false, false);
            GUI.Label(new Rect(12, 8, innerWidth - 12, 26), "LANGUAGE", S.Label);
            float buttonWidth = (innerWidth - 28) / 3;
            float y = 42;
            for (int i = 0; i < _availableLocales.Length; i++)
            {
                string locale = _availableLocales[i];
                bool selected = locale == TargetLocale.Value;
                string label = LocaleDisplayName(locale);
                if (!MenuFontCanDraw(label)) label = locale;
                if (GUI.Button(new Rect(12 + (i % 3) * (buttonWidth + 8), y + (i / 3) * 38, buttonWidth, RowHeight),
                    selected ? label + "  [active]" : label, selected ? S.SelectedButton : S.Button))
                {
                    _pendingLocale = locale;
                    _pendingLocalePersist = true;
                    ToolWindow.ShowNotice("See Activity log for the language change result.");
                }
            }
            if (_availableLocales.Length == 0)
                GUI.Label(new Rect(12, y, innerWidth, RowHeight), "No language folders installed.", S.MutedLabel);
            y += Mathf.Max(1, Mathf.Ceil(_availableLocales.Length / 3f)) * 38 + 12;
            GUI.Label(new Rect(12, y, innerWidth - 12, 26), "TRANSLATION TOOLS", S.Label);
            y += 34;
            if (GUI.Button(new Rect(12, y, innerWidth - 12, RowHeight), _pendingDump ? "Dialogue export queued..." : $"Export loaded dialogue  ({DumpDialogueKey.Value})", S.Button))
            {
                _pendingDump = true;
                ToolWindow.ShowNotice("See Activity log for the dialogue export result.");
            }
            y += 38;
            if (GUI.Button(new Rect(12, y, innerWidth - 12, RowHeight), _pendingUiDump ? "UI text export queued..." : $"Export UI text  ({DumpUiTextKey.Value})", S.Button))
            {
                _pendingUiDump = true;
                ToolWindow.ShowNotice("See Activity log for the UI text export result.");
            }
            y += 38;
            if (GUI.Button(new Rect(12, y, innerWidth - 12, RowHeight), _pendingLayoutCheck ? "Layout check queued..." : "Check translation layout", S.Button))
            {
                _pendingLayoutCheck = true;
                ToolWindow.ShowNotice("See Activity log for the layout check result.");
            }
            y += 38;
            if (GUI.Button(new Rect(12, y, innerWidth - 12, RowHeight), _pendingWorkingCopy ? "Export queued..." : "Export working copy (English beside each line)", S.Button))
            {
                _pendingWorkingCopy = true;
                ToolWindow.ShowNotice("See Activity log for the working copy result.");
            }
            y += 38;
            if (GUI.Button(new Rect(12, y, innerWidth - 12, RowHeight), _pendingHashFile ? "Hashing queued..." : "Hash for commit (rebuild strings.csv, no English)", S.Button))
            {
                _pendingHashFile = true;
                ToolWindow.ShowNotice("See Activity log for the hashing result.");
            }
            y += 38;
            if (GUI.Button(new Rect(12, y, innerWidth - 12, RowHeight), _pendingFlowDump ? "Flow export queued..." : "Export game flow (levels + dialogue graph)", S.Button))
            {
                _pendingFlowDump = true;
                ToolWindow.ShowNotice("See Activity log for the flow export result.");
            }
            y += 42;
            GUI.Label(new Rect(12, y, innerWidth - 12, 52),
                "Exports are written to Translations/_discovered.\nLanguage changes apply to text already on screen.", S.WrappedLabel);
            GUI.EndScrollView();
        }

        private Vector2 _savesScroll;
        private string _savesSlot;
        private float _savesRefreshAt;
        private List<string> _savesSlots = new List<string>();
        private List<SaveSnapshot> _savesList = new List<SaveSnapshot>();
        private int _savesLevel = -1;

        // How long a held control may hold the refresh back. Without a limit a
        // press that never gets its release - the window loses focus mid-click,
        // say - would freeze the listing for the rest of the session.
        private const float SavesRefreshHoldLimit = 5f;

        // Snapshots of the game's own save file, one per write, newest first.
        // Restore puts one back; the player then reloads the slot from the
        // title screen. Listing is cached and refreshed every couple of
        // seconds so OnGUI does not hit the disk every frame.
        private void DrawSaves(Rect area)
        {
            ToolWindow.Fill(area, ToolWindow.InsetColor);
            float innerWidth = Mathf.Max(100, area.width - 36);

            // Not while a control is held. Both lists below are addressed by
            // index, and a snapshot taken between the press and the release
            // shifts every row down without changing the control ids, so the
            // click would land on a different entry than the one under the
            // cursor. GUI.Button releases the control before it returns true,
            // so the handlers that set _savesRefreshAt = 0 still take effect on
            // the next pass.
            bool held = GUIUtility.hotControl != 0 &&
                        Time.unscaledTime < _savesRefreshAt + SavesRefreshHoldLimit;

            if (Time.unscaledTime >= _savesRefreshAt && !held)
            {
                _savesRefreshAt = Time.unscaledTime + 2f;
                _savesSlots = GameSaves.Slots();
                if (_savesSlot == null || !_savesSlots.Contains(_savesSlot))
                {
                    _savesSlot = _savesSlots.Count > 0 ? _savesSlots[0] : null;
                }
                _savesList = _savesSlot != null ? GameSaves.Snapshots(_savesSlot) : new List<SaveSnapshot>();
                _savesFlags = _savesSlot != null ? GameSaves.ReadFlags(_savesSlot) : new List<SaveFlag>();
                _savesLevel = _savesSlot != null ? GameSaves.ReadLevel(_savesSlot) : -1;
                _newestMatchesSave = _savesSlot != null && _savesList.Count > 0 && GameSaves.SnapshotMatchesSave(_savesSlot, _savesList[0]);
                RebuildFlagRows();
            }

            float y = 8;
            GUI.Label(new Rect(area.x + 12, area.y + y, innerWidth, 26), "SAVE SLOT", S.Label);
            y += 32;
            float x = 12;
            foreach (string slot in _savesSlots)
            {
                string shown = GameSaves.ShortName(slot);
                float w = 90;
                if (GUI.Button(new Rect(area.x + x, area.y + y, w, RowHeight), shown, slot == _savesSlot ? S.SelectedButton : S.Button))
                {
                    _savesSlot = slot;
                    // The rest of the listing reloads at the top of the next
                    // pass, but the progress editor below reads the level in
                    // this one, and it must not show the slot we just left.
                    _savesLevel = GameSaves.ReadLevel(slot);
                    _savesRefreshAt = 0;
                }
                x += w + 8;
            }
            if (_savesSlots.Count == 0)
                GUI.Label(new Rect(area.x + 12, area.y + y, innerWidth, RowHeight), "No save files found.", S.MutedLabel);
            y += 42;

            // ---- progress editor: levelIndex and the boolean flags ----------
            if (_savesSlot != null)
            {
                // Cached with the rest of the listing: ReadLevel reads and
                // parses the whole save file, and OnGUI runs several times per
                // rendered frame.
                int current = _savesLevel;
                if (_editLevelSlot != _savesSlot || _editLevelBase != current)
                {
                    _editLevelSlot = _savesSlot;
                    _editLevelBase = current;
                    _editLevel = current;
                    _progressConfirm = false;
                }

                GUI.Label(new Rect(area.x + 12, area.y + y, innerWidth, 26), "PROGRESS", S.Label);
                y += 32;
                GUI.Label(new Rect(area.x + 12, area.y + y, 200, RowHeight),
                    _editLevel == current ? $"level {current}" : $"level {current}  ->  {_editLevel}", S.Label);
                if (GUI.Button(new Rect(area.x + 216, area.y + y, 40, RowHeight), "-", S.Button) && _editLevel > 0)
                {
                    _editLevel--; _progressConfirm = false;
                }
                if (GUI.Button(new Rect(area.x + 262, area.y + y, 40, RowHeight), "+", S.Button))
                {
                    _editLevel++; _progressConfirm = false;
                }
                bool canApply = _editLevel != current && current >= 0;
                GUI.enabled = canApply;
                if (GUI.Button(new Rect(area.x + 314, area.y + y, 90, RowHeight), "Apply", S.Button))
                {
                    if (_editLevel > current)
                    {
                        _progressConfirm = true;   // going forward can spoil the story
                    }
                    else
                    {
                        Log("[saves] " + GameSaves.SetLevel(PluginGuid, _savesSlot, _editLevel));
                        _savesRefreshAt = 0;
                    }
                }
                GUI.enabled = true;
                // In a narrow window the Flags button goes on its own line
                // instead of past the right edge.
                float flagsX = area.x + 414, flagsWidth = innerWidth - 414 + 12;
                if (flagsWidth < 80)
                {
                    y += 36;
                    flagsX = area.x + 12;
                    flagsWidth = Mathf.Min(160, innerWidth);
                }
                if (GUI.Button(new Rect(flagsX, area.y + y, flagsWidth, RowHeight),
                    _showFlags ? "Hide flags" : "Flags...", S.Button))
                {
                    _showFlags = !_showFlags;
                }
                y += 36;

                if (_progressConfirm)
                {
                    var warn = new GUIContent($"Warning: jumping ahead to level {_editLevel} may spoil content you have not seen. Continue?");
                    float wh = S.Label.CalcHeight(warn, innerWidth);
                    GUI.Label(new Rect(area.x + 12, area.y + y, innerWidth, wh), warn, S.Label);
                    y += wh + 4;
                    if (GUI.Button(new Rect(area.x + 12, area.y + y, 120, RowHeight), "Yes, continue", S.Button))
                    {
                        Log("[saves] " + GameSaves.SetLevel(PluginGuid, _savesSlot, _editLevel));
                        _progressConfirm = false;
                        _savesRefreshAt = 0;
                    }
                    if (GUI.Button(new Rect(area.x + 140, area.y + y, 90, RowHeight), "Cancel", S.Button))
                    {
                        _progressConfirm = false;
                        _editLevel = current;
                    }
                    y += 36;
                }
            }

            // Both explanatory labels wrap on a narrow window, so size them from
            // the text instead of assuming one line.
            var historyText = new GUIContent($"HISTORY  ({_savesList.Count} snapshot(s), newest first)");
            float historyHeight = S.Label.CalcHeight(historyText, innerWidth);
            GUI.Label(new Rect(area.x + 12, area.y + y, innerWidth, historyHeight), historyText, S.Label);
            y += historyHeight + 6;

            var footerText = new GUIContent("A snapshot is taken whenever the game writes the save. After Restore: go to the title screen and load the slot. Saving in game overwrites it again.");
            float footerHeight = S.MutedLabel.CalcHeight(footerText, innerWidth);
            // The footer sits at the bottom; in a window too short for it, it
            // is left out rather than drawn over the rows above.
            bool footerFits = area.height - y - footerHeight - 12 >= 60;
            if (!footerFits)
            {
                footerHeight = 0;
            }

            var view = new Rect(area.x, area.y + y, area.width, Mathf.Max(40, area.height - y - footerHeight - 12));
            ToolWindow.ApplyScroll(view, ref _savesScroll);
            _savesScroll = GUI.BeginScrollView(view, _savesScroll, new Rect(0, 0, innerWidth, Mathf.Max(view.height, _showFlags && _savesSlot != null ? FlagRowsHeight() : _savesList.Count * 36)), false, false);
            // The flag editor replaces the history list while it is open, so
            // the flags start at the top instead of below 30 snapshot rows.
            bool flagsOpen = _showFlags && _savesSlot != null;
            for (int i = 0; i < _savesList.Count && !flagsOpen; i++)
            {
                SaveSnapshot s = _savesList[i];
                // "current" only when the newest snapshot really is the save on
                // disk; after a progress or flag edit it is the pre-edit state.
                bool isCurrent = i == 0 && _newestMatchesSave;
                GUI.Label(new Rect(12, i * 36, innerWidth - 130, RowHeight), (isCurrent ? "current   " : "") + s.Label, S.Label);
                if (!isCurrent && GUI.Button(new Rect(innerWidth - 110, i * 36, 98, RowHeight), "Restore", S.Button))
                {
                    _pendingRestoreSlot = _savesSlot;
                    _pendingRestoreSnapshot = s;
                    _savesRefreshAt = 0;
                }
            }
            if (flagsOpen)
            {
                float fy = 4;
                GUI.Label(new Rect(12, fy, innerWidth, 26), "EVENT FLAGS   click a value: unset -> true -> false", S.Label);
                fy += 30;

                // Search box, once-lines toggle, and the bulk reset.
                int dbg = FlagPanelDebug != null ? FlagPanelDebug.Value : 0;
                if ((dbg & 1) == 0)
                {
                    string newFilter = GUI.TextField(new Rect(12, fy, Mathf.Max(80, innerWidth - 330), RowHeight), _flagFilter ?? "", S.TextField);
                    if (newFilter != _flagFilter)
                    {
                        _flagFilter = newFilter;
                        RebuildFlagRows();
                    }
                }
                if (GUI.Button(new Rect(innerWidth - 310, fy, 120, RowHeight), _showOnceLines ? "Hide once-lines" : "Show once-lines", S.Button))
                {
                    _showOnceLines = !_showOnceLines;
                    RebuildFlagRows();
                }
                if (GUI.Button(new Rect(innerWidth - 182, fy, 170, RowHeight), "Reset all to false...", S.Button))
                {
                    _flagClearConfirm = !_flagClearConfirm;
                }
                fy += 34;

                if (_flagClearConfirm)
                {
                    GUI.Label(new Rect(12, fy, innerWidth - 24, 30),
                        "Set every flag in this save to false (the level index is kept). The current save is snapshotted first. Continue?", S.MutedLabel);
                    fy += 32;
                    if (GUI.Button(new Rect(12, fy, 120, RowHeight), "Yes, reset", S.Button))
                    {
                        var all = new List<KeyValuePair<string, bool>>();
                        foreach (SaveFlag f in _savesFlags) all.Add(new KeyValuePair<string, bool>(f.Id, false));
                        Log("[saves] " + GameSaves.SetFlags(PluginGuid, _savesSlot, all, $"all {all.Count} flags set to false"));
                        _flagClearConfirm = false;
                        _savesRefreshAt = 0;
                    }
                    if (GUI.Button(new Rect(140, fy, 90, RowHeight), "Cancel", S.Button))
                    {
                        _flagClearConfirm = false;
                    }
                    fy += 38;
                }

                for (int i = 0; i < _flagRows.Count && (dbg & 4) == 0; i++)
                {
                    FlagRow r = _flagRows[i];
                    float ry = fy + i * 30;
                    if (r.IsHeader)
                    {
                        if ((dbg & 8) == 0)
                            GUI.Label(new Rect(12, ry + 4, innerWidth - 24, 26), r.Id.ToUpperInvariant(), S.Label);
                        continue;
                    }
                    GUI.Label(new Rect(24, ry, Mathf.Max(60, innerWidth * 0.42f), RowHeight), r.Id, S.Label);
                    if ((dbg & 2) == 0)
                        GUI.Label(new Rect(24 + Mathf.Max(60, innerWidth * 0.42f), ry, Mathf.Max(40, innerWidth * 0.58f - 130), RowHeight), r.Description ?? "", S.MutedLabel);
                    string shown = r.Value == null ? "unset" : (r.Value.Value ? "true" : "false");
                    GUIStyle st = r.Value == true ? S.SelectedButton : S.Button;
                    if (GUI.Button(new Rect(innerWidth - 90, ry, 78, RowHeight), shown, st))
                    {
                        bool next = r.Value != true;   // unset -> true, true -> false, false -> true
                        Log("[saves] " + GameSaves.SetFlag(PluginGuid, _savesSlot, r.Id, next));
                        _savesRefreshAt = 0;
                    }
                }
            }
            GUI.EndScrollView();

            if (footerFits)
            {
                GUI.Label(new Rect(area.x + 12, area.y + area.height - footerHeight - 6, innerWidth, footerHeight), footerText, S.MutedLabel);
            }
        }

        // True when every character of the text has a glyph in the menu font.
        // With Unity's built-in font (no CJK), "日本語" would draw as nothing,
        // so the caller shows the locale code instead.
        private bool MenuFontCanDraw(string text)
        {
            return ToolWindow.CanDraw(text);
        }

        // Shown on the language buttons; the folder name is what the config stores.
        // Translators set the name in Translations/<locale>/name.txt.
        private static readonly Dictionary<string, string> _localeNames = new Dictionary<string, string>();

        private static string LocaleDisplayName(string locale)
        {
            if (_localeNames.TryGetValue(locale, out string cached))
            {
                return cached;
            }

            string name = locale == "en" ? "English" : locale;
            try
            {
                string path = Path.Combine(PluginDirectory, "Translations", locale, "name.txt");
                if (File.Exists(path))
                {
                    string text = File.ReadAllText(path).Trim();
                    if (text.Length > 0)
                    {
                        name = text;
                    }
                }
            }
            catch (Exception)
            {
                // Fall back to the folder name.
            }

            _localeNames[locale] = name;
            return name;
        }
}
}
