using System;
using System.Linq;
using System.Reflection;
using BepInEx;
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
            
            configs();

            On.RoR2.Hologram.HologramProjector.BuildHologram += (orig, self) =>
            {
                orig(self);

                if (self.gameObject.GetComponent<DroneAvailability>() && !self.gameObject.name.Contains("Turret"))
                {
                    Texture icon = self.gameObject.GetComponent<SummonMasterBehavior>().masterPrefab.GetComponent<CharacterMaster>().bodyPrefab.GetComponent<CharacterBody>().portraitIcon;
                    Transform hologram = self.gameObject.transform.Find("HologramPivot");
                    
                    GameObject sprite = new("CustomSprite");
                    sprite.transform.SetParent(hologram.transform);
                    sprite.AddComponent<HologramHelper>();
                    SpriteRenderer renderer = sprite.AddComponent<SpriteRenderer>();
                    renderer.sprite = Sprite.Create((icon as Texture2D), new Rect(0, 0, icon.width, icon.height), new Vector2(0.5f, 0.5f));
                    sprite.transform.localScale = new Vector3(2f, 2f, 1f);
                    renderer.sharedMaterial = Addressables.LoadAssetAsync<Material>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC2.matHalcyoniteShrineCrystalGlow_mat).WaitForCompletion();
                    renderer.sharedMaterial.SetTexture("_Cloud2Tex", Addressables.LoadAssetAsync<Texture>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common.texCloudOrganic2_png).WaitForCompletion());
                    renderer.sharedMaterial.SetTexture("_Cloud1Tex", Addressables.LoadAssetAsync<Texture>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC2_Child.texChildPrimaryStarCloud_png).WaitForCompletion());
                    renderer.sharedMaterial.SetColor("_TintColor", BNRUtils.Color255(191, 126, 211));
                    renderer.sharedMaterial.SetInt("_AlphaBias", 0);
                    
                    var vfx = Instantiate(Addressables.LoadAssetAsync<GameObject>("RoR2/DLC3/Drone Tech/CommandCarryTransportVFX.prefab").WaitForCompletion(), sprite.transform, true);
                    for(int i = 0; i < vfx.transform.childCount; i++)
                    {
                        GameObject vfxChild = vfx.transform.GetChild(i).gameObject;
                        if (vfxChild.name != "MainRings")
                        {
                            Destroy(vfxChild);
                        }
                        else
                        {
                            vfxChild.GetComponent<ParticleSystem>().startSpeed = -10;
                            vfxChild.GetComponent<ParticleSystem>().simulationSpace = ParticleSystemSimulationSpace.World;
                            vfxChild.GetComponent<ParticleSystemRenderer>().sharedMaterial.SetColor("_TintColor", BNRUtils.Color255(252, 142, 249));
                        }
                    }
                    vfx.transform.position = vfx.transform.localPosition;
                    vfx.transform.localPosition = new Vector3(0, 0, 0);
                    vfx.transform.position = new Vector3(vfx.transform.position.x, vfx.transform.position.y - 3.2f, vfx.transform.position.z);
                }
            };
            On.RoR2.SummonMasterBehavior.OnEnable += (orig, self) =>
            {
                orig(self);
                if (self.GetComponent<DroneAvailability>())
                {
                    HologramProjector projector = self.GetComponent<HologramProjector>();
                    if (projector)
                    {
                        projector.displayDistance = 45f;
                    }
                    else
                    {
                        Log.Warning("failed to find projector on drone !!");
                    }
                }
            };
            
            //make money/lunar text smaller why isnt it smaller it makes me go grrrrrrrrrrrrrrrrrrrrrrrr 
            RoR2.UI.HUD.onHudTargetChangedGlobal += (self) =>
            {
                if (!currencyMenu.Value) return;
                
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
            On.RoR2.UI.MainMenu.BaseMainMenuScreen.OnEnter += BaseMainMenuScreenOnOnEnter;
            
            //this doesnt work fix later 
           On.RoR2.UI.SkinControllers.ButtonSkinController.Awake += (orig, self) =>
            {
                orig(self);
                if (!colorButtons.Value) return;
                
                HGButton hgButton = self.gameObject.GetComponent<HGButton>();
                if (!hgButton || hgButton.name.Contains("SurvivorIcon")) return;
                if (hgButton.gameObject.name.Contains("Choice")) return; //difficulty icons
                if (hgButton.gameObject.name.Contains("Loadout")) return; //css
                if (hgButton.gameObject.name.Contains("ObjectivePanel")) return; 
                if (hgButton.gameObject.name.Contains("NakedButton (Quit)")) return;
                
                
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
                if (!colorButtons.Value) return;
                    
                if (self.gameObject.name.Contains("ObjectivePanel")) return;//objective panel
                if (self.gameObject.name.Contains("NakedButton (Quit)")) return; //back button css
                Image image = self.gameObject.GetComponent<Image>();
                if (image.gameObject.name.Contains("RuleBook")) return;

                image.color = new Color(buttonNorm.Value.r, buttonNorm.Value.g, buttonNorm.Value.b, image.color.a);
            };
            
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

        public static ConfigEntry<bool> currencyMenu;
        public static ConfigEntry<bool> colorButtons;
        public static ConfigEntry<Color> buttonNorm;
        public static ConfigEntry<Color> buttonHigh;
        public static ConfigEntry<Color> buttonPress;
        public static ConfigEntry<Color> buttonSelect;
        
        public void configs()
        {
            currencyMenu = Config.Bind("BNR - UI",
                "make currency thing smaller", 
                true, 
                "make the thing that has coins and lunar coins in the top left smaller like pre sotv (i miss you ,.,."); 
            ModSettingsManager.AddOption(new CheckBoxOption(currencyMenu));
            
            colorButtons = Config.Bind("BNR - UI",
                "change buttons colors", 
                true, 
                "whether or not to use custom menu button colors,..,"); 
            ModSettingsManager.AddOption(new CheckBoxOption(colorButtons));
            
            buttonNorm = Config.Bind("BNR - UI",
                "normal button color",
                new Color(110/255f, 83/255f, 120/255f, 255/255f),
                ""); 
            ModSettingsManager.AddOption(new ColorOption(buttonNorm));
            
            buttonHigh = Config.Bind("BNR - UI",
                "highlighted button coor",
                new Color(255/255f, 177/255f, 245/255f, 255/255f),
                ""); 
            ModSettingsManager.AddOption(new ColorOption(buttonHigh));
            
            buttonPress = Config.Bind("BNR - UI",
                "pressed button color",
                new Color(192/255f, 113/255f, 182/255f, 255/255f),
                ""); 
            ModSettingsManager.AddOption(new ColorOption(buttonPress));
            
            buttonSelect = Config.Bind("BNR - UI",
                "selected button color",
                new Color(255/255f, 177/255f, 238/255f, 255/255f),
                ""); 
            ModSettingsManager.AddOption(new ColorOption(buttonSelect));
        }
    }
}


public class HologramHelper : MonoBehaviour
{
    private Transform hologram;
    private Quaternion parentRot;
    public Vector3 pos;
    public void OnEnable()
    {
        hologram = transform.parent.GetChild(0);
        parentRot = transform.parent.parent.rotation;
        transform.position = transform.localPosition;
        transform.localPosition = new Vector3(0, 0, 0);
        transform.position = new Vector3(transform.position.x, transform.position.y + 2f, transform.position.z);
        
        Log.Debug("" + hologram.gameObject.name);
    }

    public void FixedUpdate()
    {
        if(!hologram) 
        {
            Destroy(this.gameObject); // might want to hook onto where the hologram gets killed instead 
            return;
        }
        Vector3 target;
        target.x = 0;
        target.y = hologram.eulerAngles.y;
        target.z = hologram.eulerAngles.z;
        transform.rotation = Quaternion.Euler(target);
    }
}