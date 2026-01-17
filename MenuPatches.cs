using BepInEx.Logging;
using GameWorld2;
using HarmonyLib;
using System;
using TingTing;

namespace TranslationPlugin
{
    /// <summary>
    /// Harmony patches for translating menu and UI strings.
    /// </summary>
    public class MenuPatches
    {
        private static ManualLogSource Logger => Plugin.Logger;

        /// <summary>
        /// Patch WorldSettings.Notify to translate notification messages.
        /// </summary>
        [HarmonyPatch(typeof(WorldSettings), "Notify")]
        [HarmonyPrefix]
        public static void Notify_Prefix(ref string pMessage)
        {
            Logger.LogInfo($"[MenuPatches] Notify_Prefix called with: '{pMessage}'");

            if (!IsCustomLanguageActive())
            {
                Logger.LogInfo("[MenuPatches] Custom language NOT active, skipping");
                return;
            }

            try
            {
                string translation = MenuTranslations.TranslateNotification(pMessage);
                Logger.LogInfo($"[MenuPatches] Notification translation lookup: '{pMessage}' -> '{translation}'");
                if (translation != null)
                {
                    pMessage = MenuTranslations.FormatBilingual(pMessage, translation);
                    Logger.LogInfo($"[MenuPatches] Notification result: '{pMessage}'");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in Notify_Prefix: {ex}");
            }
        }

        /// <summary>
        /// Patch MimanTing.Say to translate dialogue bubbles.
        /// </summary>
        [HarmonyPatch(typeof(MimanTing), "Say")]
        [HarmonyPrefix]
        public static void Say_Prefix(ref string pLine)
        {
            Logger.LogInfo($"[MenuPatches] Say_Prefix called with: '{pLine}'");

            if (!IsCustomLanguageActive())
            {
                Logger.LogInfo("[MenuPatches] Custom language NOT active, skipping");
                return;
            }
            if (string.IsNullOrEmpty(pLine)) return;

            try
            {
                string translation = MenuTranslations.TranslateDialogue(pLine);
                Logger.LogInfo($"[MenuPatches] Dialogue translation lookup: '{pLine}' -> '{translation}'");
                if (translation != null)
                {
                    pLine = MenuTranslations.FormatBilingual(pLine, translation);
                    Logger.LogInfo($"[MenuPatches] Dialogue result: '{pLine}'");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in Say_Prefix: {ex}");
            }
        }

        /// <summary>
        /// Patch Shell.ShowTooltip to translate tooltip text before it's displayed.
        /// This catches all tooltips, including interaction text and secondary text (e.g. hack/use).
        /// </summary>
        [HarmonyPatch(typeof(Shell), "ShowTooltip")]
        [HarmonyPrefix]
        public static void ShowTooltip_Prefix(Shell __instance, ref string pInteractionText, ref string pInteractionText2)
        {
            if (!IsCustomLanguageActive()) return;

            try
            {
                // Translate primary text (e.g., "open door")
                if (!string.IsNullOrEmpty(pInteractionText))
                {
                    string translation = MenuTranslations.TranslateComposedTooltip(pInteractionText);
                    if (translation != pInteractionText)
                    {
                        pInteractionText = translation;
                        Logger.LogInfo($"[MenuPatches] Tooltip translated: '{translation}'");
                    }
                }

                // Translate secondary text (e.g., "hack door")
                if (!string.IsNullOrEmpty(pInteractionText2))
                {
                    string translation2 = MenuTranslations.TranslateComposedTooltip(pInteractionText2);
                    if (translation2 != pInteractionText2)
                    {
                        pInteractionText2 = translation2;
                        Logger.LogInfo($"[MenuPatches] Tooltip secondary translated: '{translation2}'");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in ShowTooltip_Prefix: {ex}");
            }
        }

        /// <summary>
        /// Helper to translate items in the ActionMenu.
        /// </summary>
        private static void TranslateMenuItems(ActionMenu actionMenu)
        {
            if (actionMenu == null || actionMenu.items == null) return;

            Logger.LogInfo($"[MenuPatches] Translating {actionMenu.items.Length} menu items...");

            for (int i = 0; i < actionMenu.items.Length; i++)
            {
                var item = actionMenu.items[i];
                if (item == null || string.IsNullOrEmpty(item.text)) continue;

                string originalText = item.text;
                // Try translation
                string translation = MenuTranslations.TranslateMenuText(originalText);

                if (translation != null)
                {
                    item.text = MenuTranslations.FormatBilingual(originalText, translation);
                    Logger.LogInfo($"[MenuPatches] Menu item translated: '{originalText}' -> '{item.text}'");
                }
                else
                {
                    // If no direct menu text match, try composed tooltip logic for things like "open door"
                    // But "open door" is usually constructed from verb + tooltip, so it might be tricky to catch here
                    // if we don't have the components.
                    // However, we do have TranslateComposedTooltip in MenuTranslations which parses "verb description".
                    // Let's try that as a fallback if it looks like a composed string.
                    string composedTranslation = MenuTranslations.TranslateComposedTooltip(originalText);
                    if (composedTranslation != originalText) // different means it was translated
                    {
                        item.text = composedTranslation;
                         Logger.LogInfo($"[MenuPatches] Composed item translated: '{originalText}' -> '{item.text}'");
                    }
                }
            }
        }

        /// <summary>
        /// Patch PlayerRoamingState.BuildInteractionMenu to translate items after they are set.
        /// </summary>
        [HarmonyPatch(typeof(PlayerRoamingState), "BuildInteractionMenu")]
        [HarmonyPostfix]
        public static void BuildInteractionMenu_Postfix(PlayerRoamingState __instance)
        {
            if (!IsCustomLanguageActive()) return;

            try
            {
                // Access private field _actionMenu using Reflection/AccessTools
                // PlayerRoamingState defines: private ActionMenu _actionMenu;
                var field = AccessTools.Field(typeof(PlayerRoamingState), "_actionMenu");
                ActionMenu actionMenu = (ActionMenu)field.GetValue(__instance);

                TranslateMenuItems(actionMenu);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in BuildInteractionMenu_Postfix: {ex}");
            }
        }

        /// <summary>
        /// Patch PlayerRoamingState.BuildInventoryMenu to translate items after they are set.
        /// </summary>
        [HarmonyPatch(typeof(PlayerRoamingState), "BuildInventoryMenu")]
        [HarmonyPostfix]
        public static void BuildInventoryMenu_Postfix(PlayerRoamingState __instance)
        {
            if (!IsCustomLanguageActive()) return;

            try
            {
                var field = AccessTools.Field(typeof(PlayerRoamingState), "_actionMenu");
                ActionMenu actionMenu = (ActionMenu)field.GetValue(__instance);

                TranslateMenuItems(actionMenu);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in BuildInventoryMenu_Postfix: {ex}");
            }
        }

        /// <summary>
        /// Check if a custom language is currently active.
        /// </summary>
        private static bool IsCustomLanguageActive()
        {
            return TranslationConfig.ActiveLanguage != null;
        }
    }
}
