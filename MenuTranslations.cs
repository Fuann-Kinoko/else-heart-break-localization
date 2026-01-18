using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace TranslationPlugin
{
    /// <summary>
    /// Handles loading and lookup of menu/UI string translations.
    /// Now supports multiple languages and .mtf file format.
    /// </summary>
    public static class MenuTranslations
    {
        private static ManualLogSource Logger => Plugin.Logger;

        // Translation dictionaries
        private static Dictionary<string, string> _tooltips = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, string> _verbs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, string> _notifications = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, string> _dialogues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, string> _menuText = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Cache for composed tooltips to avoid repeated string processing
        private static Dictionary<string, string> _composedCache = new Dictionary<string, string>();

        // Regex from game's Translator.cs
        private static Regex _mtfRegex = new Regex("\"(.+)\" => \"(.+)\"");

        /// <summary>
        /// Initialize the menu translations based on active language.
        /// </summary>
        public static void Init()
        {
            // Always reload if language changed or first init
            LoadTranslations(TranslationConfig.ActiveLanguage);
        }

        public static void Reload()
        {
            LoadTranslations(TranslationConfig.ActiveLanguage);
        }

        private static void LoadTranslations(LanguageConfig language)
        {
            _tooltips.Clear();
            _verbs.Clear();
            _notifications.Clear();
            _dialogues.Clear();
            _menuText.Clear();
            _composedCache.Clear();

            if (language == null) return;

            // Path: ElseHeartbreak_Data/InitData/MenuTranslations/{Language}/
            // Separate from game's Translations folder to avoid Translator scanning conflicts
            string initData = Path.Combine(Application.dataPath, "InitData");
            string menuTranslationsDir = Path.Combine(initData, "MenuTranslations");
            string menuPath = Path.Combine(menuTranslationsDir, language.TranslationFolder);

            Logger.LogInfo($"Loading menu translations from: {menuPath}");

            if (!Directory.Exists(menuPath))
            {
                Logger.LogWarning($"Menu translation directory not found: {menuPath}");
                return;
            }

            // Load specific files
            LoadMtfFile(Path.Combine(menuPath, "tooltips.mtf"), _tooltips);
            LoadMtfFile(Path.Combine(menuPath, "verbs.mtf"), _verbs);
            LoadMtfFile(Path.Combine(menuPath, "notifications.mtf"), _notifications); // Contains general notifications
            LoadMtfFile(Path.Combine(menuPath, "dialogues.mtf"), _dialogues);
            LoadMtfFilesWithOverride(Path.Combine(menuPath, "menutext.mtf"), _menuText);

            // Load extra files (mapped to appropriate dictionaries)
            LoadMtfFile(Path.Combine(menuPath, "liquidtypes.mtf"), _tooltips); // Liquids are noun tooltips
            LoadMtfFile(Path.Combine(menuPath, "drugtypes.mtf"), _tooltips);   // Drugs are noun tooltips
            LoadMtfFile(Path.Combine(menuPath, "swedishdialogues.mtf"), _dialogues); // Dialogues
            LoadMtfFile(Path.Combine(menuPath, "errors.mtf"), _notifications); // Errors are notifications
            LoadMtfFile(Path.Combine(menuPath, "actiondescriptions.mtf"), _menuText); // Action descriptions act like menu text patterns

            Logger.LogInfo($"Menu translations loaded for {language.Code}: {_tooltips.Count} tooltips, {_verbs.Count} verbs, {_notifications.Count} notifications, {_dialogues.Count} dialogues, {_menuText.Count} menuText");
        }

        private static void LoadMtfFile(string path, Dictionary<string, string> targetDict)
        {
            if (!File.Exists(path)) return;

            try
            {
                string[] lines = File.ReadAllLines(path);
                int count = 0;

                foreach (string line in lines)
                {
                    Match match = _mtfRegex.Match(line);
                    if (match.Success)
                    {
                        string key = match.Groups[1].Value;
                        string value = match.Groups[2].Value;
                        targetDict[key] = value;
                        count++;
                    }
                }
                // Logger.LogDebug($"Loaded {count} entries from {Path.GetFileName(path)}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error loading {path}: {ex.Message}");
            }
        }

        /// <summary>
        /// Load MTF file with optional override support.
        /// If xxx_override.mtf exists, it will be loaded first (base file overwrites override for same keys,
        /// but override keys not in base remain as full-sentence overrides).
        /// </summary>
        private static void LoadMtfWithOverride(string basePath, Dictionary<string, string> targetDict)
        {
            string dir = Path.GetDirectoryName(basePath);
            string baseName = Path.GetFileNameWithoutExtension(basePath);
            string overridePath = Path.Combine(dir, baseName + "_override.mtf");

            // Load override first - these are full-sentence overrides that take precedence
            LoadMtfFile(overridePath, targetDict);
            // Then load base file - individual translations that DON'T overwrite override entries
            // Actually, we want override to take precedence, so load base first, then override
            // Correction: Load base first, then override (override wins for duplicate keys)
        }

        /// <summary>
        /// Load MTF file with override support - override file takes precedence.
        /// </summary>
        private static void LoadMtfFilesWithOverride(string basePath, Dictionary<string, string> targetDict)
        {
            string dir = Path.GetDirectoryName(basePath);
            string baseName = Path.GetFileNameWithoutExtension(basePath);
            string overridePath = Path.Combine(dir, baseName + "_override.mtf");

            // Load base first
            LoadMtfFile(basePath, targetDict);
            // Then load override (override wins for duplicate keys)
            LoadMtfFile(overridePath, targetDict);
        }

        /// <summary>
        /// Translate a tooltip name (e.g., "door" -> "门").
        /// Returns null if no translation found.
        /// </summary>
        public static string TranslateTooltip(string original)
        {
            if (string.IsNullOrEmpty(original)) return null;
            bool found = _tooltips.TryGetValue(original, out var translation);
            return found ? translation : null;
        }

        /// <summary>
        /// Translate a verb (e.g., "open" -> "开").
        /// Returns null if no translation found.
        /// </summary>
        public static string TranslateVerb(string original)
        {
            if (string.IsNullOrEmpty(original)) return null;
            bool found = _verbs.TryGetValue(original, out var translation);
            return found ? translation : null;
        }

        /// <summary>
        /// Translate a notification message.
        /// Returns null if no translation found.
        /// </summary>
        public static string TranslateNotification(string original)
        {
            if (string.IsNullOrEmpty(original)) return null;
            return _notifications.TryGetValue(original, out var translation) ? translation : null;
        }

        /// <summary>
        /// Translate a dialogue line (for Say()).
        /// Returns null if no translation found.
        /// </summary>
        public static string TranslateDialogue(string original)
        {
            if (string.IsNullOrEmpty(original)) return null;
            return _dialogues.TryGetValue(original, out var translation) ? translation : null;
        }

        /// <summary>
        /// Translate a menu text string (e.g., "open bag" -> "打开背包").
        /// Returns null if no translation found.
        /// </summary>
        public static string TranslateMenuText(string original)
        {
            if (string.IsNullOrEmpty(original)) return null;
            bool found = _menuText.TryGetValue(original, out var translation);
            return found ? translation : null;
        }

        /// <summary>
        /// Format bilingual output: "original [翻译]"
        /// </summary>
        public static string FormatBilingual(string original, string translated)
        {
            if (string.IsNullOrEmpty(translated))
                return original;

            if (!Plugin.BilingualMode)
                return translated;

            return $"{original} [{translated}]";
        }

        /// <summary>
        /// Translate a composed tooltip like "open door" by translating verb and tooltip separately.
        /// Also handles preposition patterns like "turn water on sink" → "打开水槽水龙头"
        /// Returns bilingual format based on settings.
        /// </summary>
        public static string TranslateComposedTooltip(string composed)
        {
            if (string.IsNullOrEmpty(composed)) return composed;

            // Cache stores only the translation, not the formatted result
            // This allows BilingualMode to be toggled at runtime
            if (_composedCache.TryGetValue(composed, out var cachedTranslation))
            {
                if (cachedTranslation == null) return composed; // No translation found (cached null)
                return FormatBilingual(composed, cachedTranslation);
            }

            // 1. Check for specific overrides in [MenuText] first (e.g. "talk to person" -> "与人交谈")
            var overrideTranslation = TranslateMenuText(composed);
            if (overrideTranslation != null)
            {
                _composedCache[composed] = overrideTranslation;
                return FormatBilingual(composed, overrideTranslation);
            }

            // 2. Try preposition pattern: "VERB NOUN PREP OBJECT" -> "VERB OBJECT NOUN"
            // Examples: "turn water on sink" -> "打开水槽水龙头", "press button on jukebox" -> "按下点唱机按钮"
            var prepResult = TryTranslateWithPreposition(composed);
            if (prepResult != null)
            {
                _composedCache[composed] = prepResult;
                return FormatBilingual(composed, prepResult);
            }

            // 3. Try to find matching verb + tooltip pattern (simple case)
            // Sort verbs by length descending to ensure "turn on" is matched before "turn"
            var sortedVerbs = new List<string>(_verbs.Keys);
            sortedVerbs.Sort((a, b) => b.Length.CompareTo(a.Length));

            foreach (var verb in sortedVerbs)
            {
                if (composed.StartsWith(verb + " ", StringComparison.OrdinalIgnoreCase))
                {
                    string translation = _verbs[verb];
                    string remainder = composed.Substring(verb.Length + 1);

                    // Handle suffix in nouns, e.g., "booze (100%)" or "booze (empty)"
                    string suffixTranslated = "";
                    // Match percentage like (100%) or (50.5%) OR text like (empty)
                    var suffixMatch = Regex.Match(remainder, @"\s*\((\d+(?:\.\d+)?%|[a-zA-Z]+)\)$");
                    string nounBase = remainder;
                    if (suffixMatch.Success)
                    {
                        string suffixContent = suffixMatch.Groups[1].Value;
                        // Try to translate the suffix content (e.g., "empty" -> "空")
                        var suffixTrans = TranslateTooltip(suffixContent);
                        suffixTranslated = " (" + (suffixTrans ?? suffixContent) + ")";
                        nounBase = remainder.Substring(0, suffixMatch.Index).Trim();
                    }

                    string nounTranslation = TranslateTooltip(nounBase);

                    if (nounTranslation != null)
                    {
                        // Both verb and noun translated
                        string translatedFull = "";
                        if (translation.Contains("[N]"))
                        {
                            translatedFull = translation.Replace("[N]", nounTranslation) + suffixTranslated;
                        }
                        else
                        {
                            translatedFull = translation + nounTranslation + suffixTranslated;
                        }
                        _composedCache[composed] = translatedFull;
                        return FormatBilingual(composed, translatedFull);
                    }
                    else if (string.IsNullOrEmpty(nounBase) || nounBase.Trim().Length == 0)
                    {
                        // Verb-only translation (e.g., "sit down" with no noun)
                        _composedCache[composed] = translation;
                        return FormatBilingual(composed.Trim(), translation);
                    }
                    else
                    {
                        // Verb translated, Noun NOT translated
                        Logger.LogWarning($"[MenuTranslations] Missing translation for noun: '{nounBase}' in composed: '{composed}'");

                        string mixedTranslation = "";
                        if (translation.Contains("[N]"))
                        {
                             mixedTranslation = translation.Replace("[N]", nounBase) + suffixTranslated;
                        }
                        else
                        {
                             mixedTranslation = translation + nounBase + suffixTranslated;
                        }
                        _composedCache[composed] = mixedTranslation;
                        return FormatBilingual(composed, mixedTranslation);
                    }
                }
            }

            // Fallback 1: try direct lookup in verbs (for complete verb phrases like "check balance")
            var verbTranslation = TranslateVerb(composed);
            if (verbTranslation != null)
            {
                _composedCache[composed] = verbTranslation;
                return FormatBilingual(composed, verbTranslation);
            }

            // Fallback 2: try direct lookup in tooltips (for noun-only items)
            var tooltipTranslation = TranslateTooltip(composed);
            if (tooltipTranslation != null)
            {
                _composedCache[composed] = tooltipTranslation;
                return FormatBilingual(composed, tooltipTranslation);
            }

            // Fallback 3: try direct lookup in notifications (sometimes used for full sentences)
            var directTranslation = TranslateNotification(composed);
            if (directTranslation != null)
            {
                _composedCache[composed] = directTranslation;
                return FormatBilingual(composed, directTranslation);
            }

            // No translation found - cache null to avoid repeated lookups
            _composedCache[composed] = null;
            return composed;
        }

        /// <summary>
        /// Common prepositions in game text that indicate word order should be reversed.
        /// Pattern: "VERB NOUN PREP OBJECT" → Chinese: "VERB翻译 + OBJECT翻译 + NOUN翻译"
        /// </summary>
        private static readonly string[] _prepositions = { " on ", " in ", " at ", " from ", " into " };

        /// <summary>
        /// Try to translate using preposition pattern.
        /// E.g., "turn water on sink" -> verb="turn", noun="water", prep="on", object="sink"
        ///
        /// First tries to find "turn water" as a complete verb phrase in verbs.mtf
        /// If found: uses that translation + object translation
        /// If not: uses verb translation + object translation + noun translation (reordered)
        /// </summary>
        private static string TryTranslateWithPreposition(string composed)
        {
            // First, find which preposition is used (if any)
            string foundPrep = null;
            int prepIndex = -1;

            foreach (var prep in _prepositions)
            {
                int idx = composed.IndexOf(prep, StringComparison.OrdinalIgnoreCase);
                if (idx > 0)
                {
                    // Found a preposition, prefer the last one to handle cases like "turn on water in sink"
                    if (prepIndex < 0 || idx > prepIndex)
                    {
                        foundPrep = prep;
                        prepIndex = idx;
                    }
                }
            }

            if (foundPrep == null) return null;

            // Split: "turn on water in sink" -> beforePrep="turn on water", afterPrep="sink"
            string beforePrep = composed.Substring(0, prepIndex).Trim();
            string objectPart = composed.Substring(prepIndex + foundPrep.Length).Trim();

            // Check if beforePrep (e.g., "turn on water in" without the prep) has a translation
            // The translation may contain [N] placeholder for the object position
            // E.g., "turn on water in" => "打开[N]水龙头"
            // With sink -> "水槽", result: "打开水槽水龙头"

            // First, try the full beforePrep + prep pattern (without trailing space issues)
            string verbWithPrep = beforePrep + foundPrep.TrimEnd();
            var patternTranslation = TranslateVerb(verbWithPrep);

            if (patternTranslation == null)
            {
                // Try just beforePrep
                patternTranslation = TranslateVerb(beforePrep);
            }

            if (patternTranslation != null)
            {
                string objectTranslation = TranslateTooltip(objectPart) ?? objectPart;

                // Check for [N] placeholder
                if (patternTranslation.Contains("[N]"))
                {
                    return patternTranslation.Replace("[N]", objectTranslation);
                }
                else
                {
                    // No placeholder, append object at end
                    return patternTranslation + objectTranslation;
                }
            }

            // Fallback: try to find a base verb and handle separately
            var sortedVerbs = new List<string>(_verbs.Keys);
            sortedVerbs.Sort((a, b) => b.Length.CompareTo(a.Length));

            foreach (var verb in sortedVerbs)
            {
                if (beforePrep.StartsWith(verb + " ", StringComparison.OrdinalIgnoreCase))
                {
                    string verbTranslation = _verbs[verb];
                    string nounSuffix = beforePrep.Substring(verb.Length + 1).Trim();
                    string nounSuffixTranslation = TranslateTooltip(nounSuffix) ?? nounSuffix;
                    string objectTranslation = TranslateTooltip(objectPart) ?? objectPart;

                    // Check for [N] placeholder in verb translation
                    if (verbTranslation.Contains("[N]"))
                    {
                        return verbTranslation.Replace("[N]", objectTranslation) + nounSuffixTranslation;
                    }
                    else
                    {
                        // Default Chinese order: verb + object + noun-suffix
                        return verbTranslation + objectTranslation + nounSuffixTranslation;
                    }
                }
                else if (beforePrep.Equals(verb, StringComparison.OrdinalIgnoreCase))
                {
                    string verbTranslation = _verbs[verb];
                    string objectTranslation = TranslateTooltip(objectPart) ?? objectPart;

                    if (verbTranslation.Contains("[N]"))
                    {
                        return verbTranslation.Replace("[N]", objectTranslation);
                    }
                    else
                    {
                        return verbTranslation + objectTranslation;
                    }
                }
            }

            return null;
        }
    }
}
