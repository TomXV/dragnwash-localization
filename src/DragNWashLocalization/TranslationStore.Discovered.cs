using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DragNWashLocalization
{
    // The _discovered files, for a translator with the developer tools on:
    // strings.csv (text shown with no translation) and seen_sources.csv (the
    // English of text shown with one). Queued while playing, written in
    // batches by FlushDiscoveredToDisk.
    internal static partial class TranslationStore
    {
        // The discovery file is appended to while playing, so without this it
        // accumulates a fresh copy of everything on every launch and keeps
        // listing strings that have since been translated. Rebuild it against
        // the locale we just loaded, and seed DiscoveredText from it so this
        // session only appends genuinely new strings.
        private static void RebuildDiscoveredFile()
        {
            // The discovered files carry the game's text in plain English; only
            // a translator with the developer tools on gets them.
            if (!DragNWash.ModFramework.DeveloperTools.Enabled)
            {
                return;
            }
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
                    row.TryGetValue("mod", out var mod);
                    kept.Add(CsvReader.Escape(source) + "," + CsvReader.Escape(draft ?? string.Empty) + "," + CsvReader.Escape(mod ?? string.Empty));
                }

                using (var writer = new StreamWriter(filePath, append: false, new UTF8Encoding(false)))
                {
                    // mod: the mod that showed the text; empty for the game.
                    writer.WriteLine("source_en,translation,mod");
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
                PendingDiscoveredLines.Add(CsvReader.Escape(source) + ",," + CsvReader.Escape(ModTextOwners.For(source) ?? string.Empty));
            }
        }

        public static string SeenSourcesPath(string pluginDirectory)
        {
            return Path.Combine(pluginDirectory, "Translations", "_discovered", "seen_sources.csv");
        }

        // key -> English for every text shown with a translation in an earlier
        // or this session (developer tools on). Local only, like the other
        // _discovered files.
        public static List<KeyValuePair<string, string>> ReadSeenSources(string pluginDirectory)
        {
            var list = new List<KeyValuePair<string, string>>();
            string path = SeenSourcesPath(pluginDirectory);
            try
            {
                if (File.Exists(path))
                {
                    foreach (var row in CsvReader.ReadRows(path))
                    {
                        if (row.TryGetValue("key", out string key) && row.TryGetValue("source_en", out string source) &&
                            !string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(source))
                        {
                            list.Add(new KeyValuePair<string, string>(key, source));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log($"[dump] Failed to read seen sources: {ex.Message}");
            }
            return list;
        }

        // A translated text was shown: remember its English once, so a later
        // working copy can name it even when it is not on screen.
        public static void NoteSeenSource(string source)
        {
            if (string.IsNullOrEmpty(source) || !DragNWash.ModFramework.DeveloperTools.Enabled)
            {
                return;
            }
            string key = KeyFor(source);
            if (!SeenKeys.TryAdd(key, 0) || IgnoreRules.IsIgnored(source))
            {
                return;
            }
            lock (PendingSeenLines)
            {
                PendingSeenLines.Add(CsvReader.Escape(key) + "," + CsvReader.Escape(source));
            }
        }

        private static void FlushSeenToDisk()
        {
            string[] lines;
            lock (PendingSeenLines)
            {
                if (PendingSeenLines.Count == 0)
                {
                    return;
                }
                lines = PendingSeenLines.ToArray();
                PendingSeenLines.Clear();
            }
            try
            {
                string filePath = SeenSourcesPath(_pluginDirectory);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                bool writeHeader = !File.Exists(filePath);
                using (var writer = new StreamWriter(filePath, append: true, new UTF8Encoding(false)))
                {
                    if (writeHeader)
                    {
                        writer.WriteLine("key,source_en");
                    }
                    foreach (string line in lines)
                    {
                        writer.WriteLine(line);
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log($"[dump] Failed to write seen sources: {ex.Message}");
            }
        }

        public static void FlushDiscoveredToDisk()
        {
            if (!DragNWash.ModFramework.DeveloperTools.Enabled)
            {
                return;
            }
            FlushSeenToDisk();
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
                using (var writer = new StreamWriter(filePath, append: true, new UTF8Encoding(false)))
                {
                    if (writeHeader)
                    {
                        writer.WriteLine("source_en,translation,mod");
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
