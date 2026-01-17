using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using GameWorld2;
using System.IO;
using System.Linq;

namespace TranslationPlugin
{
    public class TranslationPatches
    {
        private static ManualLogSource Logger => Plugin.Logger;

        [HarmonyPatch(typeof(Translator), "FoundFile")]
        [HarmonyPrefix]
        public static bool FoundFile_Prefix(Translator __instance, string pFilepath)
        {
            // Check if this file belongs to any of our custom languages
            LanguageConfig matchedLang = null;

            foreach (var lang in TranslationConfig.Languages)
            {
                if (pFilepath.EndsWith(".mtf") && pFilepath.Contains("." + lang.FileIdentifier))
                {
                    // Validate folder path
                    string unifiedPath = pFilepath.Replace('\\', '/');

                    // Check if the path is in the correct translation folder
                    if (!string.IsNullOrEmpty(lang.TranslationFolder))
                    {
                        if (unifiedPath.Contains("/Translations/" + lang.TranslationFolder + "/"))
                        {
                            matchedLang = lang;
                            break;
                        }
                        else
                        {
                            // File matches identifier but wrong folder - log debug info
                            Logger.LogDebug($"File {pFilepath} matches identifier '{lang.FileIdentifier}' but not in folder '{lang.TranslationFolder}'");
                        }
                    }
                    else
                    {
                        matchedLang = lang;
                        break;
                    }
                }
            }

            if (matchedLang == null)
            {
                // Not a custom language file, let the game handle it
                return true;
            }

            Logger.LogInfo($"Found custom translation file for [{matchedLang.Code}]: {pFilepath}");

            try
            {
                FieldInfo dictField = AccessTools.Field(typeof(Translator), "_dict");
                var dict = (Dictionary<Translator.Language, Dictionary<string, Dictionary<string, string>>>)dictField.GetValue(__instance);

                var customLang = (Translator.Language)matchedLang.CustomLanguageId;

                if (!dict.ContainsKey(customLang))
                {
                    dict.Add(customLang, new Dictionary<string, Dictionary<string, string>>());
                    Logger.LogInfo($"Initialized dictionary for language: {matchedLang.DisplayName} (ID: {matchedLang.CustomLanguageId})");
                }

                MethodInfo loadMethod = AccessTools.Method(typeof(Translator), "LoadTranslationsFile");
                loadMethod.Invoke(__instance, new object[] { pFilepath, customLang });
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error loading custom translation file {pFilepath}: {ex}");
            }

            return false;
        }

        [HarmonyPatch(typeof(Translator), "LoadTranslationFiles")]
        [HarmonyPostfix]
        public static void LoadTranslationFiles_Postfix(Translator __instance)
        {
            try
            {
                FieldInfo dictField = AccessTools.Field(typeof(Translator), "_dict");
                var dict = (Dictionary<Translator.Language, Dictionary<string, Dictionary<string, string>>>)dictField.GetValue(__instance);

                // Initialize dictionaries for all custom languages
                foreach (var lang in TranslationConfig.Languages)
                {
                    var customLang = (Translator.Language)lang.CustomLanguageId;

                    if (!dict.ContainsKey(customLang))
                    {
                        Logger.LogInfo($"Initializing dictionary for {lang.DisplayName} ({lang.Code}) in LoadTranslationFiles_Postfix.");
                        dict.Add(customLang, new Dictionary<string, Dictionary<string, string>>());
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in LoadTranslationFiles_Postfix: {ex}");
            }
        }

        [HarmonyPatch(typeof(World), "RefreshTranslationLanguage")]
        [HarmonyPrefix]
        public static bool RefreshTranslationLanguage_Prefix(World __instance)
        {
            string currentLangCode = __instance.settings.translationLanguage;

            // Check if the current language code matches any custom language
            var langConfig = TranslationConfig.GetLanguageByCode(currentLangCode);

            if (langConfig != null)
            {
                Logger.LogInfo($"RefreshTranslationLanguage: Setting language to custom: {langConfig.DisplayName} ({langConfig.Code})");

                // Set this as the active language
                TranslationConfig.SetActiveLanguage(currentLangCode);

                try
                {
                    MethodInfo setLanguageMethod = AccessTools.Method(typeof(Translator), "SetLanguage");
                    setLanguageMethod.Invoke(__instance.translator, new object[] { (Translator.Language)langConfig.CustomLanguageId });
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error setting custom language: {ex}");
                }

                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(Translator), "Get")]
        [HarmonyPrefix]
        public static bool Get_Prefix(Translator __instance, string pSentenceToTranslate, string pDialogue, ref string __result)
        {
            try
            {
                FieldInfo languageField = AccessTools.Field(typeof(Translator), "_language");
                var currentLang = (Translator.Language)languageField.GetValue(__instance);
                int currentLangId = (int)currentLang;

                // Check if the current language is one of our custom languages
                var langConfig = TranslationConfig.GetLanguageById(currentLangId);

                if (langConfig != null)
                {
                    FieldInfo dictField = AccessTools.Field(typeof(Translator), "_dict");
                    var dict = (Dictionary<Translator.Language, Dictionary<string, Dictionary<string, string>>>)dictField.GetValue(__instance);

                    if (dict == null)
                    {
                        Logger.LogError("Translator dictionary is null!");
                        __result = pSentenceToTranslate;
                        return false;
                    }

                    // 1. Get Custom Translation
                    Dictionary<string, Dictionary<string, string>> langDict;
                    dict.TryGetValue((Translator.Language)langConfig.CustomLanguageId, out langDict);

                    string customText = null;
                    if (langDict != null)
                    {
                        Dictionary<string, string> dialogueDict;
                        if (langDict.TryGetValue(pDialogue, out dialogueDict))
                        {
                            dialogueDict.TryGetValue(pSentenceToTranslate, out customText);
                        }
                    }

                    // 2. Get English Translation (for Bilingual mode or Fallback)
                    string englishText = null;
                    Dictionary<string, Dictionary<string, string>> engDict;
                    if (dict.TryGetValue(Translator.Language.ENGLISH, out engDict))
                    {
                        Dictionary<string, string> engDialogueDict;
                        if (engDict.TryGetValue(pDialogue, out engDialogueDict))
                        {
                            engDialogueDict.TryGetValue(pSentenceToTranslate, out englishText);
                        }
                    }
                    if (string.IsNullOrEmpty(englishText)) englishText = pSentenceToTranslate;

                    // 3. Return result based on what we found
                    if (customText != null)
                    {
                        if (Plugin.BilingualMode)
                        {
                            __result = englishText + "\n" + customText;
                        }
                        else
                        {
                            __result = customText;
                        }
                        return false;
                    }

                    // If Custom MISSING:
                    if (TranslationConfig.FallbackToEnglish)
                    {
                        Logger.LogWarning($"[{langConfig.Code}] Fallback to English for '{pSentenceToTranslate}' in '{pDialogue}'");
                        __result = englishText;
                        return false;
                    }

                    __result = pSentenceToTranslate;
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in Translator.Get Prefix: {ex}");
                return true;
            }

            return true;
        }
    }
}
