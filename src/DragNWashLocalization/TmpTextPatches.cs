using System;
using DragNWash.ModFramework.Text;
using TMPro;

namespace DragNWashLocalization
{
    // Translation of every TextMeshPro text, as a rewriter on Drag'n Wash
    // ModFramework's text library. The library owns the hooks on TMP_Text (the
    // text setter, both SetText overloads and prefab text on OnEnable), remembers
    // the text the game set on each component, and re-runs rewriters from that
    // source on RefreshAll, so a locale switch re-translates text on screen.
    internal static class TmpTextHook
    {
        private static IDisposable _rewriter;

        internal static void Install()
        {
            _rewriter = GameText.AddRewriter(Plugin.PluginGuid, Rewrite);
            Plugin.Log(GameText.IsAvailable
                ? "[text] Translating through Drag'n Wash ModFramework's text library."
                : "[text] The text library is unavailable on this game build; nothing will be translated.");
        }

        private static void Rewrite(TextContext context)
        {
            string source = context.Source;
            TMP_Text instance = context.Component;
            if (string.IsNullOrEmpty(source))
            {
                return;
            }

            try
            {
                if (!context.IsRefresh)
                {
                    ModTextOwners.Note(source);
                }
                // A row for this exact line of dialogue wins over the hash row
                // shared by every line with the same English.
                if (instance != null && LineIdContext.TryGetTranslation(instance, source, out string perLine, out string lineId))
                {
                    RightToLeft.Apply(instance, perLine);
                    context.Text = perLine;
                    TranslationStore.NoteSeenSource(source);
                    if (Plugin.VerboseTextLog != null && Plugin.VerboseTextLog.Value &&
                        TranslationStore.IsFirstApplication(lineId + "|" + source))
                    {
                        Plugin.Log($"[OK] {lineId} \"{source}\" -> \"{perLine}\"");
                    }
                    return;
                }

                bool translated = TranslationStore.TryGetTranslation(source, out string translation);
                bool ignored = IgnoreRules.IsIgnored(source);

                // Every text is checked, not only translated ones: a component that
                // showed Hebrew may be reused for a name or a number.
                RightToLeft.Apply(instance, translated ? translation : source);

                if (translated)
                {
                    context.Text = translation;
                    TranslationStore.NoteSeenSource(source);
                }
                else if (!context.IsRefresh)
                {
                    // Queues in memory; Plugin.Update writes the discovery CSV.
                    TranslationStore.NoteDiscoveredText(source);
                }

                // Slider values, resolutions and the like would otherwise bury the
                // lines a translator is looking for. An explicit entry in
                // strings.csv still wins, hence the `translated` check first.
                if (Plugin.VerboseTextLog != null && Plugin.VerboseTextLog.Value &&
                    TranslationStore.IsFirstApplication(source) &&
                    (translated || !ignored))
                {
                    Plugin.Log(translated
                        ? $"[OK] \"{source}\" -> \"{translation}\""
                        : $"[--] \"{source}\"");
                }
            }
            catch (Exception ex)
            {
                // A localization failure must not prevent TMP from setting text.
                Plugin.Log($"[text] Failed to localize text: {ex.Message}");
            }
        }

        // A component we have already translated reports its translation from
        // .text, not the English it started as. The dumpers need the source.
        internal static bool TryGetTrackedSource(TMP_Text instance, out string source)
        {
            return GameText.TryGetSource(instance, out source) && !IgnoreRules.IsIgnored(source);
        }

        // Re-apply the currently loaded locale to every text on screen.
        // Runs on the main thread (Plugin.Update); never from a render callback.
        internal static void RefreshAll()
        {
            GameText.RefreshAll();
        }
    }
}
