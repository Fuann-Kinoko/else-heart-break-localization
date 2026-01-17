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
    /// Uses a simple INI-like format for easy editing.
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

        private static bool _initialized = false;

        /// <summary>
        /// Initialize the menu translations from file.
        /// </summary>
        public static void Init()
        {
            if (_initialized) return;

            string gameRoot = Path.GetDirectoryName(Application.dataPath);
            string translationsPath = Path.Combine(gameRoot, "menu_translations.ini");

            Logger.LogInfo($"Looking for menu_translations.ini at: {translationsPath}");

            if (File.Exists(translationsPath))
            {
                LoadFromIni(translationsPath);
            }
            else
            {
                Logger.LogWarning($"menu_translations.ini not found. Creating default file.");
                CreateDefaultFile(translationsPath);
                LoadFromIni(translationsPath);
            }

            _initialized = true;
            Logger.LogInfo($"Menu translations loaded: {_tooltips.Count} tooltips, {_verbs.Count} verbs, {_notifications.Count} notifications, {_dialogues.Count} dialogues, {_menuText.Count} menuText");
        }

        private static void LoadFromIni(string path)
        {
            _tooltips.Clear();
            _verbs.Clear();
            _notifications.Clear();
            _dialogues.Clear();
            _dialogues.Clear();
            _menuText.Clear();
            _composedCache.Clear();

            string currentSection = null;

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
                        currentSection = line.Substring(1, line.Length - 2).ToLowerInvariant();
                        continue;
                    }

                    // Key=Value pair
                    int eqIndex = line.IndexOf('=');
                    if (eqIndex > 0)
                    {
                        string key = line.Substring(0, eqIndex).Trim();
                        string value = line.Substring(eqIndex + 1).Trim();

                        switch (currentSection)
                        {
                            case "tooltips":
                                _tooltips[key] = value;
                                break;
                            case "verbs":
                                _verbs[key] = value;
                                break;
                            case "notifications":
                                _notifications[key] = value;
                                break;
                            case "dialogues":
                                _dialogues[key] = value;
                                break;
                            case "menutext":
                                _menuText[key] = value;
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error loading menu_translations.ini: {ex.Message}");
            }
        }

        /// <summary>
        /// Translate a tooltip name (e.g., "door" -> "门").
        /// Returns null if no translation found.
        /// </summary>
        public static string TranslateTooltip(string original)
        {
            if (string.IsNullOrEmpty(original)) return null;
            bool found = _tooltips.TryGetValue(original, out var translation);
            Logger.LogDebug($"[MenuTranslations] TranslateTooltip('{original}') -> found={found}, result='{translation}'");
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
            Logger.LogDebug($"[MenuTranslations] TranslateVerb('{original}') -> found={found}, result='{translation}'");
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
            Logger.LogDebug($"[MenuTranslations] TranslateMenuText('{original}') -> found={found}, result='{translation}'");
            return found ? translation : null;
        }

        /// <summary>
        /// Format bilingual output: "original (翻译)"
        /// </summary>
        public static string FormatBilingual(string original, string translated)
        {
            if (string.IsNullOrEmpty(translated))
                return original;

            if (!Plugin.BilingualMode)
                return translated;

            return $"{original} ({translated})";
        }

        /// <summary>
        /// Translate a composed tooltip like "open door" by translating verb and tooltip separately.
        /// Returns "open door (开门)" format.
        /// </summary>
        public static string TranslateComposedTooltip(string composed)
        {
            if (string.IsNullOrEmpty(composed)) return composed;

            if (_composedCache.TryGetValue(composed, out var cachedResult))
                return cachedResult;

            // 1. Check for specific overrides in [MenuText] first (e.g. "talk to person" -> "与人交谈")
            // This allows disabling the generic "Verb + Noun" logic for specific cases.
            var overrideTranslation = TranslateMenuText(composed);
            if (overrideTranslation != null)
            {
                string result = FormatBilingual(composed, overrideTranslation);
                _composedCache[composed] = result;
                return result;
            }

            // 2. Try to find matching verb + tooltip pattern
            foreach (var verbKvp in _verbs)
            {
                string verb = verbKvp.Key;
                if (composed.StartsWith(verb + " ", StringComparison.OrdinalIgnoreCase))
                {
                    string remainder = composed.Substring(verb.Length + 1);
                    string tooltipTranslation = TranslateTooltip(remainder);

                    if (tooltipTranslation != null)
                    {
                        string translatedFull = verbKvp.Value + tooltipTranslation;
                        string result = FormatBilingual(composed, translatedFull);
                        _composedCache[composed] = result;
                        return result;
                    }
                }
            }

            // Fallback: try direct lookup
            var directTranslation = TranslateNotification(composed);
            if (directTranslation != null)
            {
                string result = FormatBilingual(composed, directTranslation);
                _composedCache[composed] = result;
                return result;
            }

            _composedCache[composed] = composed;
            return composed;
        }

        private static void CreateDefaultFile(string path)
        {
            try
            {
                string resourceName = "TranslationPlugin.assets.menu_translations.ini";
                string defaultContent = TranslationConfig.LoadResourceText(resourceName);

                File.WriteAllText(path, defaultContent, System.Text.Encoding.UTF8);
                Logger.LogInfo($"Created default menu_translations.ini at: {path}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to create menu_translations.ini: {ex.Message}");
            }
        }
    }
}
