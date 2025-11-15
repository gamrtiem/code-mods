using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using On.RoR2.Items;
using R2API;
using Rewired;
using RoR2;
using RoR2.ContentManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using LocalUserManager = On.RoR2.LocalUserManager;
using UnityHotReloadNS;
using MiscModpackUtils;
using UnityEngine.UI;
using BepInEx;
using BepInEx.Configuration;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;
using Path = System.IO.Path;

namespace TestMod
{
    [BepInDependency(ItemAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    public class TestMod : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName; // adding zzz to make sure plugin loads after everything else so modded itemdefs are intiialized
        public const string PluginAuthor = "icebro";
        public const string PluginName = "TestMod";
        public const string PluginVersion = "1.0.0";

        public void Awake()
        {
            Log.Init(Logger);

            Logger.LogDebug("loading mod !!");

            On.RoR2.UI.MainMenu.MainMenuController.Awake += (orig, self) =>
            {
                orig(self);

                GameObject obj = GameObject.Find("LogoImage");
                if (obj == null) return;
                obj.transform.localPosition = new Vector3(0, 20, 0);
                obj.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                obj.GetComponent<Image>().sprite = Utils.Load(Path.Combine(Paths.ConfigPath, "logo.png"));
                Main.Log.LogInfo("Changed Logo Image");
            };
        }

        private void Update()
        {
            if (Input.GetKeyUp(KeyCode.F2))
            {
                UnityHotReload.LoadNewAssemblyVersion(
                    typeof(TestMod).Assembly, // The currently loaded assembly to replace.
                    "Z:\\run\\media\\icebrah\\buh\\gale\\riskofrain2\\profiles\\debug\\BepInEx\\plugins\\TestMod/TestMod.dll"  // The path to the newly compiled DLL.
                );
            }
            if (Input.GetKeyUp(KeyCode.F3))
            {
                var helper = "Sdf sdf sdfsfd ";
                Log.Debug("hhihihiii 2324234234234234" + helper);
            }
        }
    }
}
