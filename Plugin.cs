using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using UnityEngine;

namespace ElseHeartbreakLocalization
{
    [BepInPlugin("org.bepinex.plugins.ElseHeartbreakLocalization", "Else Heartbreak Localization", "1.0.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;

        // Bilingual Mode toggle - initialized from config, can be toggled with hotkey
        public static bool BilingualMode = true;

        private void Awake()
        {
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            TranslationConfig.Init(Config);
            MenuTranslations.Init();

            // Initialize BilingualMode from config
            BilingualMode = TranslationConfig.BilingualModeEnabled;
            Logger.LogInfo($"Bilingual Mode initialized: {BilingualMode}");

            Harmony.CreateAndPatchAll(typeof(TranslationPatches));
            Harmony.CreateAndPatchAll(typeof(UIPatches));
            Harmony.CreateAndPatchAll(typeof(MenuPatches));
        }

        private void Update()
        {
            // Toggle Bilingual Mode with hotkey (default F11)
            if (Input.GetKeyDown(TranslationConfig.BilingualToggleKey))
            {
                BilingualMode = !BilingualMode;
                Logger.LogInfo($"Bilingual Mode Toggled: {BilingualMode}");
            }
        }
    }
}
