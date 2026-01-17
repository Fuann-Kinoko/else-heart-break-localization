using BepInEx;
using BepInEx.Logging;

namespace TranslationPlugin;

[BepInPlugin("org.bepinex.plugins.EHTranslationPlugin", "Translation Plugin", "1.0.0.0")]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    private void Awake()
    {
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }
}
