using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using GameWorld2;

namespace ElseHeartbreakLocalization;

public class TranslationPatches
{
    private static ManualLogSource Logger => Plugin.Logger;

    [HarmonyPatch(typeof(Translator), "FoundFile"), HarmonyPrefix]
    public static bool FoundFile(Translator __instance, string pFilepath)
    {
        var lang = TranslationConfig.Languages.Find(l =>
            pFilepath.EndsWith(".mtf") &&
            pFilepath.Contains("." + l.FileIdentifier) &&
            (string.IsNullOrEmpty(l.TranslationFolder) || pFilepath.Replace('\\', '/').Contains("/Translations/" + l.TranslationFolder + "/"))
        );

        if (lang == null) return true;

        Logger.LogInfo($"Found custom file [{lang.Code}]: {pFilepath}");
        try
        {
            var dict = (Dictionary<Translator.Language, Dictionary<string, Dictionary<string, string>>>)
                AccessTools.Field(typeof(Translator), "_dict").GetValue(__instance);

            var customLang = (Translator.Language)lang.CustomLanguageId;
            if (!dict.ContainsKey(customLang))
            {
                dict.Add(customLang, new Dictionary<string, Dictionary<string, string>>());
                Logger.LogInfo($"Init dict for {lang.DisplayName} ({lang.CustomLanguageId})");
            }

            AccessTools.Method(typeof(Translator), "LoadTranslationsFile")
                .Invoke(__instance, new object[] { pFilepath, customLang });
        }
        catch (Exception ex) { Logger.LogError($"Error loading {pFilepath}: {ex}"); }

        return false;
    }

    [HarmonyPatch(typeof(Translator), "LoadTranslationFiles"), HarmonyPostfix]
    public static void LoadTranslationFiles(Translator __instance)
    {
        try
        {
             var dict = (Dictionary<Translator.Language, Dictionary<string, Dictionary<string, string>>>)
                AccessTools.Field(typeof(Translator), "_dict").GetValue(__instance);

             foreach(var lang in TranslationConfig.Languages)
             {
                 var cl = (Translator.Language)lang.CustomLanguageId;
                 if (!dict.ContainsKey(cl)) dict.Add(cl, new Dictionary<string, Dictionary<string, string>>());
             }
        }
        catch (Exception ex) { Logger.LogError($"LoadTranslationFiles err: {ex}"); }
    }

    [HarmonyPatch(typeof(World), "RefreshTranslationLanguage"), HarmonyPrefix]
    public static bool RefreshTranslationLanguage(World __instance)
    {
        var code = __instance.settings.translationLanguage;
        var lang = TranslationConfig.GetLanguageByCode(code);
        if (lang == null) return true;

        Logger.LogInfo($"Setting language: {lang.DisplayName} ({lang.Code})");
        if (TranslationConfig.SetActiveLanguage(code)) MenuTranslations.Reload();

        try
        {
            AccessTools.Method(typeof(Translator), "SetLanguage")
                .Invoke(__instance.translator, new object[] { (Translator.Language)lang.CustomLanguageId });
        }
        catch (Exception ex) { Logger.LogError($"SetLanguage err: {ex}"); }
        return false;
    }

    [HarmonyPatch(typeof(Translator), "Get"), HarmonyPrefix]
    public static bool Get(Translator __instance, string pSentenceToTranslate, string pDialogue, ref string __result)
    {
        try
        {
            var curLang = (Translator.Language)AccessTools.Field(typeof(Translator), "_language").GetValue(__instance);
            var langConfig = TranslationConfig.GetLanguageById((int)curLang);

            if (langConfig == null) return true;

            var dict = (Dictionary<Translator.Language, Dictionary<string, Dictionary<string, string>>>)
                 AccessTools.Field(typeof(Translator), "_dict").GetValue(__instance);

            if (dict == null) { __result = pSentenceToTranslate; return false; }

            string custom = null, english = null;

            // 1. Custom
            if (dict.TryGetValue(curLang, out var lDict) &&
                lDict.TryGetValue(pDialogue, out var dDict))
                dDict.TryGetValue(pSentenceToTranslate, out custom);

            // 2. English
            if (dict.TryGetValue(Translator.Language.ENGLISH, out var eDict) &&
                eDict.TryGetValue(pDialogue, out var edDict))
                edDict.TryGetValue(pSentenceToTranslate, out english);

            english ??= pSentenceToTranslate;

            if (custom != null)
            {
                __result = TranslationConfig.BilingualModeEnabled ? $"{english}\n{custom}" : custom;
                return false;
            }

            if (TranslationConfig.FallbackToEnglish)
            {
                __result = english;
                return false;
            }

            __result = pSentenceToTranslate;
            return false;
        }
        catch (Exception ex) { Logger.LogError($"Translator.Get err: {ex}"); return true; }
    }
}
