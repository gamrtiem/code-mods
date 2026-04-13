using BepInEx;

namespace AssetEditor
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class AssetEditor : BaseUnityPlugin
    {
        private const string PluginGUID = PluginAuthor + "." + PluginName;
        private const string PluginAuthor = "icebro";
        private const string PluginName = "AssetEditor";
        private const string PluginVersion = "1.0.0";
        
        public void Awake()
        {
            Log.Init(Logger);
            
            Edits.addEdits();
        }
    }
}
