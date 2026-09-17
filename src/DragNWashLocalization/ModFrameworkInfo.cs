using System;
using System.IO;
using DragNWash.ModFramework;

namespace DragNWashLocalization
{
    // Tells Drag'n Wash ModFramework's Mods screen what this mod is.
    internal static class ModFrameworkInfo
    {
        // Shown on the Mods screen; translation packs key it by this exact English.
        internal const string Description = "Play Drag'n Wash in 13 languages. Translates dialogue, choices, UI and options.";

        internal static void Register()
        {
            try
            {
                ModFramework.Register(new ModInfo
                {
                    Guid = Plugin.PluginGuid,
                    DisplayName = "Drag'n Wash Localization",
                    Description = Description,
                    Authors = new[] { "TomXV" },
                    Website = "https://github.com/TomXV/dragnwash-localization",
                    // The Mods screen tells players when a newer release is out.
                    UpdateRepository = "TomXV/dragnwash-localization",
                    // The logo Mister ERIO drew for this mod, next to the DLL.
                    IconPath = Path.Combine(Path.GetDirectoryName(typeof(ModFrameworkInfo).Assembly.Location) ?? "", "icon.png"),
                    // The framework may reload this DLL while the game runs
                    // (developer tools): Harmony ID = GUID, everything registered
                    // through the framework, and OnDestroy cleans up (GUIDE rule 10).
                    Reloadable = true,
                });
            }
            catch (Exception ex)
            {
                Plugin.Log($"[modframework] Could not register with Drag'n Wash ModFramework: {ex.Message}");
            }
        }
    }
}
