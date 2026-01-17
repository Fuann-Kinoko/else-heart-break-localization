using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace TranslationPlugin
{
    /// <summary>
    /// Represents a single custom language configuration.
    /// </summary>
    public class LanguageConfig
    {
        public string Code { get; set; }              // e.g., "chn"
        public string DisplayName { get; set; }       // e.g., "中文"
        public string TranslationFolder { get; set; } // e.g., "Chinese"
        public string FileIdentifier { get; set; }    // e.g., "chn"
        public int CustomLanguageId { get; set; }     // e.g., 100

        public LanguageConfig(string code, string displayName, string translationFolder, string fileIdentifier, int customLanguageId)
        {
            Code = code;
            DisplayName = displayName;
            TranslationFolder = translationFolder;
            FileIdentifier = fileIdentifier;
            CustomLanguageId = customLanguageId;
        }
    }

    public static class TranslationConfig
    {
        // All defined languages from INI
        public static List<LanguageConfig> Languages { get; private set; } = new List<LanguageConfig>();

        // Currently active language (selected by user)
        public static LanguageConfig ActiveLanguage { get; private set; }

        // Global settings
        public static KeyCode BilingualToggleKey { get; private set; } = KeyCode.F11;
        public static bool BilingualModeEnabled { get; private set; } = true;
        public static bool FallbackToEnglish { get; private set; } = true;

        // Base ID for custom languages (game uses 0-3)
        private const int BaseCustomLanguageId = 100;

        // Path to the localization.ini file
        private static string IniFilePath;

        /// <summary>
        /// Initializes the configuration from localization.ini in the game root directory.
        /// Falls back to default Chinese if the file doesn't exist or is empty.
        /// </summary>
        public static void Init(ConfigFile bepInExConfig)
        {
            // Game root directory (where ElseHeartbreak.exe is)
            string gameRoot = Path.GetDirectoryName(Application.dataPath);
            IniFilePath = Path.Combine(gameRoot, "localization.ini");

            Plugin.Logger.LogInfo($"Looking for localization.ini at: {IniFilePath}");

            if (File.Exists(IniFilePath))
            {
                LoadFromIni(IniFilePath);
            }
            else
            {
                Plugin.Logger.LogWarning($"localization.ini not found at {IniFilePath}. Creating default configuration.");
                CreateDefaultIni(IniFilePath);
                LoadFromIni(IniFilePath);
            }

            // Ensure at least one language is defined
            if (Languages.Count == 0)
            {
                Plugin.Logger.LogWarning("No languages defined in INI. Using default Chinese.");
                Languages.Add(new LanguageConfig("chn", "中文", "Chinese", "chn", BaseCustomLanguageId));
            }

            // Set first language as active by default
            ActiveLanguage = Languages[0];

            // Log loaded languages
            Plugin.Logger.LogInfo($"Loaded {Languages.Count} custom language(s):");
            foreach (var lang in Languages)
            {
                Plugin.Logger.LogInfo($"  - {lang.Code}: {lang.DisplayName} (folder: {lang.TranslationFolder}, id: {lang.CustomLanguageId})");
            }
        }

        /// <summary>
        /// Parses the localization.ini file.
        /// </summary>
        private static void LoadFromIni(string path)
        {
            Languages.Clear();
            string currentSection = null;
            var sectionData = new Dictionary<string, string>();
            int languageIndex = 0;

            try
            {
                string[] lines = File.ReadAllLines(path);

                foreach (string rawLine in lines)
                {
                    string line = rawLine.Trim();

                    // Skip empty lines and comments
                    if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("#"))
                        continue;

                    // Section header
                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        // Save previous section if it was a language section
                        if (currentSection != null && currentSection != "General")
                        {
                            ParseLanguageSection(currentSection, sectionData, languageIndex++);
                        }

                        currentSection = line.Substring(1, line.Length - 2);
                        sectionData.Clear();
                        continue;
                    }

                    // Key=Value pair
                    int eqIndex = line.IndexOf('=');
                    if (eqIndex > 0)
                    {
                        string key = line.Substring(0, eqIndex).Trim();
                        string value = line.Substring(eqIndex + 1).Trim();
                        sectionData[key] = value;

                        // Handle General section immediately
                        if (currentSection == "General")
                        {
                            ApplyGeneralSetting(key, value);
                        }
                    }
                }

                // Don't forget the last section
                if (currentSection != null && currentSection != "General")
                {
                    ParseLanguageSection(currentSection, sectionData, languageIndex);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error reading localization.ini: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies a setting from the [General] section.
        /// </summary>
        private static void ApplyGeneralSetting(string key, string value)
        {
            switch (key.ToLowerInvariant())
            {
                case "bilingualtogglekey":
                    try
                    {
                        BilingualToggleKey = (KeyCode)Enum.Parse(typeof(KeyCode), value, true);
                    }
                    catch { /* Invalid key code, keep default */ }
                    break;
                case "bilingualmodeenabled":
                    bool bilingual;
                    if (bool.TryParse(value, out bilingual))
                        BilingualModeEnabled = bilingual;
                    break;
                case "fallbacktoenglish":
                    bool fallback;
                    if (bool.TryParse(value, out fallback))
                        FallbackToEnglish = fallback;
                    break;
            }
        }

        /// <summary>
        /// Parses a language section and adds it to the Languages list.
        /// </summary>
        private static void ParseLanguageSection(string sectionName, Dictionary<string, string> data, int index)
        {
            // Get values with defaults
            string code = GetValueOrDefault(data, "Code", sectionName.ToLowerInvariant());
            string displayName = GetValueOrDefault(data, "DisplayName", sectionName);
            string translationFolder = GetValueOrDefault(data, "TranslationFolder", sectionName);
            string fileIdentifier = GetValueOrDefault(data, "FileIdentifier", code);

            int customId = BaseCustomLanguageId + index;

            Languages.Add(new LanguageConfig(code, displayName, translationFolder, fileIdentifier, customId));
        }

        private static string GetValueOrDefault(Dictionary<string, string> data, string key, string defaultValue)
        {
            return data.TryGetValue(key, out var value) ? value : defaultValue;
        }

        /// <summary>
        /// Creates a default localization.ini with Chinese as the default language.
        /// </summary>
        private static void CreateDefaultIni(string path)
        {
            string defaultContent = @"; === Else Heartbreak Translation Plugin Configuration ===
; This file configures custom language support for the game.
; Place this file in the same directory as ElseHeartbreak.exe

[General]
; Enable bilingual mode by default (shows both English + Custom language)
; Can also be toggled in-game with the hotkey below
BilingualModeEnabled=true

; Hotkey to toggle bilingual mode in-game (press to switch on/off)
BilingualToggleKey=F11

; If a translation is missing, show English instead of the raw key
FallbackToEnglish=true

; ============================================================
; Language Definitions
; Each [SectionName] defines a new language.
; You can add multiple languages by creating new sections.
; ============================================================

[Chinese]
; Internal code used for language selection (saved to PlayerPrefs)
Code=chn

; Name displayed on the UI button in the main menu
DisplayName=中文

; Folder name inside InitData/Translations/ containing .mtf files
TranslationFolder=Chinese

; The identifier in translation filenames (e.g., dialogue.chn.mtf)
FileIdentifier=chn

; ============================================================
; Example: Add another language by uncommenting and modifying:
; ============================================================
; [Japanese]
; Code=jpn
; DisplayName=日本語
; TranslationFolder=Japanese
; FileIdentifier=jpn
";
            try
            {
                File.WriteAllText(path, defaultContent, System.Text.Encoding.UTF8);
                Plugin.Logger.LogInfo($"Created default localization.ini at: {path}");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Failed to create default localization.ini: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the active language by its code.
        /// </summary>
        public static bool SetActiveLanguage(string code)
        {
            var lang = Languages.FirstOrDefault(l => l.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
            if (lang != null)
            {
                ActiveLanguage = lang;
                Plugin.Logger.LogInfo($"Active language set to: {lang.DisplayName} ({lang.Code})");
                return true;
            }
            Plugin.Logger.LogWarning($"Language code '{code}' not found in configuration.");
            return false;
        }

        /// <summary>
        /// Gets a language configuration by its code.
        /// </summary>
        public static LanguageConfig GetLanguageByCode(string code)
        {
            return Languages.FirstOrDefault(l => l.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets a language configuration by its custom language ID.
        /// </summary>
        public static LanguageConfig GetLanguageById(int id)
        {
            return Languages.FirstOrDefault(l => l.CustomLanguageId == id);
        }

        /// <summary>
        /// Checks if the given code belongs to any custom language.
        /// </summary>
        public static bool IsCustomLanguageCode(string code)
        {
            return Languages.Any(l => l.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Checks if the given ID belongs to any custom language.
        /// </summary>
        public static bool IsCustomLanguageId(int id)
        {
            return Languages.Any(l => l.CustomLanguageId == id);
        }

        // ============================================================
        // Compatibility properties for existing code
        // These delegate to ActiveLanguage for backward compatibility
        // ============================================================

        public static string LanguageCode => ActiveLanguage?.Code ?? "chn";
        public static string DisplayName => ActiveLanguage?.DisplayName ?? "中文";
        public static string TranslationFolder => ActiveLanguage?.TranslationFolder ?? "Chinese";
        public static string FileIdentifier => ActiveLanguage?.FileIdentifier ?? "chn";
        public static int CustomLanguageId => ActiveLanguage?.CustomLanguageId ?? BaseCustomLanguageId;
    }
}
