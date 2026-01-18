using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TranslationPlugin;

public class LanguageConfig
{
    public string Code { get; }
    public string DisplayName { get; }
    public string TranslationFolder { get; }
    public string FileIdentifier { get; }
    public int CustomLanguageId { get; }
    public float CharacterWidthMultiplier { get; }

    public LanguageConfig(string code, string displayName, string translationFolder, string fileIdentifier, int customLanguageId, float charWidthMultiplier)
    {
        Code = code;
        DisplayName = displayName;
        TranslationFolder = translationFolder;
        FileIdentifier = fileIdentifier;
        CustomLanguageId = customLanguageId;
        CharacterWidthMultiplier = charWidthMultiplier;
    }
}

public static class TranslationConfig
{
    public static List<LanguageConfig> Languages { get; } = new();
    public static LanguageConfig ActiveLanguage { get; private set; }

    public static KeyCode BilingualToggleKey { get; private set; } = KeyCode.F11;
    public static bool BilingualModeEnabled { get; private set; } = true;
    public static bool FallbackToEnglish { get; private set; } = true;

    private const int BaseCustomLanguageId = 100;
    private static string IniFilePath => Path.Combine(Path.GetDirectoryName(Application.dataPath), "localization.ini");

    public static void Init(ConfigFile bepInExConfig)
    {
        Plugin.Logger.LogInfo($"Looking for localization.ini at: {IniFilePath}");

        if (!File.Exists(IniFilePath))
        {
            Plugin.Logger.LogWarning("localization.ini not found. Creating default.");
            CreateDefaultIni(IniFilePath);
        }

        LoadFromIni(IniFilePath);

        if (Languages.Count == 0)
        {
            Plugin.Logger.LogWarning("No languages defined. Using default Chinese.");
            Languages.Add(new LanguageConfig("chn", "中文", "Chinese", "chn", BaseCustomLanguageId, 2.0f));
        }

        ActiveLanguage = Languages[0];
        Plugin.Logger.LogInfo($"Loaded {Languages.Count} custom language(s). Active: {ActiveLanguage.Code}");
    }

    private static void LoadFromIni(string path)
    {
        Languages.Clear();
        var lines = File.ReadAllLines(path)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l) && !l.StartsWith(";") && !l.StartsWith("#"));

        var currentSection = "";
        var sectionData = new Dictionary<string, string>();
        int languageIndex = 0;

        foreach (var line in lines)
        {
            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                ProcessSection(currentSection, sectionData, ref languageIndex);
                currentSection = line.Substring(1, line.Length - 2);
                sectionData.Clear();
                continue;
            }

            var parts = line.Split(new[] { '=' }, 2);
            if (parts.Length == 2)
            {
                var key = parts[0].Trim();
                var val = parts[1].Trim();
                sectionData[key] = val;

                if (currentSection == "General") ApplyGeneralSetting(key, val);
            }
        }
        ProcessSection(currentSection, sectionData, ref languageIndex);
    }

    private static void ProcessSection(string section, Dictionary<string, string> data, ref int index)
    {
        if (section != "General" && !string.IsNullOrEmpty(section))
        {
            ParseLanguageSection(section, data, index++);
        }
    }

    private static void ApplyGeneralSetting(string key, string value)
    {
        try
        {
            switch (key.ToLowerInvariant())
            {
                case "bilingualtogglekey": BilingualToggleKey = (KeyCode)Enum.Parse(typeof(KeyCode), value, true); break;
                case "bilingualmodeenabled": bool.TryParse(value, out var b); BilingualModeEnabled = b; break;
                case "fallbacktoenglish": bool.TryParse(value, out var f); FallbackToEnglish = f; break;
            }
        }
        catch { /* Ignore invalid enum parse */ }
    }

    private static void ParseLanguageSection(string sectionName, Dictionary<string, string> data, int index)
    {
        string Get(string k, string d) => data.ContainsKey(k) ? data[k] : d;
        var code = Get("Code", sectionName.ToLowerInvariant());

        float.TryParse(Get("CharacterWidthMultiplier", "2.0"), out var width);

        Languages.Add(new LanguageConfig(
            code,
            Get("DisplayName", sectionName),
            Get("TranslationFolder", sectionName),
            Get("FileIdentifier", code),
            BaseCustomLanguageId + index,
            width == 0 ? 2.0f : width
        ));
    }

    private static void CreateDefaultIni(string path)
    {
        try { File.WriteAllText(path, LoadResourceText("TranslationPlugin.assets.localization.ini"), System.Text.Encoding.UTF8); }
        catch (Exception ex) { Plugin.Logger.LogError($"Failed to create default ini: {ex.Message}"); }
    }

    private static string LoadResourceText(string resourceName)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        if (stream == null) throw new Exception($"Resource '{resourceName}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static bool SetActiveLanguage(string code)
    {
        var lang = GetLanguageByCode(code);
        if (lang == null) return false;

        ActiveLanguage = lang;
        Plugin.Logger.LogInfo($"Active language set to: {lang.DisplayName}");
        return true;
    }

    public static LanguageConfig GetLanguageByCode(string code) => Languages.FirstOrDefault(l => l.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
    public static LanguageConfig GetLanguageById(int id) => Languages.FirstOrDefault(l => l.CustomLanguageId == id);
    public static bool IsCustomLanguageCode(string code) => GetLanguageByCode(code) != null;

    // Output formatting specific helpers could exist here if needed, but keeping separate.

    // Compatibility properties
    public static string LanguageCode => ActiveLanguage?.Code ?? "chn";
    public static string DisplayName => ActiveLanguage?.DisplayName ?? "中文";
    public static string TranslationFolder => ActiveLanguage?.TranslationFolder ?? "Chinese";
    public static string FileIdentifier => ActiveLanguage?.FileIdentifier ?? "chn";
    public static int CustomLanguageId => ActiveLanguage?.CustomLanguageId ?? BaseCustomLanguageId;
}
