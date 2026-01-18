using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ElseHeartbreakLocalization;

public static partial class MenuTranslations
{
    private static ManualLogSource Logger => Plugin.Logger;

    private static readonly Dictionary<string, string> _tooltips = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> _verbs = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> _notifications = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> _dialogues = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> _menuText = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> _composedCache = new();

    private static readonly Regex _mtfRegex = new("\"(.+)\" => \"(.+)\"");
    private static readonly string[] _prepositions = { " on ", " in ", " at ", " from ", " into " };

    public static void Init() => LoadTranslations(TranslationConfig.ActiveLanguage);
    public static void Reload() => LoadTranslations(TranslationConfig.ActiveLanguage);

    private static void LoadTranslations(LanguageConfig language)
    {
        ClearDictionaries();
        if (language == null) return;

        var menuPath = Path.Combine(Path.Combine(Application.dataPath, "InitData"), "MenuTranslations");
        menuPath = Path.Combine(menuPath, language.TranslationFolder);

        Logger.LogInfo($"Loading translations from: {menuPath}");

        if (!Directory.Exists(menuPath))
        {
            Logger.LogWarning($"Directory missing: {menuPath}");
            return;
        }

        LoadVariants(menuPath, "tooltips", language.FileIdentifier, _tooltips);
        LoadVariants(menuPath, "verbs", language.FileIdentifier, _verbs);
        LoadVariants(menuPath, "notifications", language.FileIdentifier, _notifications);
        LoadVariants(menuPath, "dialogues", language.FileIdentifier, _dialogues);
        LoadVariants(menuPath, "menutext", language.FileIdentifier, _menuText);
        LoadVariants(menuPath, "liquidtypes", language.FileIdentifier, _tooltips);
        LoadVariants(menuPath, "drugtypes", language.FileIdentifier, _tooltips);
        LoadVariants(menuPath, "swedishdialogues", language.FileIdentifier, _dialogues);
        LoadVariants(menuPath, "errors", language.FileIdentifier, _notifications);
        LoadVariants(menuPath, "actiondescriptions", language.FileIdentifier, _menuText);

        Logger.LogInfo($"Loaded: {_tooltips.Count} tooltips, {_verbs.Count} verbs, {_notifications.Count} notifications");
    }

    private static void ClearDictionaries()
    {
        _tooltips.Clear(); _verbs.Clear(); _notifications.Clear();
        _dialogues.Clear(); _menuText.Clear(); _composedCache.Clear();
    }

    private static void LoadVariants(string dir, string baseName, string fid, Dictionary<string, string> dict)
    {
        void Load(string f) => LoadMtfFile(Path.Combine(dir, f), dict);
        Load($"{baseName}.mtf");
        if (!string.IsNullOrEmpty(fid)) Load($"{baseName}.{fid}.mtf");
        Load($"{baseName}_override.mtf");
        if (!string.IsNullOrEmpty(fid)) Load($"{baseName}_override.{fid}.mtf");
    }

    private static void LoadMtfFile(string path, Dictionary<string, string> target)
    {
        if (!File.Exists(path)) return;
        try
        {
            foreach (var line in File.ReadAllLines(path))
            {
                var match = _mtfRegex.Match(line);
                if (match.Success) target[match.Groups[1].Value] = match.Groups[2].Value;
            }
        }
        catch (Exception ex) { Logger.LogError($"Error loading {path}: {ex.Message}"); }
    }

    public static string TranslateTooltip(string s) => TryGet(_tooltips, s);
    public static string TranslateVerb(string s) => TryGet(_verbs, s);
    public static string TranslateNotification(string s) => TryGet(_notifications, s);
    public static string TranslateDialogue(string s) => TryGet(_dialogues, s);
    public static string TranslateMenuText(string s) => TryGet(_menuText, s);

    private static string TryGet(Dictionary<string, string> dict, string key) =>
        !string.IsNullOrEmpty(key) && dict.TryGetValue(key, out var val) ? val : null;

    public static string FormatBilingual(string orig, string trans) =>
        string.IsNullOrEmpty(trans) ? orig : (!TranslationConfig.BilingualModeEnabled ? trans : $"{orig} [{trans}]");

    public static string TranslateComposedTooltip(string composed)
    {
        if (string.IsNullOrEmpty(composed)) return composed;
        if (_composedCache.TryGetValue(composed, out var cached))
            return cached == null ? composed : FormatBilingual(composed, cached);

        var trans = ResolveComposed(composed);
        _composedCache[composed] = trans;
        return trans != null ? FormatBilingual(composed, trans) : composed;
    }

    private static string ResolveComposed(string text)
    {
        if (TranslateMenuText(text) is { } m) return m;
        if (TryTranslateWithPreposition(text) is { } p) return p;

        // Verb + Noun logic
        var verbs = _verbs.Keys.OrderByDescending(k => k.Length).ToList();

        foreach (var verb in verbs)
        {
            if (text.StartsWith(verb + " ", StringComparison.OrdinalIgnoreCase))
            {
                var vTrans = _verbs[verb];
                var remainder = text.Substring(verb.Length + 1);
                var nounBase = ExtractSuffix(remainder, out var suffix);

                if (string.IsNullOrEmpty(nounBase.Trim())) return vTrans;

                var finalNoun = TranslateTooltip(nounBase) ?? nounBase;
                if (TranslateTooltip(nounBase) == null) Logger.LogWarning($"Missing noun: '{nounBase}'");

                var finalSuffix = string.IsNullOrEmpty(suffix) ? "" :
                    (TranslateTooltip(suffix) is { } sTrans ? $" ({sTrans})" : $" ({suffix})");

                return vTrans.Contains("[N]")
                    ? vTrans.Replace("[N]", finalNoun) + finalSuffix
                    : vTrans + finalNoun + finalSuffix;
            }
        }

        return TranslateVerb(text) ?? TranslateTooltip(text) ?? TranslateNotification(text);
    }

    private static string TryTranslateWithPreposition(string text)
    {
        // Find last matching preposition
        var prepIdx = -1;
        string prepStr = null;

        foreach (var p in _prepositions)
        {
            var idx = text.IndexOf(p, StringComparison.OrdinalIgnoreCase);
            if (idx > 0 && idx > prepIdx)
            {
                prepIdx = idx;
                prepStr = p;
            }
        }

        if (prepStr == null) return null;

        var before = text.Substring(0, prepIdx).Trim();
        var obj = text.Substring(prepIdx + prepStr.Length).Trim();

        // 1. Check "verb prep" combo directly
        var verbKey = before + prepStr.TrimEnd();
        if (TranslateVerb(verbKey) is { } vTrans)
        {
            var objTrans = TranslateTooltip(obj) ?? obj;
            return vTrans.Contains("[N]") ? vTrans.Replace("[N]", objTrans) : vTrans + objTrans;
        }

        // 2. Base Verb Pattern
        var verbs = _verbs.Keys.OrderByDescending(k => k.Length).ToList();
        foreach (var v in verbs)
        {
            if (before.StartsWith(v + " ", StringComparison.OrdinalIgnoreCase))
            {
                var noun = before.Substring(v.Length + 1).Trim();
                var nounTrans = TranslateTooltip(noun) ?? noun;
                var objTrans = TranslateTooltip(obj) ?? obj;
                var verbTrans = _verbs[v];

                return verbTrans.Contains("[N]")
                    ? verbTrans.Replace("[N]", objTrans) + nounTrans
                    : verbTrans + objTrans + nounTrans;
            }
            else if (before.Equals(v, StringComparison.OrdinalIgnoreCase))
            {
                var objTrans = TranslateTooltip(obj) ?? obj;
                var verbTrans = _verbs[v];
                return verbTrans.Contains("[N]")
                    ? verbTrans.Replace("[N]", objTrans)
                    : verbTrans + objTrans;
            }
        }

        return null;
    }

    private static readonly Regex _extractionRegex = new Regex(@"\s*\((\d+(?:\.\d+)?%|[a-zA-Z]+)\)$");

    private static string ExtractSuffix(string text, out string suffixContent)
    {
        var match = _extractionRegex.Match(text);
        if (match.Success)
        {
            suffixContent = match.Groups[1].Value;
            return text.Substring(0, match.Index).Trim();
        }
        suffixContent = "";
        return text;
    }
}
