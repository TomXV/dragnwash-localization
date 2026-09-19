using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DragNWashLocalization
{
    // Reload the active locale's translation files while the game is running,
    // so a translator can edit a line, alt-tab back, and see it on screen
    // without restarting. The locale-switch path already does everything
    // needed - re-read the files, re-apply to every TMP_Text we have seen - so
    // this only has to notice a file changed and call it from the main thread.
    //
    // The files watched: the published strings.csv, if present the plain
    // _discovered/<locale>.working.csv that translators actually edit, and the
    // packs of other mods read for the language (experimental).
    //
    // Polling rather than FileSystemWatcher: the watcher fires on a thread pool
    // thread, fires several times per save, and fires while the editor still
    // holds the file open. Checking the write time once a second from Update
    // and waiting for it to sit still for a couple of polls avoids all three.
    internal static class HotReload
    {
        private const float PollInterval = 1f;
        // Editors write in stages; require the timestamp to stop moving.
        private const int StablePollsRequired = 2;
        // A bulk paste can change hundreds of lines; keep the log readable.
        private const int MaxDiffLines = 20;

        private sealed class Watched
        {
            public string Path;
            public DateTime LastSeen = DateTime.MinValue;
            public DateTime Candidate = DateTime.MinValue;
            public int StablePolls;
        }

        private static float _nextPoll;
        private static readonly List<Watched> Files = new List<Watched>();

        public static void Track(string pluginDirectory, string locale)
        {
            Files.Clear();
            foreach (string path in new[]
            {
                Path.Combine(pluginDirectory, "Translations", locale, "strings.csv"),
                WorkingCopy.PathFor(pluginDirectory, locale),
            })
            {
                Files.Add(new Watched { Path = path, LastSeen = SafeWriteTime(path) });
            }
            // Other mods' packs read for this language (experimental).
            foreach (ModTranslations.Pack pack in ModTranslations.Packs)
            {
                Files.Add(new Watched { Path = pack.Path, LastSeen = SafeWriteTime(pack.Path) });
            }
        }

        // Main thread only.
        public static void Tick(string pluginDirectory, string locale)
        {
            if (Time.unscaledTime < _nextPoll)
            {
                return;
            }
            _nextPoll = Time.unscaledTime + PollInterval;

            if (Files.Count == 0)
            {
                Track(pluginDirectory, locale);
                return;
            }

            bool changed = false;
            foreach (Watched w in Files)
            {
                DateTime now = SafeWriteTime(w.Path);
                if (now == w.LastSeen)
                {
                    w.Candidate = DateTime.MinValue;
                    w.StablePolls = 0;
                    continue;
                }
                if (now != w.Candidate)
                {
                    w.Candidate = now;
                    w.StablePolls = 0;
                    continue;
                }
                if (++w.StablePolls < StablePollsRequired)
                {
                    continue;
                }
                w.LastSeen = now;
                w.Candidate = DateTime.MinValue;
                w.StablePolls = 0;
                changed = true;
            }

            if (changed)
            {
                Reload(pluginDirectory, locale);
            }
        }

        private static void Reload(string pluginDirectory, string locale)
        {
            try
            {
                // Snapshot so the log can say what actually changed, which is
                // what a translator iterating on a line wants to see.
                var before = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (KeyValuePair<string, string> kv in TranslationStore.Entries)
                {
                    before[kv.Key] = kv.Value;
                }

                TranslationStore.Load(pluginDirectory, locale);
                // Any character the edit introduced would otherwise be
                // rasterized on first render - a runtime atlas upload, the
                // Direct3D 12 crash path. Only the new ones get added here.
                DragNWash.ModFramework.Assets.GameFonts.Prepare(locale, TranslationStore.TranslatedTexts);
                TmpTextHook.RefreshAll();

                // Logged after RefreshAll so the diff sits at the bottom of
                // the log rather than above the re-application noise.
                int changed = 0, added = 0, removed = 0, shown = 0;
                foreach (KeyValuePair<string, string> kv in TranslationStore.Entries)
                {
                    // Keys are hashes; show the English when a file had it.
                    string label = TranslationStore.DescribeKey(kv.Key);
                    if (!before.TryGetValue(kv.Key, out string old))
                    {
                        added++;
                        if (shown++ < MaxDiffLines) Plugin.Log($"[reload] + \"{label}\" -> \"{kv.Value}\"");
                    }
                    else if (old != kv.Value)
                    {
                        changed++;
                        if (shown++ < MaxDiffLines) Plugin.Log($"[reload] ~ \"{label}\": \"{old}\" -> \"{kv.Value}\"");
                    }
                    before.Remove(kv.Key);
                }
                foreach (KeyValuePair<string, string> kv in before)
                {
                    removed++;
                    if (shown++ < MaxDiffLines) Plugin.Log($"[reload] - \"#{kv.Key}\" (was \"{kv.Value}\")");
                }
                int total = changed + added + removed;
                if (total > MaxDiffLines) Plugin.Log($"[reload] ... and {total - MaxDiffLines} more");
                Plugin.Log($"[reload] {locale}: {changed} changed, {added} added, {removed} removed ({TranslationStore.EntryCount} entries). Applied to text on screen.");
            }
            catch (IOException ex)
            {
                // Still being written; the next poll will see a newer time.
                foreach (Watched w in Files) w.LastSeen = DateTime.MinValue;
                Plugin.Log($"[reload] File busy, will retry: {ex.Message}");
            }
            catch (Exception ex)
            {
                Plugin.Log($"[reload] Failed to reload translations: {ex.Message}");
            }
        }

        private static DateTime SafeWriteTime(string path)
        {
            try
            {
                return File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }
    }
}
