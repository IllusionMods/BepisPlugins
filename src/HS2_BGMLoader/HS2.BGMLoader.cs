using BepInEx;
using BepisPlugins;
using XUnity.ResourceRedirector;

namespace BGMLoader
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInDependency(XUnity.ResourceRedirector.Constants.PluginData.Identifier)]
    public partial class BGMLoader : BaseUnityPlugin
    {
        private static bool TryGetOverrideFileName(IAssetLoadingContext context, out string overrideFileName)
        {
            overrideFileName = null;
            //hs2_bgm_10 or hs2a_bgm_00 or ai_bgm_10
            var isBgm = context.Parameters.Name.Length == 10 && context.Parameters.Name.StartsWith("hs2_bgm", System.StringComparison.InvariantCultureIgnoreCase) ||
            context.Parameters.Name.Length == 11 && context.Parameters.Name.StartsWith("hs2a_bgm", System.StringComparison.InvariantCultureIgnoreCase) ||
            context.Parameters.Name.Length == 9 && context.Parameters.Name.StartsWith("ai_bgm", System.StringComparison.InvariantCultureIgnoreCase);
            if (!isBgm) return false;

            var parts = context.Parameters.Name.Split('_');
            if (parts.Length != 3) return false;
            //AIBGM10.ogg or HS2ABGM00.ogg or HS2BGM00.ogg
            overrideFileName = $"{parts[0].ToUpper()}{parts[1].ToUpper()}{int.Parse(parts[2]):00}.ogg";
            return true;
        }
    }
}
