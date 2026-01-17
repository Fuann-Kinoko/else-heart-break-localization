using BepInEx.Logging;
using HarmonyLib;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TranslationPlugin
{
    public class UIPatches
    {
        private static ManualLogSource Logger => Plugin.Logger;
        private const string PrefKey = "EHTranslation_Language";

        [HarmonyPatch(typeof(MainMenu), "Start")]
        [HarmonyPostfix]
        public static void Start_Postfix(MainMenu __instance)
        {
            // Restore Language from Prefs
            if (PlayerPrefs.HasKey(PrefKey))
            {
                string savedLang = PlayerPrefs.GetString(PrefKey);
                Logger.LogInfo($"Restoring saved language: {savedLang}");

                // Check if it's a custom language and set it as active
                if (TranslationConfig.IsCustomLanguageCode(savedLang))
                {
                    TranslationConfig.SetActiveLanguage(savedLang);
                }

                __instance.SetLanguage(savedLang);
            }

            var languageButtonsRoot = GameObject.Find("LanguageButtons");
            if (languageButtonsRoot == null) return;

            var languagesContainer = languageButtonsRoot.transform.Find("Languages");
            if (languagesContainer == null) return;

            // Hook existing language buttons to save preference
            var englishBtn = languagesContainer.Find("English");
            if (englishBtn != null)
            {
                var btn = englishBtn.GetComponent<Button>();
                btn.onClick.AddListener(() => {
                    Logger.LogInfo("English selected - Saving preference.");
                    PlayerPrefs.SetString(PrefKey, "eng");
                    PlayerPrefs.Save();
                });
            }

            var swedishBtn = languagesContainer.Find("Swedish");
            if (swedishBtn != null)
            {
                var btn = swedishBtn.GetComponent<Button>();
                btn.onClick.AddListener(() => {
                    Logger.LogInfo("Swedish selected - Saving preference.");
                    PlayerPrefs.SetString(PrefKey, "swe");
                    PlayerPrefs.Save();
                });
            }

            // Create buttons for all custom languages
            CreateCustomLanguageButtons(languagesContainer, __instance);
        }

        /// <summary>
        /// Creates UI buttons for all custom languages defined in the configuration.
        /// </summary>
        private static void CreateCustomLanguageButtons(Transform languagesContainer, MainMenu mainMenu)
        {
            // Find a template button (English button is preferred as it calls NewGame("eng"))
            var englishBtn = languagesContainer.Find("English");
            var swedishBtn = languagesContainer.Find("Swedish");
            Transform template = englishBtn ?? swedishBtn;

            if (template == null)
            {
                Logger.LogError("No template button found for creating custom language buttons.");
                return;
            }

            // Calculate spacing between buttons
            float buttonSpacing = 35f; // Default spacing
            if (englishBtn != null && swedishBtn != null)
            {
                buttonSpacing = englishBtn.localPosition.y - swedishBtn.localPosition.y;
            }

            // Get the starting position (above the top-most existing button)
            Vector3 basePosition = template.localPosition;

            // Create a button for each custom language
            for (int i = 0; i < TranslationConfig.Languages.Count; i++)
            {
                var langConfig = TranslationConfig.Languages[i];
                string buttonName = $"CustomLanguage_{langConfig.Code}";

                // Skip if button already exists
                if (languagesContainer.Find(buttonName) != null)
                {
                    Logger.LogDebug($"Button for {langConfig.Code} already exists, skipping.");
                    continue;
                }

                // Create the button by cloning the template
                // The template button (English/Swedish) already has onClick setup to call NewGame(lang)
                GameObject newButtonObj = (GameObject)Object.Instantiate(template.gameObject);
                newButtonObj.transform.SetParent(languagesContainer, false);
                newButtonObj.transform.localScale = Vector3.one;
                newButtonObj.name = buttonName;

                // Position the button above existing buttons
                Vector3 newPos = basePosition;
                newPos.y += buttonSpacing * (i + 1);
                newButtonObj.transform.localPosition = newPos;

                // Update the button text
                var textComp = newButtonObj.GetComponentInChildren<Text>();
                if (textComp != null)
                {
                    textComp.text = langConfig.DisplayName;
                }
                else
                {
                    var textTransform = newButtonObj.transform.Find("Text");
                    if (textTransform != null)
                    {
                        textTransform.GetComponent<Text>().text = langConfig.DisplayName;
                    }
                }

                // Setup button click handler
                // IMPORTANT: We need to REPLACE the onClick listeners because the cloned button
                // still references the original language (eng/swe). We replicate the same behavior
                // but with our custom language code.
                var buttonComp = newButtonObj.GetComponent<Button>();
                if (buttonComp != null)
                {
                    buttonComp.onClick.RemoveAllListeners();

                    // Capture the language config in closure
                    var capturedLang = langConfig;
                    buttonComp.onClick.AddListener(() =>
                    {
                        Logger.LogInfo($"Custom language '{capturedLang.DisplayName}' ({capturedLang.Code}) selected.");

                        // Set this as the active language in config
                        TranslationConfig.SetActiveLanguage(capturedLang.Code);

                        // Save preference
                        PlayerPrefs.SetString(PrefKey, capturedLang.Code);
                        PlayerPrefs.Save();

                        // Check if we're in the main menu (before game starts) or in-game pause menu
                        if (mainMenu.controls == null)
                        {
                            // Main menu - start new game with this language
                            Logger.LogInfo($"Starting new game with language: {capturedLang.Code}");
                            mainMenu.NewGame(capturedLang.Code);
                        }
                        else
                        {
                            // In-game pause menu - just switch language
                            Logger.LogInfo($"Switching in-game language to: {capturedLang.Code}");
                            mainMenu.SetLanguage(capturedLang.Code);
                        }
                    });
                }

                Logger.LogInfo($"Created language button: {langConfig.DisplayName} ({langConfig.Code})");
            }
        }
    }
}
