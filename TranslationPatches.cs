using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using GameWorld2;

namespace TranslationPlugin
{
    public class TranslationPatches
    {
        private static ManualLogSource Logger => Plugin.Logger;

        [HarmonyPatch(typeof(Translator), "FoundFile")]
        [HarmonyPrefix]
        public static bool FoundFile_Prefix(Translator __instance, string pFilepath)
        {
            if (pFilepath.EndsWith(".mtf") && pFilepath.Contains("." + TranslationConfig.FileIdentifier.Value))
            {
                Logger.LogInfo($"Found custom translation file: {pFilepath}");

                try
                {
                    FieldInfo dictField = AccessTools.Field(typeof(Translator), "_dict");
                    var dict = (Dictionary<Translator.Language, Dictionary<string, Dictionary<string, string>>>)dictField.GetValue(__instance);

                    var customLang = (Translator.Language)TranslationConfig.CustomLanguageId;

                    if (!dict.ContainsKey(customLang))
                    {
                        dict.Add(customLang, new Dictionary<string, Dictionary<string, string>>());
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

            return true;
        }

        [HarmonyPatch(typeof(Translator), "LoadTranslationFiles")]
        [HarmonyPostfix]
        public static void LoadTranslationFiles_Postfix(Translator __instance)
        {
            try
            {
                FieldInfo dictField = AccessTools.Field(typeof(Translator), "_dict");
                var dict = (Dictionary<Translator.Language, Dictionary<string, Dictionary<string, string>>>)dictField.GetValue(__instance);
                var customLang = (Translator.Language)TranslationConfig.CustomLanguageId;

                if (!dict.ContainsKey(customLang))
                {
                    Logger.LogInfo($"Initializing dictionary for custom language {customLang} in LoadTranslationFiles_Postfix.");
                    dict.Add(customLang, new Dictionary<string, Dictionary<string, string>>());
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
            // Logic:
            // 1. If Global Toggle is ON, force custom language.
            // 2. If Global Toggle is OFF, but settings says Custom Language (loaded from save?), respect settings?
            //    -> User wants F11 to be "global switch". If I launch game (OFF) and load a CHN save, should it be CHN?
            //    -> Ideally yes, save file should be respected unless I explicitly toggled.
            //    -> To keep it simple: If Active OR settings match, use custom.

            bool shouldUseCustom = Plugin.CustomLanguageActive || __instance.settings.translationLanguage == TranslationConfig.LanguageCode.Value;

            if (shouldUseCustom)
            {
                Logger.LogInfo($"RefreshTranslationLanguage: Setting language to custom: {TranslationConfig.LanguageCode.Value} (Toggle: {Plugin.CustomLanguageActive})");

                // Ensure settings match reality
                if (__instance.settings.translationLanguage != TranslationConfig.LanguageCode.Value)
                {
                    __instance.settings.translationLanguage = TranslationConfig.LanguageCode.Value;
                }

                try
                {
                    MethodInfo setLanguageMethod = AccessTools.Method(typeof(Translator), "SetLanguage");
                    setLanguageMethod.Invoke(__instance.translator, new object[] { (Translator.Language)TranslationConfig.CustomLanguageId });
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

                if ((int)currentLang == TranslationConfig.CustomLanguageId)
                {
                    FieldInfo dictField = AccessTools.Field(typeof(Translator), "_dict");
                    var dict = (Dictionary<Translator.Language, Dictionary<string, Dictionary<string, string>>>)dictField.GetValue(__instance);

                    if (dict == null)
                    {
                        Logger.LogError("Translator dictionary is null!");
                         __result = pSentenceToTranslate;
                         return false;
                    }

                    Dictionary<string, Dictionary<string, string>> langDict;
                    if (!dict.TryGetValue((Translator.Language)TranslationConfig.CustomLanguageId, out langDict))
                    {
                         Logger.LogWarning("Custom language dictionary missing in Get!");
                         langDict = null;
                    }

                    string result = null;
                    if (langDict != null)
                    {
                        Dictionary<string, string> dialogueDict;
                        if (langDict.TryGetValue(pDialogue, out dialogueDict))
                        {
                             dialogueDict.TryGetValue(pSentenceToTranslate, out result);
                        }
                    }

                    if (result != null)
                    {
                        __result = result;
                        return false;
                    }

                    if (TranslationConfig.FallbackToEnglish.Value)
                    {
                        Dictionary<string, Dictionary<string, string>> engDict;
                        if (dict.TryGetValue(Translator.Language.ENGLISH, out engDict))
                        {
                             Dictionary<string, string> engDialogueDict;
                             if (engDict.TryGetValue(pDialogue, out engDialogueDict))
                             {
                                 if (engDialogueDict.TryGetValue(pSentenceToTranslate, out result))
                                 {
                                     Logger.LogWarning($"Fallback to English for '{pSentenceToTranslate}' in '{pDialogue}'");
                                     __result = result;
                                     return false;
                                 }
                             }
                        }
                    }

                    Logger.LogWarning($"No translation found for '{pSentenceToTranslate}' in '{pDialogue}' (Custom & Fallback failed).");
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
