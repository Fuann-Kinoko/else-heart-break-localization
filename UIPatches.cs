using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace ElseHeartbreakLocalization;

public class UIPatches
{
    private static ManualLogSource Logger => Plugin.Logger;
    private const string PrefKey = "EHTranslation_Language";

    [HarmonyPatch(typeof(MainMenu), "Start"), HarmonyPostfix]
    public static void Start_Postfix(MainMenu __instance)
    {
        RestorePreference(__instance);
        SetupLanguageButtons(__instance);
    }

    private static void RestorePreference(MainMenu menu)
    {
        if (!PlayerPrefs.HasKey(PrefKey)) return;

        var saved = PlayerPrefs.GetString(PrefKey);
        Logger.LogInfo($"Restoring language: {saved}");

        TranslationConfig.SetActiveLanguage(saved);
        MenuTranslations.Reload();

        // Always restore the language to the menu, even if it's "eng"
        menu.SetLanguage(saved);
    }

    private static void SetupLanguageButtons(MainMenu menu)
    {
        var root = GameObject.Find("LanguageButtons/Languages");
        if (root == null) return;

        // Hook existing
        HookButton(root, "English", "eng");
        HookButton(root, "Swedish", "swe");

        // Create new
        CreateCustomButtons(root.transform, menu);
    }

    private static void HookButton(GameObject root, string name, string code)
    {
        var t = root.transform.Find(name);
        if (t != null && t.GetComponent<Button>() is { } btn)
            btn.onClick.AddListener(() => SavePref(code));
    }

    private static void SavePref(string code)
    {
        Logger.LogInfo($"Selected: {code}");
        PlayerPrefs.SetString(PrefKey, code);
        PlayerPrefs.Save();
    }

    private static void CreateCustomButtons(Transform container, MainMenu menu)
    {
        var english = container.Find("English");
        var swedish = container.Find("Swedish");

        var template = english ?? swedish;
        if (template == null) { Logger.LogError("No template button"); return; }

        float spacing = (english != null && swedish != null)
            ? english.localPosition.y - swedish.localPosition.y
            : 35f;

        var startPos = (swedish ?? english).localPosition;

        for (int i = 0; i < TranslationConfig.Languages.Count; i++)
        {
            var lang = TranslationConfig.Languages[i];
            var btnName = $"CustomLanguage_{lang.Code}";
            if (container.Find(btnName)) continue;

            var btnObj = (GameObject)Object.Instantiate(template.gameObject);
            btnObj.name = btnName;
            btnObj.transform.SetParent(container, false);
            btnObj.transform.localScale = Vector3.one;
            btnObj.transform.localPosition = startPos - new Vector3(0, spacing * (i + 1), 0);

            SetButtonText(btnObj, lang.DisplayName);
            SetupButtonAction(btnObj, lang, menu);

            Logger.LogInfo($"Created button: {lang.DisplayName}");
        }

        // Adjust hint text
        var hint = container.Find("This can be changed later");
        if (hint != null)
            hint.localPosition -= new Vector3(0, spacing * TranslationConfig.Languages.Count, 0);
    }

    private static void SetButtonText(GameObject obj, string text)
    {
        if (obj.GetComponentInChildren<Text>() is { } t) t.text = text;
        else if (obj.transform.Find("Text")?.GetComponent<Text>() is { } t2) t2.text = text;
    }

    private static void SetupButtonAction(GameObject obj, LanguageConfig lang, MainMenu menu)
    {
        var btn = obj.GetComponent<Button>();
        if (btn == null) return;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            SavePref(lang.Code);
            TranslationConfig.SetActiveLanguage(lang.Code);

            if (menu.controls == null) menu.NewGame(lang.Code);
            else menu.SetLanguage(lang.Code);
        });
    }
}
