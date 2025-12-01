using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BNR;
using BNR.patches;
using GoldenCoastPlusRevived.Items;
using HarmonyLib;
using On.RoR2.UI.MainMenu;
//using MiscModpackUtils;
using R2API;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using RoR2.Hologram;
using RoR2.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using MainMenuController = RoR2.UI.MainMenu.MainMenuController;
using Path = System.IO.Path;

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
                }
            }
            
            //try to fix misc modpackutils logo not showing up when i boot into main profile (dunno why it happens ,.,.
            On.RoR2.UI.MainMenu.BaseMainMenuScreen.OnEnter += BaseMainMenuScreenOnOnEnter;
        }

        private void BaseMainMenuScreenOnOnEnter(BaseMainMenuScreen.orig_OnEnter orig, RoR2.UI.MainMenu.BaseMainMenuScreen self, MainMenuController mainMenuController)
        {
            orig(self, mainMenuController);
                
            //if (!MiscModpackUtils.Patches.MainScreen.Override.Value) return;
                
            GameObject obj = GameObject.Find("LogoImage");
            if (obj == null) return;
            obj.transform.localPosition = new Vector3(0, 20, 0);
            obj.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            obj.GetComponent<Image>().sprite = ProdzModpackUtils.Utils.Load(Path.Combine(Paths.ConfigPath, "logo.png"));
            Log.Debug("Changed Logo Image");
        }
    }
}