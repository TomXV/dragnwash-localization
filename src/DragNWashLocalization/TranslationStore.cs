using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using DragNWash.ModFramework.Dialogue;

namespace DragNWashLocalization
{
    // Maps the text TMP_Text.text is set to, straight to a translation. There
    // is no table or key concept for translators to learn: a row is the exact
    // on-screen English string and its translation.
    //
    // The published file does not contain that English, though. Rows are keyed
    // by LineKey.Hash of the source, so the repository redistributes none of
    // the script. Both layouts are accepted in the same file:
    //
    //   key,translation           what the repository ships
    //   source_en,translation     what a translator writes locally, against
    //                             the exports the plugin produces for them
    //
    // A translator works in the second form (hot reload picks it up), and runs
    // "Hash strings.csv for commit" or tools/hash-strings.ps1 before opening a
    // pull request. The plugin hashes each string it is asked to translate and
    // looks the hash up, so a plain row and its hashed form behave identically.
    //
    // Split by responsibility into TranslationStore.<Feature>.cs beside this
    // file (Fonts, Loading, Hashing, Discovered). This file keeps the tables,
    // the key helpers (DescribeKey, KeyFor) and the lookups the text patches
    // call on every assignment.
    internal static partial class TranslationStore
    {
        private static readonly Dictionary<string, string> ByKey = new Dictionary<string, string>(StringComparer.Ordinal);
        // line:xxxxxxxx -> translation. Wins over ByKey for that one line of
        // dialogue; see LineIdContext.
        private static readonly Dictionary<string, string> ByLineId = new Dictionary<string, string>(StringComparer.Ordinal);
        // Source text for keys that arrived as source_en rows. Only for
        // logging and exports; lookups never need it.
        private static readonly Dictionary<string, string> SourceByKey = new Dictionary<string, string>(StringComparer.Ordinal);
        // Keys a pack of another mod added (experimental, ModTranslations), and
        // which mod, to name both sides of a conflict.
        private static readonly Dictionary<string, string> ModOwner = new Dictionary<string, string>(StringComparer.Ordinal);
        // TryGetTranslation runs on every text assignment, so hash once per
        // distinct string rather than once per call.
        private static readonly ConcurrentDictionary<string, string> HashCache = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
        private static readonly ConcurrentDictionary<string, byte> DiscoveredText = new ConcurrentDictionary<string, byte>();
        private static readonly ConcurrentDictionary<string, byte> AppliedOnce = new ConcurrentDictionary<string, byte>();
        private static readonly List<string> PendingDiscoveredLines = new List<string>();
        // Keys whose English has been shown at least once, in any session, and
        // the rows still to be written to _discovered/seen_sources.csv. The
        // working copy fills its source_en column from that file for text that
        // is not on screen when it is exported (the Mods screen, rare menus).
        private static readonly ConcurrentDictionary<string, byte> SeenKeys = new ConcurrentDictionary<string, byte>();
        private static readonly List<string> PendingSeenLines = new List<string>();

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
            return HashCache.GetOrAdd(source, LineKey.Hash);
        }

        public static bool TryGetTranslation(string source, out string translation)
        {
            return ByKey.TryGetValue(KeyFor(source), out translation);
        }

        public static bool TryGetKeyTranslation(string key, out string translation)
        {
            translation = null;
            return key != null && ByKey.TryGetValue(key, out translation);
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
    }
}
