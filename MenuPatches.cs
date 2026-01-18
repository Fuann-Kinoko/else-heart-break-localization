using BepInEx.Logging;
using GameWorld2;
using HarmonyLib;
using System;
using TingTing;
using UnityEngine;

namespace ElseHeartbreakLocalization;

public class MenuPatches
{
    private static ManualLogSource Logger => Plugin.Logger;
    private static bool IsActive => TranslationConfig.ActiveLanguage != null;

    [HarmonyPatch(typeof(WorldSettings), "Notify"), HarmonyPrefix]
    public static void Notify(ref string pMessage) =>
        ApplyTranslation(ref pMessage, MenuTranslations.TranslateNotification, "Notify");

    [HarmonyPatch(typeof(WorldSettings), "Hint"), HarmonyPrefix]
    public static void Hint(ref string pMessage) =>
        ApplyTranslation(ref pMessage, MenuTranslations.TranslateNotification, "Hint");

    [HarmonyPatch(typeof(MimanTing), "Say"), HarmonyPrefix]
    public static void Say(ref string pLine) =>
        ApplyTranslation(ref pLine, MenuTranslations.TranslateDialogue, "Dialogue");

    private static void ApplyTranslation(ref string text, Func<string, string> translator, string ctx)
    {
        if (!IsActive || string.IsNullOrEmpty(text)) return;
        try
        {
            var t = translator(text);
            if (t != null)
            {
                Logger.LogInfo($"[{ctx} Lookup] '{text}' -> '{t}'");
                text = MenuTranslations.FormatBilingual(text, t);
                Logger.LogInfo($"[{ctx} Result] '{text}'");
            }
        }
        catch (Exception ex) { Logger.LogError($"Error in {ctx}: {ex}"); }
    }

    [HarmonyPatch(typeof(Shell), "ShowTooltip"), HarmonyPrefix]
    public static void ShowTooltip(Shell __instance, ref string pInteractionText, ref string pInteractionText2)
    {
        if (!IsActive) return;
        try
        {
            TranslateAndApply(ref pInteractionText, "Tooltip");
            TranslateAndApply(ref pInteractionText2, "Tooltip2");
        }
        catch (Exception ex) { Logger.LogError($"Error in ShowTooltip: {ex}"); }

        void TranslateAndApply(ref string s, string ctx)
        {
            if (string.IsNullOrEmpty(s)) return;
            var t = MenuTranslations.TranslateComposedTooltip(s);
            if (t != s)
            {
                s = t;
                Logger.LogInfo($"[{ctx}] '{s}'");
            }
        }
    }

    [HarmonyPatch(typeof(PlayerRoamingState), "BuildInteractionMenu"), HarmonyPostfix]
    public static void BuildInteractionMenu(PlayerRoamingState __instance) => TranslateMenus(__instance);

    [HarmonyPatch(typeof(PlayerRoamingState), "BuildInventoryMenu"), HarmonyPostfix]
    public static void BuildInventoryMenu(PlayerRoamingState __instance) => TranslateMenus(__instance);

    private static void TranslateMenus(PlayerRoamingState instance)
    {
        if (!IsActive) return;
        try
        {
             var menu = AccessTools.Field(typeof(PlayerRoamingState), "_actionMenu").GetValue(instance) as ActionMenu;
             if (menu?.items == null) return;

             Logger.LogInfo($"Translating {menu.items.Length} items...");

             foreach(var item in menu.items)
             {
                 if (item == null || string.IsNullOrEmpty(item.text)) continue;

                 var orig = item.text;
                 if (MenuTranslations.TranslateMenuText(orig) is { } trans)
                 {
                     item.text = MenuTranslations.FormatBilingual(orig, trans);
                     Logger.LogInfo($"[Menu] '{orig}' -> '{item.text}'");
                 }
                 else
                 {
                     var comp = MenuTranslations.TranslateComposedTooltip(orig);
                     if (comp != orig)
                     {
                         item.text = comp;
                         Logger.LogInfo($"[Menu Composed] '{orig}' -> '{item.text}'");
                     }
                 }
             }
        }
        catch (Exception ex) { Logger.LogError($"Error menus: {ex}"); }
    }

    [HarmonyPatch(typeof(BubbleCanvasController), "CreateBubble"), HarmonyPostfix]
    public static void CreateBubble(Bubble __result, string pText)
    {
        if (!IsActive) return;
        var lang = TranslationConfig.ActiveLanguage;
        if (lang.CharacterWidthMultiplier <= 1.0f) return;

        try
        {
            float extra = 0f;
            float perChar = 7f * (lang.CharacterWidthMultiplier - 1.0f);
            foreach(char c in pText) if (c > 255) extra += perChar;

            if (extra > 0 && __result.GetComponent<RectTransform>() is { } rt)
                rt.sizeDelta += new Vector2(extra, 0);
        }
        catch (Exception ex) { Logger.LogError($"Error CreateBubble: {ex}"); }
    }
}
