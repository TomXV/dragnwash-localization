using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;

namespace DragNWashLocalization
{
    // DialogueDumper reads Yarn's compiled project, so one press yields every
    // line in the game no matter how far the player has got. Static UI text has
    // no such table: it lives on individual TMP_Text components, and the hook
    // only meets one when something assigns its text or the component is
    // enabled. Translating the whole UI would otherwise mean opening every
    // screen in the game and hoping none was missed.
    //
    // FindObjectsOfTypeAll walks loaded objects including inactive ones, so a
    // single press collects every label in the scenes currently loaded - the
    // pause menu without opening it, a dialog without triggering it.
    internal static class UiTextDumper
    {
        private const int MaxPathDepth = 12;

        public static void DumpAll(string pluginDirectory)
        {
            try
            {
                var seen = new HashSet<string>(StringComparer.Ordinal);
                var rows = new List<string>();
                int ignored = 0;
                int translated = 0;

                foreach (TMP_Text component in Resources.FindObjectsOfTypeAll<TMP_Text>())
                {
                    if (component == null)
                    {
                        continue;
                    }

                    string source;
                    try
                    {
                        // A component we already translated reports the
                        // translation; the tracked value is the English it had.
                        if (!TmpTextHook.TryGetTrackedSource(component, out source))
                        {
                            source = component.text;
                        }
                    }
                    catch
                    {
                        continue;
                    }

                    // Trimming would change the key: the version label's
                    // text really ends in a newline, and the trimmed form
                    // matches no entry, so the row would look untranslated
                    // no matter how many times someone translated it.
                    if (string.IsNullOrEmpty(source) || source.Trim().Length == 0)
                    {
                        continue;
                    }

                    // Values the game computes for itself - slider readouts,
                    // resolutions, timers. See IgnoreRules.
                    if (IgnoreRules.IsIgnored(source))
                    {
                        ignored++;
                        continue;
                    }

                    if (!seen.Add(source))
                    {
                        continue;
                    }

                    bool hasTranslation = TranslationStore.TryGetTranslation(source, out var existing);
                    if (hasTranslation)
                    {
                        translated++;
                    }

                    rows.Add(string.Concat(
                        TranslationStore.KeyFor(source), ",",
                        CsvReader.Escape(source), ",",
                        CsvReader.Escape(existing ?? string.Empty), ",",
                        CsvReader.Escape(DescribePath(component)), ",",
                        CsvReader.Escape(ModTextOwners.For(source, component) ?? string.Empty)));
                }

                if (rows.Count == 0)
                {
                    Plugin.Log("[ui] No UI text found.");
                    return;
                }

                rows.Sort(StringComparer.Ordinal);

                string dir = Path.Combine(pluginDirectory, "Translations", "_discovered");
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, "ui_texts.csv");

                using (var writer = new StreamWriter(path, append: false, new UTF8Encoding(false)))
                {
                    // mod: the mod that showed the text, when known; empty for the game.
                    writer.WriteLine("key,source_en,translation,object_path,mod");
                    foreach (string row in rows)
                    {
                        writer.WriteLine(row);
                    }
                }

                Plugin.Log($"[ui] {rows.Count} UI string(s) ({translated} already translated, {ignored} ignored) -> {path}");
            }
            catch (Exception ex)
            {
                Plugin.Log($"[ui] Failed to export UI text: {ex.Message}");
            }
        }

        // Where the label sits, so a translator can tell "Back" on the options
        // screen from "Back" somewhere else without hunting for it in game.
        private static string DescribePath(TMP_Text component)
        {
            try
            {
                var parts = new List<string>();
                Transform current = component.transform;
                for (int depth = 0; current != null && depth < MaxPathDepth; depth++)
                {
                    parts.Add(current.name);
                    current = current.parent;
                }
                parts.Reverse();
                return string.Join("/", parts.ToArray());
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
