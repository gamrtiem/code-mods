using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BNR.patches;
using GoldenCoastPlusRevived.Items;
using HarmonyLib;
//using MiscModpackUtils;
using R2API;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace BNR
{
    [BepInDependency(ItemAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    public class butterscotchnroses : BaseUnityPlugin
    {
        public const string PluginGUID = "zzz" + PluginAuthor + "." + PluginName;

        public const string PluginAuthor = "icebro";
        public const string PluginName = "BNR";
        public const string PluginVersion = "1.0.0";

        public void Awake()
        {
            //TODO add making inferno + ESBM config not give them double jumps TT 
            //TODO add mod options button (uses something different i think idk( and highlighted text color change configfs 
            //TODO cleanesthud color force instead of survivor color 
            //TODO main menu pink color option like wolfo qol 
            Log.Init(Logger);
            Logger.LogDebug("loading mod !!");
            
            configs();

            Harmony harmony = new(Info.Metadata.GUID);
            //look into items like how that works instead of individual
            var patches = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(PatchBase)));
            foreach (Type patch in patches)
            {
                PatchBase patchBase = (PatchBase)Activator.CreateInstance(patch);

                try
                {
                    patchBase.Config(Config);
                    patchBase.Init(harmony);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                    throw;
                }
            }
            
            //make money/lunar text smaller why isnt it smaller it makes me go grrrrrrrrrrrrrrrrrrrrrrrr 
            RoR2.UI.HUD.onHudTargetChangedGlobal += (self) =>
            {
                GameObject upperLeft = GameObject.Find("UpperLeftCluster");

                if (!upperLeft) return;

                upperLeft.GetComponent<RectTransform>().offsetMin = new Vector2(0, -64);
                upperLeft.GetComponent<VerticalLayoutGroup>().spacing = 0;
                
                GameObject dollar = GameObject.Find("DollarSign");
                if (dollar)
                {
                    dollar.transform.localPosition = new Vector3(4, dollar.transform.localPosition.y, dollar.transform.localPosition.z);
                }
                
                GameObject buildLabel = GameObject.Find("SteamBuildLabel");
                if (buildLabel)
                {
                    buildLabel.gameObject.SetActive(false);
                }
            };
            
            //try to fix misc modpackutils logo not showing up when i boot into main profile (dunno why it happens ,.,.
            /*On.RoR2.UI.MainMenu.MainMenuController.Awake += (orig, controller) =>
            {
                orig(controller);
                
                if (!MiscModpackUtils.Patches.MainScreen.Override.Value) return;
                
                GameObject obj = GameObject.Find("LogoImage");
                if (obj == null) return;
                obj.transform.localPosition = new Vector3(0, 20, 0);
                obj.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                obj.GetComponent<Image>().sprite = Utils.Load(Path.Combine(Paths.ConfigPath, "logo.png"));
                Log.Debug("Changed Logo Image");
            };*/
            
            //this doesnt work fix later 
           On.RoR2.UI.SkinControllers.ButtonSkinController.Awake += (orig, self) =>
            {
                orig(self);
                
                HGButton hgButton = self.gameObject.GetComponent<HGButton>();
                if (!hgButton || hgButton.name.Contains("SurvivorIcon")) return;
                if (hgButton.transform.parent.gameObject.name.Contains("Choice")) return; //difficulty icons
                if (hgButton.transform.parent.gameObject.name.Contains("Loadout")) return; //css 
                if (hgButton.gameObject.name.Contains("Music&More"))
                {
                    hgButton.gameObject.SetActive(false); // sorry chris chris please dont kill me !!
                }
                
                ColorBlock colorVar = hgButton.colors;
                colorVar.normalColor = buttonNorm.Value;
                colorVar.highlightedColor = buttonHigh.Value;
                colorVar.pressedColor = buttonPress.Value;
                colorVar.selectedColor = buttonSelect.Value;
                hgButton.colors = colorVar;
            };
           
            On.RoR2.UI.SkinControllers.PanelSkinController.Awake += (orig, self) =>
            {
                orig(self);
                
                Image image = self.gameObject.GetComponent<Image>();
                if (!image || image.gameObject.name.Contains("RuleBook")) return;

                image.color = new Color(buttonNorm.Value.r, buttonNorm.Value.g, buttonNorm.Value.b, image.color.a);
            };
        }
        
        public static ConfigEntry<Color> buttonNorm;
        public static ConfigEntry<Color> buttonHigh;
        public static ConfigEntry<Color> buttonPress;
        public static ConfigEntry<Color> buttonSelect;
        
        public void configs()
        {
            buttonNorm = Config.Bind("BNR",
                "normal button color",
                new Color(110/255f, 83/255f, 120/255f, 255/255f),
                ""); 
            ModSettingsManager.AddOption(new ColorOption(buttonNorm));
            
            buttonHigh = Config.Bind("BNR",
                "highlighted button coor",
                new Color(255/255f, 177/255f, 245/255f, 255/255f),
                ""); 
            ModSettingsManager.AddOption(new ColorOption(buttonHigh));
            
            buttonPress = Config.Bind("BNR",
                "pressed button color",
                new Color(192/255f, 113/255f, 182/255f, 255/255f),
                ""); 
            ModSettingsManager.AddOption(new ColorOption(buttonPress));
            
            buttonSelect = Config.Bind("BNR",
                "selected button color",
                new Color(255/255f, 177/255f, 238/255f, 255/255f),
                ""); 
            ModSettingsManager.AddOption(new ColorOption(buttonSelect));
        }
    }
}