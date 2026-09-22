using System;

namespace DragNWashLocalization
{
    // The published translation files carry no English. Each row is keyed by a
    // hash of the exact source string instead, so the repository does not
    // redistribute the game's script and only someone who can see the text on
    // screen - which is to say, someone who owns the game - can translate it.
    //
    // The key is the first 16 hex digits of SHA-256 over the UTF-8 bytes of the
    // string, exactly as TMP received it (no trimming, tags included). That is
    // exactly DragNWash.ModFramework.Dialogue.LineKey.Hash, which this mod calls
    // rather than keeping a second copy; tools/hash-strings.ps1 computes the
    // same on the offline side.
    internal static class TranslationKey
    {
        public const int Length = 16;

        public const string LinePrefix = "line:";

        // A Yarn line ID such as line:6046bedf. A row keyed this way translates
        // one line of dialogue only, where the hash row translates every line
        // with the same English (see LineIdContext).
        public static bool LooksLikeLineId(string value)
        {
            if (value == null || value.Length <= LinePrefix.Length || value.Length > 64 ||
                !value.StartsWith(LinePrefix, StringComparison.Ordinal))
            {
                return false;
            }
            for (int i = LinePrefix.Length; i < value.Length; i++)
            {
                char c = value[i];
                bool ok = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_' || c == '-' || c == '.';
                if (!ok)
                {
                    return false;
                }
            }
            return true;
        }

        // A key column value: 16 lowercase hex digits. Anything else is
        // treated as not-a-key so a mistyped row is reported, not silently
        // matched against nothing.
        public static bool LooksLikeKey(string value)
        {
            if (value == null || value.Length != Length)
            {
                return false;
            }
            foreach (char c in value)
            {
                bool hex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f');
                if (!hex)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
