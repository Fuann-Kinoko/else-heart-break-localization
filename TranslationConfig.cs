using BepInEx.Configuration;
using UnityEngine;

namespace TranslationPlugin
{
    public static class TranslationConfig
    {
        public static ConfigEntry<string> LanguageCode;
        public static ConfigEntry<string> FileIdentifier;
        public static ConfigEntry<KeyCode> SwitchLanguageKey;
        public static ConfigEntry<bool> FallbackToEnglish;

        public const int CustomLanguageId = 100;

        public static void Init(ConfigFile config)
        {
            LanguageCode = config.Bind("General", "LanguageCode", "chn", "The language code to use in the game settings (e.g., 'chn').");
            FileIdentifier = config.Bind("General", "FileIdentifier", "chn", "The identifier string in filenames (e.g., 'chn' for files ending in .chn.mtf).");
            SwitchLanguageKey = config.Bind("General", "SwitchLanguageKey", KeyCode.F11, "The key to switch to the custom language in-game.");
            FallbackToEnglish = config.Bind("General", "FallbackToEnglish", true, "If a translation is missing in the custom language, try to find it in English.");
        }
    }
}
