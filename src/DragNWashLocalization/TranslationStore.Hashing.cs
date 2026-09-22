using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DragNWashLocalization
{
    // "Hash strings.csv for commit": rewrites the locale's published file keyed
    // by hash, with no English, in script order.
    internal static partial class TranslationStore
    {
        // Rewrite the locale file so every row is keyed by hash and carries no
        // English. This is what makes the file safe to publish. Rows already in
        // key form pass through; a key column is added ahead of translation.
        public static string HashFileInPlace(string pluginDirectory, string locale)
        {
            // Rows without a speaker column fall back to SpeakerLookup, which
            // scans every loaded object to build its table. Reset it once here
            // so the scan happens at most once for the whole file rather than
            // once per row.
            SpeakerLookup.Reset();
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
            // Written through SafeFile so a failure partway through leaves the
            // previous file intact rather than a truncated one.
            SafeFile.Write(path, new UTF8Encoding(false), writer =>
            {
                // The published file is committed, and the repository keeps
                // its CSVs in LF. WriteLine would use Environment.NewLine,
                // so hashing on Windows would rewrite every line.
                writer.NewLine = "\n";
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
            });

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
    }
}
