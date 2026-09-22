using System;
using System.Collections.Generic;
using System.IO;

namespace DragNWashLocalization
{
    // Loading a locale: the published strings.csv, other mods' packs
    // (experimental) and the working copy, and how a row's key is worked out
    // (a key, a line ID or the English).
    internal static partial class TranslationStore
    {
        public static void Load(string pluginDirectory, string locale)
        {
            _pluginDirectory = pluginDirectory;
            IgnoreRules.Load(pluginDirectory);
            ByKey.Clear();
            ByLineId.Clear();
            SourceByKey.Clear();
            ModOwner.Clear();
            ModTranslations.Clear();
            DiscoveredText.Clear();
            AppliedOnce.Clear();
            lock (PendingDiscoveredLines)
            {
                PendingDiscoveredLines.Clear();
            }
            SeenKeys.Clear();
            lock (PendingSeenLines)
            {
                PendingSeenLines.Clear();
            }
            foreach (KeyValuePair<string, string> seen in ReadSeenSources(pluginDirectory))
            {
                SeenKeys.TryAdd(seen.Key, 0);
            }

            if (!string.IsNullOrEmpty(locale))
            {
                // The published file first, then the translator's plain working
                // copy on top if there is one, so edits made there win.
                string localeDir = Path.Combine(pluginDirectory, "Translations", locale);
                LoadFile(Path.Combine(localeDir, "strings.csv"), locale + "/strings.csv");
                // Other mods' packs next: they add lines, never replace these.
                if (ModTranslations.Enabled)
                {
                    foreach (ModTranslations.Pack pack in ModTranslations.Find(pluginDirectory, locale))
                    {
                        LoadModPack(pack);
                    }
                }
                LoadFile(WorkingCopy.PathFor(pluginDirectory, locale), "_discovered/" + WorkingCopy.FileNameFor(locale));
            }

            RebuildDiscoveredFile();
            LineResolution.Rebuild(pluginDirectory);
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
        private const int MaxConflictsLogged = 5;
        private const string OwnName = "Drag'n Wash Localization";

        // Another mod's pack (experimental). Its rows fill only lines nothing
        // loaded before it translates; a row that disagrees with one already
        // loaded (this mod's pack, or a mod earlier in GUID order) is left out
        // and noted as a conflict. A file too large, or one that cannot be read,
        // is skipped whole, and nothing else is affected.
        private static void LoadModPack(ModTranslations.Pack pack)
        {
            string label = pack.Name + ": Translations/" + Path.GetFileName(Path.GetDirectoryName(pack.Path)) + "/strings.csv";
            try
            {
                long size = new FileInfo(pack.Path).Length;
                if (size > ModTranslations.MaxFileBytes)
                {
                    pack.Problem = $"larger than {ModTranslations.MaxFileBytes / (1024 * 1024)} MB, not read";
                    Plugin.Log($"[mods] {label} is {size / 1024} KB, larger than {ModTranslations.MaxFileBytes / (1024 * 1024)} MB; not read.");
                    ModTranslations.NoteLoaded(pack);
                    return;
                }
                int badKeys = 0, logged = 0;
                foreach (var row in CsvReader.ReadRows(pack.Path))
                {
                    if (!row.TryGetValue("translation", out var translation) || string.IsNullOrEmpty(translation))
                    {
                        continue;
                    }
                    string key = ResolveKey(row, out string source, out bool malformed);
                    if (key == null)
                    {
                        if (malformed) badKeys++;
                        continue;
                    }
                    if (pack.Rows >= ModTranslations.MaxRowsPerMod)
                    {
                        pack.Problem = $"only the first {ModTranslations.MaxRowsPerMod} rows read";
                        Plugin.Log($"[mods] {label}: only the first {ModTranslations.MaxRowsPerMod} rows are read.");
                        break;
                    }
                    Dictionary<string, string> table = TranslationKey.LooksLikeLineId(key) ? ByLineId : ByKey;
                    if (table.TryGetValue(key, out string existing))
                    {
                        if (existing != translation)
                        {
                            string other = ModOwner.TryGetValue(key, out string owner) ? owner : OwnName;
                            ModTranslations.NoteConflict(pack, key, other, existing, translation);
                            if (logged++ < MaxConflictsLogged)
                            {
                                Plugin.Log($"[mods] Conflict: {pack.Name} translates \"{DescribeKey(key)}\" as \"{translation}\", {other} as \"{existing}\"; {other}'s is kept.");
                            }
                        }
                        continue;
                    }
                    table[key] = translation;
                    ModOwner[key] = pack.Name;
                    if (source != null && table == ByKey)
                    {
                        SourceByKey[key] = source;
                    }
                    pack.Rows++;
                }
                if (badKeys > 0)
                {
                    Plugin.Log($"[mods] {badKeys} row(s) in {label} were skipped: not a key, a line ID or English.");
                }
                Plugin.Log($"[mods] {label}: {pack.Rows} line(s){(pack.Conflicts > 0 ? $", {pack.Conflicts} conflict(s) left out" : "")}.");
            }
            catch (Exception ex)
            {
                pack.Problem = "could not be read: " + ex.Message;
                Plugin.Log($"[mods] Could not read {label}: {ex.Message}. That file is skipped; nothing else is affected.");
            }
            ModTranslations.NoteLoaded(pack);
        }

        // The mod whose pack supplied a key's translation, or null for this
        // mod's own pack and the working copy.
        public static string ModFor(string key) => key != null && ModOwner.TryGetValue(key, out string mod) ? mod : null;

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
    }
}
