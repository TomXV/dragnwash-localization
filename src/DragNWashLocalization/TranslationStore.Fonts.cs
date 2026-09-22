using System;
using System.Collections.Generic;
using System.IO;

namespace DragNWashLocalization
{
    // The texts that can be put on screen, for FontFallback to rasterize up
    // front: every installed locale's (all at once or per locale) and the
    // loaded locale's.
    internal static partial class TranslationStore
    {
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
                if (name.StartsWith("_", StringComparison.Ordinal))
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
                if (name.StartsWith("_", StringComparison.Ordinal)) continue;
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
            // Other mods' packs (experimental), for the languages this mod has.
            if (ModTranslations.Enabled)
            {
                foreach (KeyValuePair<string, string> pack in ModTranslations.AllLanguagePaths(pluginDirectory))
                {
                    if (!result.TryGetValue(pack.Key, out List<string> texts)) continue;
                    try
                    {
                        foreach (var row in CsvReader.ReadRows(pack.Value))
                        {
                            if (row.TryGetValue("translation", out var translation) && !string.IsNullOrEmpty(translation))
                            {
                                texts.Add(translation);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log($"[font] Could not read {pack.Value} for font preparation: {ex.Message}");
                    }
                }
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
    }
}
