using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using MiscModpackUtils;
using R2API;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2.UI;
using UnityEngine;
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
            
            if (applySS2.Value)
            {
                try
                {
                    harmony.CreateClassProcessor(typeof(starstorm.Starstorm2ExeChanges)).Patch();
                    LanguageAPI.Add("SS2_EXECUTIONER2_EXECUTION_DESC", $"Leap into the air, then slam an ion axe for <style=cIsDamage>{baseDamage.Value * 100f}-{boostedDamage.Value * 100f}% damage</style>. Hitting an isolated target deals <style=cIsDamage>double damage</style> and restores 3 <color=#29e5f2>Ion Charges</color>.");
                    
                }
                catch
                {
                    Log.Error("failed to patch ss2 !!");
                }
            }
            
            if (applyGCP.Value)
            {
                try
                {
                    harmony.CreateClassProcessor(typeof(gildedcoastplus.GoldenCoastChanges)).Patch();
                }
                catch
                {
                    Log.Error("failed to patch golded coast plus !!");
                }
            }
            
            if (applyCE.Value)
            {
                try
                {
                    harmony.CreateClassProcessor(typeof(coolereclipse.CoolerEclipseChanges)).Patch();
                }
                catch
                {
                    Log.Error("failed to patch cooler eclipse !!");
                }
            }

            

            //make money/lunar text smaller why isnt it smaller it makes me go grrrrrrrrrrrrrrrrrrrrrrrr 
            RoR2.UI.HUD.onHudTargetChangedGlobal += (self) =>
            {
                GameObject upperLeft = GameObject.Find("UpperLeftCluster").gameObject;

                if (!upperLeft) return;

                upperLeft.GetComponent<RectTransform>().offsetMin = new Vector2(0, -64);
                upperLeft.GetComponent<VerticalLayoutGroup>().spacing = 0;
                
                Transform dollar = GameObject.Find("DollarSign").transform;
                if (dollar)
                {
                    dollar.localPosition = new Vector3(4, dollar.localPosition.y, dollar.localPosition.z);
                }
                
                Transform buildLabel = GameObject.Find("SteamBuildLabel").transform;
                if (buildLabel)
                {
                    buildLabel.gameObject.SetActive(false);
                }
            };
            
            //try to fix misc modpackutils logo not showing up when i boot into main profile (dunno why it happens ,.,.
            On.RoR2.UI.MainMenu.MainMenuController.Awake += (orig, controller) =>
            {
                orig(controller);
                
                if (!MiscModpackUtils.Patches.MainScreen.Override.Value) return;
                
                GameObject obj = GameObject.Find("LogoImage");
                if (obj == null) return;
                obj.transform.localPosition = new Vector3(0, 20, 0);
                obj.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                obj.GetComponent<Image>().sprite = Utils.Load(Path.Combine(Paths.ConfigPath, "logo.png"));
                Log.Debug("Changed Logo Image");
            };
            
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

        public static ConfigEntry<bool> applySS2;
        public static ConfigEntry<bool> applyGCP;
        public static ConfigEntry<bool> applyCE;
        public static ConfigEntry<float> baseSpeed;
        public static ConfigEntry<float> boostedSpeed;
        public static ConfigEntry<float> terminalSpeed;
        public static ConfigEntry<float> speedmult;
        public static ConfigEntry<float> baseDamage;
        public static ConfigEntry<float> boostedDamage;
        public static ConfigEntry<bool> useMovespeed;
        public static ConfigEntry<float> eclipseChance;
        public static ConfigEntry<string> blacklistStages;
        public static ConfigEntry<string> whitelistStages;
        public static ConfigEntry<bool> skipGoldenRewards;
        
        public static ConfigEntry<Color> buttonNorm;
        public static ConfigEntry<Color> buttonHigh;
        public static ConfigEntry<Color> buttonPress;
        public static ConfigEntry<Color> buttonSelect;
        
        public void configs()
        {
            #region patches
                
            applySS2 = Config.Bind("apply patches",
                "try to apply ss2 patches !!",
                true,
                "");
            
            applyGCP = Config.Bind("apply patches",
                "try to apply gilded coast plus reival patches !!",
                true,
                "");
            
            applyCE = Config.Bind("apply patches",
                "try to apply cooler eclipse patches !!",
                true,
                "");
            
            #endregion
            
            #region ss2
            
            speedmult = Config.Bind("stats",
                "speed damage multiplier",
                10f,
                "like uhh how much extrad amage should be added of how fast you go past starting velocity compared to terminal ,.,. idk just move it around be yourself !!!! you can just set to 0 if you dont like !!!!");
            StepSliderConfig sliderConfig = new()
            {
                max = 60,
                min = 1,
                FormatString = "{0:0}"
            };
            StepSliderOption slider = new(speedmult, sliderConfig);
            ModSettingsManager.AddOption(slider);

            baseSpeed = Config.Bind("stats",
                "base speed",
                10f,
                "base starting speed for special !!!! multiplied by movespeed unless config for that is off ,.,. (then its multiplied by 10,. .,.,");
            StepSliderConfig sliderConfig2 = new()
            {
                max = 40,
                min = 1,
                FormatString = "{0:0}"
            };
            StepSliderOption slider2 = new(baseSpeed, sliderConfig2);
            ModSettingsManager.AddOption(slider2);

            boostedSpeed = Config.Bind("stats",
                "boosted speed",
                20f,
                "boosted speed when you use special from dash !!!! also multiplied by movespeed unless config for that is off ,.,. (then its multiplied by 10,. .,.,");
            StepSliderConfig sliderConfig3 = new()
            {
                max = 40,
                min = 1,
                FormatString = "{0:0}"
            };
            StepSliderOption slider3 = new(boostedSpeed, sliderConfig3);
            ModSettingsManager.AddOption(slider3);

            terminalSpeed = Config.Bind("stats",
                "terminal speed",
                30f,
                "how fast max speed should be !! be careful equation uses lerp so you go reallys fast if you put a high number ,..,");
            StepSliderConfig sliderConfig4 = new()
            {
                max = 60,
                min = 1,
                FormatString = "{0:0}"
            };
            StepSliderOption slider4 = new StepSliderOption(terminalSpeed, sliderConfig4);
            ModSettingsManager.AddOption(slider4);

            baseDamage = Config.Bind("stats",
                "base damage coeff",
                13f,
                "base damage coeff when not boosted !!!!! speedmult is added on top too ,.,. ");
            StepSliderConfig sliderConfig5 = new()
            {
                max = 25,
                min = 1,
                FormatString = "{0:0}"
            };
            StepSliderOption slider5 = new(baseDamage, sliderConfig5);
            ModSettingsManager.AddOption(slider5);

            boostedDamage = Config.Bind("stats",
                "boosted damage coeff",
                15.5f,
                "boosted damage coeff  !!!!! speedmult is added on top too ,.,. ");
            StepSliderConfig sliderConfig6 = new()
            {
                max = 25,
                min = 1,
                FormatString = "{0:0}"
            };
            StepSliderOption slider6 = new StepSliderOption(boostedDamage, sliderConfig6);
            ModSettingsManager.AddOption(slider6);

            useMovespeed = Config.Bind("stats",
                "use movespeed",
                true,
                "should movespeed affect how fast down you go !!! regular ss2 its just a set number ,.,.");
            CheckBoxConfig checkBoxConfig = new();
            CheckBoxOption checkbox = new(useMovespeed, checkBoxConfig);
            ModSettingsManager.AddOption(checkbox);
            
            baseDamage.SettingChanged += BaseDamageOnSettingChanged;
            boostedDamage.SettingChanged += BaseDamageOnSettingChanged;

            void BaseDamageOnSettingChanged(object sender, EventArgs e)
            {
                LanguageAPI.Add("SS2_EXECUTIONER2_EXECUTION_DESC",
                    $"Leap into the air, then slam an ion axe for <style=cIsDamage>{baseDamage.Value * 100f}-{boostedDamage.Value * 100f}% damage</style>. Hitting an isolated target deals <style=cIsDamage>double damage</style> and restores 3 <color=#29e5f2>Ion Charges</color>.");
            }
            
            #endregion
            #region gildedCostPlus

            skipGoldenRewards = Config.Bind("golden coast plus", 
                "skip hidden buff", 
                true, 
                "");
            
            CheckBoxConfig skipGoldenRewardsCheckboxConfig = new();
            CheckBoxOption skipGoldenRewardsCheckbox = new(skipGoldenRewards, skipGoldenRewardsCheckboxConfig);
            ModSettingsManager.AddOption(skipGoldenRewardsCheckbox);

            #endregion
            #region coolerEclipse

            eclipseChance = Config.Bind("coolerEclipse", "chance for eclipse", 100f, "bwaa,  (0-100 !!!");
            blacklistStages = Config.Bind("coolerEclipse", 
                "stage blacklist", 
                "", 
                "eclipse stage blacklist (seperate by , !! (eg golemplains,blackbeach!!");
            whitelistStages = Config.Bind("coolerEclipse", 
                "stage whitelist", 
                "", 
                "what stages to force eclipses on (seperate by , !! (eg golemplains,blackbeach!! will not work with moon2, ,..");

            StepSliderConfig eclipseChanceSliderConfig = new()
            {
                max = 100,
                min = 1,
                FormatString = "{0:0}"
            };
            StepSliderOption eclipseChanceSlider = new(eclipseChance, eclipseChanceSliderConfig);
            ModSettingsManager.AddOption(eclipseChanceSlider);
            
            InputFieldConfig blacklistStagesInputConfig = new();
            StringInputFieldOption blacklistStagesInput = new(blacklistStages, blacklistStagesInputConfig);
            ModSettingsManager.AddOption(blacklistStagesInput);
            
            InputFieldConfig whitelistStagesInputConfig = new();
            StringInputFieldOption whitelistStagesInput = new(whitelistStages, whitelistStagesInputConfig);
            ModSettingsManager.AddOption(whitelistStagesInput);
            
            #endregion

            #region custom

            buttonNorm = Config.Bind("BNA",
                "normal button color",
                new Color(110/255f, 83/255f, 120/255f, 255/255f),
                ""); 
            ModSettingsManager.AddOption(new ColorOption(buttonNorm));
            
            buttonHigh = Config.Bind("BNA",
                "highlighted button coor",
                new Color(255/255f, 177/255f, 245/255f, 255/255f),
                ""); 
            ModSettingsManager.AddOption(new ColorOption(buttonHigh));
            
            buttonPress = Config.Bind("BNA",
                "pressed button color",
                new Color(192/255f, 113/255f, 182/255f, 255/255f),
                ""); 
            ModSettingsManager.AddOption(new ColorOption(buttonPress));
            
            buttonSelect = Config.Bind("BNA",
                "selected button color",
                new Color(255/255f, 177/255f, 238/255f, 255/255f),
                ""); 
            ModSettingsManager.AddOption(new ColorOption(buttonSelect));

            #endregion
        }
    }
}