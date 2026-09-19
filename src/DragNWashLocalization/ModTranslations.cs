using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Bootstrap;

namespace DragNWashLocalization
{
    // Experimental, off by default (issue #28, docs/MOD_TRANSLATIONS.md): other
    // mods ship the translations of their own text as
    // <their plugin folder>/Translations/<locale>/strings.csv, in the same form
    // as this mod's packs, and this mod reads them after its own. A mod's rows
    // never replace the game's (this mod's pack); a row that disagrees is kept
    // out and listed as a conflict.
    //
    // Only the folders of plugins BepInEx loaded are read, never an arbitrary
    // folder, and only for a language this mod has: the language list, names
    // and fonts are this mod's.
    internal static class ModTranslations
    {
        // A pack larger than this is not read at all.
        internal const long MaxFileBytes = 5L * 1024 * 1024;
        // Rows read from one mod at most.
        internal const int MaxRowsPerMod = 20000;

        internal sealed class Pack
        {
            public string Guid;
            public string Name;
            public string Folder;
            public string Path;
            public int Rows;
            public int Conflicts;
            public string Problem;
        }

        internal sealed class Conflict
        {
            public string Key;
            public string Mod;
            public string Other;
            public string Kept;
            public string Dropped;
        }

        private static readonly List<Pack> LoadedPacks = new List<Pack>();
        private static readonly List<Conflict> AllConflicts = new List<Conflict>();
        private static bool _warned;
        private static readonly HashSet<string> SaidUnknown = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        internal const int MaxConflictsKept = 500;

        internal static bool Enabled => Plugin.TranslationsFromOtherMods != null && Plugin.TranslationsFromOtherMods.Value;

        internal static IList<Pack> Packs => LoadedPacks;

        internal static IList<Conflict> Conflicts => AllConflicts;

        internal static int ConflictCount { get; private set; }

        internal static void Clear()
        {
            LoadedPacks.Clear();
            AllConflicts.Clear();
            ConflictCount = 0;
        }

        internal static void NoteConflict(Pack pack, string key, string other, string kept, string dropped)
        {
            pack.Conflicts++;
            ConflictCount++;
            if (AllConflicts.Count < MaxConflictsKept)
            {
                AllConflicts.Add(new Conflict { Key = key, Mod = pack.Name, Other = other, Kept = kept, Dropped = dropped });
            }
        }

        internal static void NoteLoaded(Pack pack)
        {
            LoadedPacks.Add(pack);
        }

        // The loaded plugins' folders other than this mod's, one entry per
        // folder, in GUID order so the first to claim a line is the same every
        // time.
        internal static List<Pack> Find(string pluginDirectory, string locale)
        {
            var found = new List<Pack>();
            if (string.IsNullOrEmpty(locale))
            {
                return found;
            }
            string own = Normalize(pluginDirectory);
            string pluginsRoot = Normalize(Paths.PluginPath);
            var folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var infos = new List<PluginInfo>(Chainloader.PluginInfos.Values);
            infos.Sort((a, b) => string.CompareOrdinal(a.Metadata.GUID, b.Metadata.GUID));
            foreach (PluginInfo info in infos)
            {
                string location = info.Location;
                if (string.IsNullOrEmpty(location)) continue;
                string folder = Normalize(Path.GetDirectoryName(location));
                // A DLL straight in plugins/ has no folder of its own.
                if (folder == null || folder == own || folder == pluginsRoot || !folders.Add(folder)) continue;
                string translations = Path.Combine(folder, "Translations");
                if (!Directory.Exists(translations)) continue;
                SayUnknownLanguages(info.Metadata.Name, translations, pluginDirectory);
                string path = Path.Combine(translations, locale, "strings.csv");
                if (!File.Exists(path)) continue;
                found.Add(new Pack { Guid = info.Metadata.GUID, Name = info.Metadata.Name, Folder = folder, Path = path });
            }
            if (!_warned)
            {
                _warned = true;
                Plugin.Log("[mods] Experimental: translations from other mods are on. How they work with each mod is untested; turn the setting off if text looks wrong.");
            }
            return found;
        }

        // Every other mod's pack for every language, for fonts prepared at
        // startup (Direct3D 12).
        internal static IEnumerable<KeyValuePair<string, string>> AllLanguagePaths(string pluginDirectory)
        {
            string own = Normalize(pluginDirectory);
            string pluginsRoot = Normalize(Paths.PluginPath);
            var folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (PluginInfo info in Chainloader.PluginInfos.Values)
            {
                if (string.IsNullOrEmpty(info.Location)) continue;
                string folder = Normalize(Path.GetDirectoryName(info.Location));
                if (folder == null || folder == own || folder == pluginsRoot || !folders.Add(folder)) continue;
                string translations = Path.Combine(folder, "Translations");
                if (!Directory.Exists(translations)) continue;
                foreach (string dir in Directory.GetDirectories(translations))
                {
                    string locale = Path.GetFileName(dir);
                    string path = Path.Combine(dir, "strings.csv");
                    if (!locale.StartsWith("_", StringComparison.Ordinal) && File.Exists(path) && new FileInfo(path).Length <= MaxFileBytes)
                    {
                        yield return new KeyValuePair<string, string>(locale, path);
                    }
                }
            }
        }

        // Folders for languages this mod does not have are skipped, once each.
        private static void SayUnknownLanguages(string mod, string translations, string pluginDirectory)
        {
            try
            {
                foreach (string dir in Directory.GetDirectories(translations))
                {
                    string locale = Path.GetFileName(dir);
                    if (locale.StartsWith("_", StringComparison.Ordinal)) continue;
                    if (Directory.Exists(Path.Combine(pluginDirectory, "Translations", locale))) continue;
                    if (SaidUnknown.Add(mod + "|" + locale))
                    {
                        Plugin.Log($"[mods] {mod}: Translations/{locale} is skipped; Drag'n Wash Localization has no {locale} pack. Add the language here first.");
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log($"[mods] {mod}: could not list its Translations folder: {ex.Message}");
            }
        }

        private static string Normalize(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            try
            {
                return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch
            {
                return null;
            }
        }
    }
}
