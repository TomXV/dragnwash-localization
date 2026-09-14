using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DragNWashLocalization
{
    // Maps the text TMP_Text.text is set to, straight to a translation. There
    // is no table or key concept for translators to learn: a row is the exact
    // on-screen English string and its translation.
    //
    // The published file does not contain that English, though. Rows are keyed
    // by TranslationKey.Hash of the source, so the repository redistributes
    // none of the script. Both layouts are accepted in the same file:
    //
    //   key,translation           what the repository ships
    //   source_en,translation     what a translator writes locally, against
    //                             the exports the plugin produces for them
    //
    // A translator works in the second form (hot reload picks it up), and runs
    // "Hash strings.csv for commit" or tools/hash-strings.ps1 before opening a
    // pull request. The plugin hashes each string it is asked to translate and
    // looks the hash up, so a plain row and its hashed form behave identically.
    internal static class TranslationStore
    {
        private static readonly Dictionary<string, string> ByKey = new Dictionary<string, string>(StringComparer.Ordinal);
        // line:xxxxxxxx -> translation. Wins over ByKey for that one line of
        // dialogue; see LineIdContext.
        private static readonly Dictionary<string, string> ByLineId = new Dictionary<string, string>(StringComparer.Ordinal);
        // Source text for keys that arrived as source_en rows. Only for
        // logging and exports; lookups never need it.
        private static readonly Dictionary<string, string> SourceByKey = new Dictionary<string, string>(StringComparer.Ordinal);
        // TryGetTranslation runs on every text assignment, so hash once per
        // distinct string rather than once per call.
        private static readonly ConcurrentDictionary<string, string> HashCache = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
        private static readonly ConcurrentDictionary<string, byte> DiscoveredText = new ConcurrentDictionary<string, byte>();
        private static readonly ConcurrentDictionary<string, byte> AppliedOnce = new ConcurrentDictionary<string, byte>();
        private static readonly List<string> PendingDiscoveredLines = new List<string>();

        private static string _pluginDirectory;

        public static int EntryCount => ByKey.Count + ByLineId.Count;

        public static int LineEntryCount => ByLineId.Count;

        // key -> translation, line-ID rows included. HotReload diffs these; use
        // DescribeKey to label.
        public static IEnumerable<KeyValuePair<string, string>> Entries
        {
            get
            {
                foreach (KeyValuePair<string, string> kv in ByKey) yield return kv;
                foreach (KeyValuePair<string, string> kv in ByLineId) yield return kv;
            }
        }

        // The English for a key when this locale's file supplied it, else the
        // key itself prefixed so it is obviously not text.
        public static string DescribeKey(string key)
        {
            if (TranslationKey.LooksLikeLineId(key))
            {
                return key;
            }
            return SourceByKey.TryGetValue(key, out string source) ? source : "#" + key;
        }

        public static string KeyFor(string source)
        {
            return HashCache.GetOrAdd(source, TranslationKey.Hash);
        }

        // Every translation in every installed locale. FontFallback rasterizes
        // all of these glyphs at startup so switching locale mid-game never has
        // to grow an atlas (a runtime texture upload that trips the Direct3D 12
        // crash). Locale folders under Translations/ starting with '_' are
        // runtime output and skipped.
        public static IEnumerable<string> CollectAllLocalesTexts(string pluginDirectory)
        {
            string translationsDir = Path.Combine(pluginDirectory, "Translations");
            if (!Directory.Exists(translationsDir))
            {
                yield break;
            }

            foreach (string localeDir in Directory.GetDirectories(translationsDir))
            {
                string name = Path.GetFileName(localeDir);
                if (name.StartsWith("_"))
                {
                    continue;
                }

                string path = Path.Combine(localeDir, "strings.csv");
                if (!File.Exists(path))
                {
                    continue;
                }

                foreach (var row in CsvReader.ReadRows(path))
                {
                    if (row.TryGetValue("translation", out var translation) &&
                        !string.IsNullOrEmpty(translation))
                    {
                        yield return translation;
                    }
                }
            }
        }

        // The same texts as CollectAllLocalesTexts, grouped by locale folder,
        // so fonts can be prepared per language.
        public static Dictionary<string, List<string>> CollectTextsByLocale(string pluginDirectory)
        {
            var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            string translationsDir = Path.Combine(pluginDirectory, "Translations");
            if (!Directory.Exists(translationsDir))
            {
                return result;
            }
            foreach (string localeDir in Directory.GetDirectories(translationsDir))
            {
                string name = Path.GetFileName(localeDir);
                if (name.StartsWith("_")) continue;
                string path = Path.Combine(localeDir, "strings.csv");
                if (!File.Exists(path)) continue;
                var texts = new List<string>();
                try
                {
                    foreach (var row in CsvReader.ReadRows(path))
                    {
                        if (row.TryGetValue("translation", out var translation) && !string.IsNullOrEmpty(translation))
                        {
                            texts.Add(translation);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log($"[font] Could not read {path} for font preparation: {ex.Message}");
                }
                result[name] = texts;
            }
            return result;
        }

        // Every string this locale can put on screen. FontFallback rasterizes
        // their glyphs up front so no atlas has to grow mid-gameplay.
        public static IEnumerable<string> TranslatedTexts
        {
            get
            {
                foreach (string t in ByKey.Values) yield return t;
                foreach (string t in ByLineId.Values) yield return t;
            }
        }

        public static void Load(string pluginDirectory, string locale)
        {
            _pluginDirectory = pluginDirectory;
            IgnoreRules.Load(pluginDirectory);
            ByKey.Clear();
            ByLineId.Clear();
            SourceByKey.Clear();
            DiscoveredText.Clear();
            AppliedOnce.Clear();
            lock (PendingDiscoveredLines)
            {
                PendingDiscoveredLines.Clear();
            }

            if (!string.IsNullOrEmpty(locale))
            {
                // The published file first, then the translator's plain working
                // copy on top if there is one, so edits made there win.
                string localeDir = Path.Combine(pluginDirectory, "Translations", locale);
                LoadFile(Path.Combine(localeDir, "strings.csv"), locale + "/strings.csv");
                LoadFile(WorkingCopy.PathFor(pluginDirectory, locale), "_discovered/" + WorkingCopy.FileNameFor(locale));
            }

            RebuildDiscoveredFile();
        }

        private static void LoadFile(string path, string label)
        {
            try
            {
                if (File.Exists(path))
                {
                    int badKeys = 0;
                    var examples = new List<string>();
                    foreach (var row in CsvReader.ReadRows(path))
                    {
                        if (!row.TryGetValue("translation", out var translation) || string.IsNullOrEmpty(translation))
                        {
                            continue;
                        }

                        string key = ResolveKey(row, out string source, out bool malformed);
                        if (key == null)
                        {
                            if (malformed)
                            {
                                badKeys++;
                                if (examples.Count < MaxMalformedExamples)
                                {
                                    examples.Add(DescribeMalformed(row));
                                }
                            }
                            continue;
                        }

                        if (TranslationKey.LooksLikeLineId(key))
                        {
                            ByLineId[key] = translation;
                            continue;
                        }

                        ByKey[key] = translation;
                        if (source != null)
                        {
                            SourceByKey[key] = source;
                        }
                    }

                    if (badKeys > 0)
                    {
                        Plugin.Log($"[load] {badKeys} row(s) in {label} were skipped: {string.Join("; ", examples)}{(badKeys > examples.Count ? "; ..." : "")}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Typically a sharing violation from an editor holding the file
                // exclusively. Keep going with whatever else loaded.
                Plugin.Log($"[load] Could not read {label}: {ex.Message}. Close the program holding it and save the file again to hot reload.");
            }
        }

        private const int MaxMalformedExamples = 3;

        // Why a row was skipped, in terms a translator can act on.
        private static string DescribeMalformed(Dictionary<string, string> row)
        {
            row.TryGetValue("key", out string key);
            row.TryGetValue("source_en", out string text);
            key = key?.Trim();
            if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(key))
            {
                return TranslationKey.LooksLikeKey(key.ToLowerInvariant())
                    ? $"key {key} does not match its English \"{text}\" (whose key is {KeyFor(text)}); if the game was updated, export the working copy again"
                    : $"\"{key}\" is not a {TranslationKey.Length}-digit key";
            }
            return $"\"{key}\" is not a {TranslationKey.Length}-digit key";
        }

        // A row identifies its string either way. key wins if both are present
        // and agree; if they disagree the row is wrong and is dropped loudly.
        private static string ResolveKey(Dictionary<string, string> row, out string source, out bool malformed)
        {
            source = null;
            malformed = false;

            row.TryGetValue("key", out string key);
            row.TryGetValue("source_en", out string text);
            key = key?.Trim();
            // A line-ID row keeps its ID as the key; any English beside it in a
            // working copy is only there for the translator to read.
            if (TranslationKey.LooksLikeLineId(key))
            {
                return key;
            }
            key = key?.ToLowerInvariant();
            bool hasKey = !string.IsNullOrEmpty(key);
            bool hasText = !string.IsNullOrEmpty(text);

            if (hasKey && !TranslationKey.LooksLikeKey(key))
            {
                // A translator appending "English,訳" to a published file puts
                // the English under the key column, since that is the header.
                // That is the most natural edit there is, so take it as source
                // text rather than rejecting it. Only something that is neither
                // a key nor plausibly text is malformed.
                if (!hasText)
                {
                    row.TryGetValue("key", out string rawKey);
                    source = rawKey;
                    return KeyFor(rawKey);
                }
                malformed = true;
                return null;
            }

            if (hasText)
            {
                string hashed = KeyFor(text);
                if (hasKey && hashed != key)
                {
                    malformed = true;
                    return null;
                }
                source = text;
                return hashed;
            }

            return hasKey ? key : null;
        }

        // Rewrite the locale file so every row is keyed by hash and carries no
        // English. This is what makes the file safe to publish. Rows already in
        // key form pass through; a key column is added ahead of translation.
        public static string HashFileInPlace(string pluginDirectory, string locale)
        {
            string localeDir = Path.Combine(pluginDirectory, "Translations", locale);
            string path = Path.Combine(localeDir, "strings.csv");
            // When a working copy exists it is the thing being edited, so the
            // published file is regenerated from it rather than from itself.
            string working = WorkingCopy.PathFor(pluginDirectory, locale);
            string input = File.Exists(working) ? working : path;
            if (!File.Exists(input))
            {
                return $"[hash] {locale}/strings.csv not found.";
            }

            // key -> (speaker, translation), first occurrence wins, input order kept.
            var rows = new Dictionary<string, KeyValuePair<string, string>>(StringComparer.Ordinal);
            var inputOrder = new List<string>();
            // line:xxxxxxxx -> translation. Published only when translated: a
            // working copy lists an empty line-ID row at every shared line.
            var lineRows = new Dictionary<string, string>(StringComparer.Ordinal);
            int converted = 0, kept = 0, dropped = 0, lineKept = 0, fromPublished = 0;
            var droppedExamples = new List<string>();
            // Keys whose English came from the working copy, to spot a working
            // copy written before a game update changed that English.
            var fromWorkingEnglish = new List<string>();
            foreach (var row in CsvReader.ReadRows(input))
            {
                string key = ResolveKey(row, out string source, out bool malformed);
                if (key == null)
                {
                    dropped++;
                    if (malformed && droppedExamples.Count < MaxMalformedExamples)
                    {
                        droppedExamples.Add(DescribeMalformed(row));
                    }
                    continue;
                }
                if (TranslationKey.LooksLikeLineId(key))
                {
                    row.TryGetValue("translation", out string lineTranslation);
                    if (!string.IsNullOrEmpty(lineTranslation) && !lineRows.ContainsKey(key))
                    {
                        lineRows[key] = lineTranslation;
                    }
                    continue;
                }
                if (rows.ContainsKey(key))
                {
                    continue;
                }
                // Nothing to publish for a line nobody has translated yet (the
                // check rejects empty translations, and tools/hash-strings.ps1
                // leaves them out too).
                row.TryGetValue("translation", out string rowTranslation);
                if (string.IsNullOrEmpty(rowTranslation))
                {
                    continue;
                }
                if (source != null)
                {
                    converted++;
                    fromWorkingEnglish.Add(key);
                }
                else
                {
                    kept++;
                }
                row.TryGetValue("translation", out string translation);
                // Who says the line is not the game's text, so it may ship.
                // Take it from the row, else work it out from the loaded script.
                row.TryGetValue("speaker", out string speaker);
                if (string.IsNullOrEmpty(speaker))
                {
                    speaker = source != null ? SpeakerLookup.For(source) : SpeakerLookup.ForKey(key);
                }
                rows[key] = new KeyValuePair<string, string>(speaker ?? string.Empty, translation ?? string.Empty);
                inputOrder.Add(key);
            }

            // A working copy only holds the rows it was written with. Rows the
            // published file gained since (a pack update, keys re-made after a
            // game update) would otherwise be lost, so keep every published row
            // the working copy does not have. The working copy wins where both do.
            var publishedKeys = new HashSet<string>(StringComparer.Ordinal);
            if (input == working && File.Exists(path))
            {
                foreach (var row in CsvReader.ReadRows(path))
                {
                    string key = ResolveKey(row, out _, out _);
                    if (key == null)
                    {
                        continue;
                    }
                    row.TryGetValue("translation", out string published);
                    if (string.IsNullOrEmpty(published))
                    {
                        continue;
                    }
                    if (TranslationKey.LooksLikeLineId(key))
                    {
                        if (!lineRows.ContainsKey(key))
                        {
                            lineRows[key] = published;
                        }
                        continue;
                    }
                    publishedKeys.Add(key);
                    if (rows.ContainsKey(key))
                    {
                        continue;
                    }
                    row.TryGetValue("speaker", out string speaker);
                    rows[key] = new KeyValuePair<string, string>(speaker ?? string.Empty, published);
                    inputOrder.Add(key);
                    fromPublished++;
                }
            }

            // The comment block under the header (language name, provisional
            // notice, credits) belongs to the published file; keep it.
            List<string> leadingComments = ReadLeadingComments(path);
            ScriptOrder.Data order = ScriptOrder.Load(pluginDirectory);
            int stale = 0;
            if (order != null && input == working)
            {
                var scriptKeys = new HashSet<string>(StringComparer.Ordinal);
                foreach (ScriptOrder.Entry e in order.Entries)
                {
                    scriptKeys.Add(e.Key);
                }
                foreach (string key in fromWorkingEnglish)
                {
                    if (!scriptKeys.Contains(key) && !publishedKeys.Contains(key))
                    {
                        stale++;
                    }
                }
            }
            using (var writer = new StreamWriter(path, append: false, new UTF8Encoding(false)))
            {
                writer.WriteLine("key,section,node,order,speaker,translation");
                foreach (string comment in leadingComments)
                {
                    writer.WriteLine(comment);
                }
                if (order == null)
                {
                    foreach (string key in inputOrder)
                    {
                        writer.WriteLine(key + ",,,," + CsvReader.Escape(rows[key].Key) + "," + CsvReader.Escape(rows[key].Value));
                    }
                }
                else
                {
                    ScriptOrder.WriteOrdered(writer, order, inputOrder, (key, e) =>
                    {
                        // The script order knows every character who says this
                        // English; a speaker typed into the pack is only a fallback.
                        string speaker = order.SpeakersFor(key);
                        if (string.IsNullOrEmpty(speaker)) speaker = string.IsNullOrEmpty(rows[key].Key) ? e.Speaker : rows[key].Key;
                        writer.WriteLine(key + "," + CsvReader.Escape(e.Section) + "," + CsvReader.Escape(e.Node) + "," + e.Order + "," + CsvReader.Escape(speaker) + "," + CsvReader.Escape(rows[key].Value));
                    },
                    e => lineRows.ContainsKey(e.LineId),
                    e =>
                    {
                        writer.WriteLine(e.LineId + "," + CsvReader.Escape(e.Section) + "," + CsvReader.Escape(e.Node) + "," + e.Order + "," + CsvReader.Escape(e.Speaker) + "," + CsvReader.Escape(lineRows[e.LineId]));
                        lineRows.Remove(e.LineId);
                        lineKept++;
                    },
                    out List<string> leftovers);
                    if (leftovers.Count > 0)
                    {
                        writer.WriteLine();
                        writer.WriteLine("# ===== UI and other text (not part of the dialogue script) =====");
                        foreach (string key in leftovers)
                        {
                            writer.WriteLine(key + ",UI,,," + CsvReader.Escape(string.IsNullOrEmpty(rows[key].Key) ? "UI" : rows[key].Key) + "," + CsvReader.Escape(rows[key].Value));
                        }
                    }
                }
                // Line-ID rows the order does not know, or all of them without order data.
                if (lineRows.Count > 0)
                {
                    writer.WriteLine();
                    writer.WriteLine("# ===== Per-line translations not found in the script order =====");
                    foreach (KeyValuePair<string, string> kv in lineRows)
                    {
                        writer.WriteLine(kv.Key + ",,,,," + CsvReader.Escape(kv.Value));
                        lineKept++;
                    }
                }
            }

            string from = input == working ? $" from the working copy ({fromPublished} row(s) kept from the published file)" : string.Empty;
            string ordered = order == null ? " No script order data found, so rows keep their input order." : $" Ordered by {Path.GetFileName(Path.GetDirectoryName(order.Source))}/script_order.csv.";
            string droppedDetail = droppedExamples.Count > 0 ? $" Dropped: {string.Join("; ", droppedExamples)}{(dropped > droppedExamples.Count ? "; ..." : "")}." : string.Empty;
            string staleDetail = stale > 0 ? $" {stale} row(s) of the working copy have English that is neither in the game's script nor in the published file; if the game was updated since the working copy was written, export it again." : string.Empty;
            return $"[hash] {locale}/strings.csv written{from}: {converted} row(s) converted from English, {kept} already hashed, {lineKept} per-line, {dropped} malformed dropped. No source text in the published file.{ordered}{droppedDetail}{staleDetail}";
        }

        // "# ..." lines directly under the header line, up to the first line
        // that is not a comment or is a section header ("# =====", "# ---").
        private static List<string> ReadLeadingComments(string path)
        {
            var result = new List<string>();
            if (!File.Exists(path))
            {
                return result;
            }
            bool first = true;
            foreach (string line in File.ReadLines(path, Encoding.UTF8))
            {
                if (first)
                {
                    first = false;
                    continue;
                }
                if (!line.StartsWith("#", StringComparison.Ordinal) ||
                    line.StartsWith("# =====", StringComparison.Ordinal) || line.StartsWith("# ---", StringComparison.Ordinal))
                {
                    break;
                }
                result.Add(line);
            }
            return result;
        }

        // The discovery file is appended to while playing, so without this it
        // accumulates a fresh copy of everything on every launch and keeps
        // listing strings that have since been translated. Rebuild it against
        // the locale we just loaded, and seed DiscoveredText from it so this
        // session only appends genuinely new strings.
        private static void RebuildDiscoveredFile()
        {
            string filePath = Path.Combine(_pluginDirectory, "Translations", "_discovered", "strings.csv");

            try
            {
                if (!File.Exists(filePath))
                {
                    return;
                }

                var kept = new List<string>();
                foreach (var row in CsvReader.ReadRows(filePath))
                {
                    if (!row.TryGetValue("source_en", out var source) || string.IsNullOrEmpty(source))
                    {
                        continue;
                    }
                    if (ByKey.ContainsKey(KeyFor(source)))
                    {
                        continue;
                    }
                    if (!DiscoveredText.TryAdd(source, 0))
                    {
                        continue;
                    }
                    // Drops values recorded before the ignore rules existed.
                    if (IgnoreRules.IsIgnored(source))
                    {
                        continue;
                    }

                    // Keep any draft the translator typed into this file directly.
                    row.TryGetValue("translation", out var draft);
                    kept.Add(CsvReader.Escape(source) + "," + CsvReader.Escape(draft ?? string.Empty));
                }

                using (var writer = new StreamWriter(filePath, append: false, Encoding.UTF8))
                {
                    writer.WriteLine("source_en,translation");
                    foreach (string line in kept)
                    {
                        writer.WriteLine(line);
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log($"[dump] Failed to rebuild discovered strings: {ex.Message}");
            }
        }

        public static bool TryGetTranslation(string source, out string translation)
        {
            return ByKey.TryGetValue(KeyFor(source), out translation);
        }

        public static bool TryGetLineTranslation(string lineId, out string translation)
        {
            translation = null;
            return lineId != null && ByLineId.TryGetValue(lineId, out translation);
        }

        // Used by the verbose debug log so repeated re-renders of the same line
        // (sliders, per-frame counters, replayed dialogue) don't spam the log.
        public static bool IsFirstApplication(string source)
        {
            return AppliedOnce.TryAdd(source, 0);
        }

        // Lets "Clear log" actually un-stick the verbose log: without this, text
        // already seen once this session would never be logged again even though
        // the visible log window is now empty.
        public static void ResetAppliedOnceTracking()
        {
            AppliedOnce.Clear();
        }

        // Queue discoveries in memory to avoid per-label file I/O during text
        // updates. Plugin.Update flushes them in batches. The observed Options
        // crash is in the native Direct3D12 renderer, not this CSV writer.
        public static void NoteDiscoveredText(string source)
        {
            if (Plugin.LogDiscoveredKeys == null || !Plugin.LogDiscoveredKeys.Value)
            {
                return;
            }

            if (!DiscoveredText.TryAdd(source, 0))
            {
                return;
            }

            // After the dedupe, so each distinct string is matched once.
            if (IgnoreRules.IsIgnored(source))
            {
                return;
            }

            lock (PendingDiscoveredLines)
            {
                PendingDiscoveredLines.Add(CsvReader.Escape(source) + ",");
            }
        }

        public static void FlushDiscoveredToDisk()
        {
            string[] lines;
            lock (PendingDiscoveredLines)
            {
                if (PendingDiscoveredLines.Count == 0)
                {
                    return;
                }
                lines = PendingDiscoveredLines.ToArray();
                PendingDiscoveredLines.Clear();
            }

            try
            {
                string dir = Path.Combine(_pluginDirectory, "Translations", "_discovered");
                Directory.CreateDirectory(dir);
                string filePath = Path.Combine(dir, "strings.csv");

                bool writeHeader = !File.Exists(filePath);
                using (var writer = new StreamWriter(filePath, append: true, Encoding.UTF8))
                {
                    if (writeHeader)
                    {
                        writer.WriteLine("source_en,translation");
                    }
                    foreach (string line in lines)
                    {
                        writer.WriteLine(line);
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log($"[dump] Failed to write discovered strings: {ex.Message}");
            }
        }
    }
}
