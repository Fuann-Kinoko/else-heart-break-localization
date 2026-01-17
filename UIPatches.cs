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

        [HarmonyPatch(typeof(MainMenu), "Start")]
        [HarmonyPostfix]
        public static void Start_Postfix(MainMenu __instance)
        {
            // Only inject if in the correct scene or context
            // MainMenu.Start() is called in "StartScene" or "FullGameStartScene" usually.

            // Try to find the Language buttons container
            // Path based on screenshot: Canvas/LanguageButtons/Languages
            // We should find "Canvas" separately or search by name.

            // Since there might be multiple Canvases, find the one with MainMenuButtons or LanguageButtons
            var languageButtonsRoot = GameObject.Find("LanguageButtons");
            if (languageButtonsRoot == null)
            {
                Logger.LogWarning("Could not find 'LanguageButtons' GameObject.");
                return;
            }

            var languagesContainer = languageButtonsRoot.transform.Find("Languages");
            if (languagesContainer == null)
            {
                Logger.LogWarning("Could not find 'Languages' container.");
                return;
            }

            // Check if we already injected
            if (languagesContainer.Find("CustomLanguage") != null)
            {
                return;
            }

            // Find a template button (e.g., English)
            var template = languagesContainer.Find("English");
            if (template == null)
            {
                // Fallback to Swedish
                template = languagesContainer.Find("Swedish");
            }

            if (template == null)
            {
                Logger.LogWarning("Could not find 'English' or 'Swedish' button to use as template.");
                return;
            }

            // Instantiate the new button (Unity 4 compatible)
            GameObject newButtonObj = (GameObject)Object.Instantiate(template.gameObject);
            newButtonObj.transform.parent = languagesContainer;
            newButtonObj.transform.localScale = Vector3.one;
            newButtonObj.name = "CustomLanguage";

            // Calculate position offset (if not using LayoutGroup)
            // Assuming vertical list: Swedish -> English -> Custom
            var swedish = languagesContainer.Find("Swedish");
            if (swedish != null && template != null)
            {
                float gap = template.localPosition.y - swedish.localPosition.y;
                // Apply gap to new button relative to template (English)
                // Note: If gap is 0 or they are same pos, this might not work, but assuming list structure.
                // It looks like Swedish is TOP, English is BELOW. Gap should be negative.
                Vector3 newPos = template.localPosition;
                newPos.y += gap;
                newButtonObj.transform.localPosition = newPos;
            }
            else
            {
                 // Fallback specific offset if we can't calc
                 Vector3 newPos = template.localPosition;
                 newPos.y -= 35f; // Approx height
                 newButtonObj.transform.localPosition = newPos;
            }

            // Update Text
            var textComp = newButtonObj.GetComponentInChildren<Text>();
            if (textComp != null)
            {
                textComp.text = "中文";
            }
            else
            {
                // Try finding child named "Text" specifically if GetComponentInChildren fails or grabs wrong one
                var textTransform = newButtonObj.transform.Find("Text");
                if (textTransform != null)
                    textTransform.GetComponent<Text>().text = "中文";
            }

            // Update Button Logic
            var buttonComp = newButtonObj.GetComponent<Button>();
            if (buttonComp != null)
            {
                // Remove old listeners (UnityEvent)
                buttonComp.onClick.RemoveAllListeners();

                // Add new listener
                buttonComp.onClick.AddListener(() =>
                {
                    Logger.LogInfo("Custom Language Button Clicked");

                    // Logic:
                    // 1. Set Global Toggle TRUE (so it sticks)
                    Plugin.CustomLanguageActive = true;

                    // 2. Call MainMenu.SetLanguage
                    // Note: MainMenu.SetLanguage takes a string code.
                    __instance.SetLanguage(TranslationConfig.LanguageCode.Value);
                });
            }

            // Adjust position/order?
            // If it's a LayoutGroup, it auto-arranges.
            // If manual, we might need to shift it.
            // Assuming LayoutGroup or vertical list for now based on screenshot structure.
            newButtonObj.transform.SetSiblingIndex(template.GetSiblingIndex() + 1);

            Logger.LogInfo("Successfully injected Custom Language button.");
        }
    }
}
