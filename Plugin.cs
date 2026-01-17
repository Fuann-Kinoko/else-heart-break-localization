using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using UnityEngine;

namespace TranslationPlugin
{
    [BepInPlugin("org.bepinex.plugins.EHTranslationPlugin", "Translation Plugin", "1.0.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;

        // Global toggle state
        public static bool CustomLanguageActive = false;

        private void Awake()
        {
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            TranslationConfig.Init(Config);

            Harmony.CreateAndPatchAll(typeof(TranslationPatches));
            Harmony.CreateAndPatchAll(typeof(UIPatches));
        }

        private void Update()
        {
            if (Input.GetKeyDown(TranslationConfig.SwitchLanguageKey.Value))
            {
                CustomLanguageActive = !CustomLanguageActive;
                Logger.LogInfo($"Global Custom Language Toggle: {CustomLanguageActive}");

                try
                {
                    // If World is loaded, apply immediate change
                    if (WorldOwner.instance != null && WorldOwner.instance.worldIsLoaded)
                    {
                        var world = WorldOwner.instance.world;
                        if (world != null)
                        {
                            if (CustomLanguageActive)
                            {
                                Logger.LogInfo($"Applying Custom Language {TranslationConfig.LanguageCode.Value} immediately.");
                                world.settings.translationLanguage = TranslationConfig.LanguageCode.Value;
                                world.RefreshTranslationLanguage();
                            }
                            else
                            {
                                // Fallback to English if toggled off
                                Logger.LogInfo($"Reverting to English immediately.");
                                world.settings.translationLanguage = "eng";
                                world.RefreshTranslationLanguage();
                            }
                        }
                    }
                    else
                    {
                        Logger.LogInfo("World not loaded yet. Change queued for next World load.");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error in Hotkey Update: {ex}");
                }
            }
        }
    }
}
