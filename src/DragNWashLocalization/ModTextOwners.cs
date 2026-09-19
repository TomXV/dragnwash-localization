using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using UnityEngine;

namespace DragNWashLocalization
{
    // Which mod put a text on screen, for the translator's exports (the mod
    // column of _discovered/strings.csv and ui_texts.csv). Only with the
    // developer tools on, and once per distinct text: the first time a text is
    // met, the calling stack is walked for the first plugin assembly that is
    // neither this mod nor the framework (the same way as the framework's
    // network watch). Text a prefab carries has no mod on the stack; for the
    // F7 export the components on the label and its parents are asked
    // instead. Empty means the game, or not known.
    internal static class ModTextOwners
    {
        private static readonly ConcurrentDictionary<string, string> BySource = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
        private static readonly ConcurrentDictionary<string, byte> Checked = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);
        private static Dictionary<Assembly, string> _plugins;
        private static int _pluginCount = -1;
        private const int MaxParents = 12;

        // Called by the text hook for each text it meets; walks the stack once
        // per distinct text.
        internal static void Note(string source)
        {
            if (!DragNWash.ModFramework.DeveloperTools.Enabled || string.IsNullOrEmpty(source) || !Checked.TryAdd(source, 0))
            {
                return;
            }
            try
            {
                string mod = FromStack();
                if (mod != null)
                {
                    BySource[source] = mod;
                }
            }
            catch
            {
                // Only a label for an export; never in the way of the text.
            }
        }

        internal static string For(string source)
        {
            return source != null && BySource.TryGetValue(source, out string mod) ? mod : null;
        }

        // The text's own record first, then the components on the label and
        // its parents.
        internal static string For(string source, Component component)
        {
            string mod = For(source);
            if (mod != null || component == null)
            {
                return mod;
            }
            try
            {
                Dictionary<Assembly, string> plugins = Plugins();
                Transform t = component.transform;
                for (int depth = 0; t != null && depth < MaxParents; depth++, t = t.parent)
                {
                    foreach (MonoBehaviour behaviour in t.GetComponents<MonoBehaviour>())
                    {
                        if (behaviour != null && plugins.TryGetValue(behaviour.GetType().Assembly, out mod))
                        {
                            return mod;
                        }
                    }
                }
            }
            catch
            {
            }
            return null;
        }

        private static string FromStack()
        {
            Dictionary<Assembly, string> plugins = Plugins();
            var trace = new StackTrace(2, false);
            for (int i = 0; i < trace.FrameCount; i++)
            {
                Type type = trace.GetFrame(i)?.GetMethod()?.DeclaringType;
                if (type != null && plugins.TryGetValue(type.Assembly, out string mod))
                {
                    return mod;
                }
            }
            return null;
        }

        // Plugin assemblies by name, leaving out this mod and the framework's
        // core and libraries, which only pass text along.
        private static Dictionary<Assembly, string> Plugins()
        {
            // Rebuilt while BepInEx is still starting plugins.
            if (_plugins != null && _pluginCount == Chainloader.PluginInfos.Count)
            {
                return _plugins;
            }
            _pluginCount = Chainloader.PluginInfos.Count;
            var map = new Dictionary<Assembly, string>();
            Assembly own = typeof(ModTextOwners).Assembly;
            foreach (PluginInfo info in Chainloader.PluginInfos.Values)
            {
                Assembly assembly = info.Instance != null ? info.Instance.GetType().Assembly : null;
                if (assembly == null || assembly == own || map.ContainsKey(assembly)) continue;
                if ((assembly.GetName().Name ?? "").StartsWith("DragNWash.ModFramework", StringComparison.Ordinal)) continue;
                map[assembly] = info.Metadata.Name;
            }
            _plugins = map;
            return map;
        }
    }
}
