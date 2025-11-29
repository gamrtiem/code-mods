using static BNR.butterscotchnroses;
using BNR.patches;
using BepInEx.Configuration;
using HarmonyLib;
using On.RoR2.UI.SkinControllers;
using RiskOfOptions;
using RiskOfOptions.Options;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BNR;

public class buttonrecolors : PatchBase<buttonrecolors>
{
    public override void Init(Harmony harmony)
    {
        if (!enabled.Value)
        {
            return;
        }
        
        On.RoR2.UI.SkinControllers.ButtonSkinController.Awake += ButtonSkinControllerOnAwake;
        On.RoR2.UI.SkinControllers.PanelSkinController.Awake += PanelSkinControllerOnAwake;
    }

    private static void PanelSkinControllerOnAwake(PanelSkinController.orig_Awake orig, RoR2.UI.SkinControllers.PanelSkinController self)
    {
        orig(self);
        if (!colorButtons.Value) return;
                    
        if (self.gameObject.name.Contains("ObjectivePanel")) return;//objective panel
        if (self.gameObject.name.Contains("NakedButton (Quit)")) return; //back button css
        Image image = self.gameObject.GetComponent<Image>();
        if (image.gameObject.name.Contains("RuleBook")) return;

        image.color = BNRUtils.Color255(buttonNorm.Value.r, buttonNorm.Value.g, buttonNorm.Value.b, image.color.a);
    }

    private static void ButtonSkinControllerOnAwake(ButtonSkinController.orig_Awake orig, RoR2.UI.SkinControllers.ButtonSkinController self)
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
    }

    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("BNR - UI",
            "enable patches for button recolors",
            true,
            "");
        BNRUtils.CheckboxConfig(enabled);
        enabled.SettingChanged += (_, _) =>
        {
            if (enabled.Value)
            {
                On.RoR2.UI.SkinControllers.ButtonSkinController.Awake += ButtonSkinControllerOnAwake;
                On.RoR2.UI.SkinControllers.PanelSkinController.Awake += PanelSkinControllerOnAwake;
            }
            else
            {
                On.RoR2.UI.SkinControllers.ButtonSkinController.Awake -= ButtonSkinControllerOnAwake;
                On.RoR2.UI.SkinControllers.PanelSkinController.Awake -= PanelSkinControllerOnAwake;
            }
        };
        
        colorButtons = config.Bind("BNR - UI",
            "change buttons colors", 
            true, 
            "whether or not to use custom menu button colors,..,"); 
        ModSettingsManager.AddOption(new CheckBoxOption(colorButtons));
            
        buttonNorm = config.Bind("BNR - UI",
            "normal button color",
            BNRUtils.Color255(110, 83, 120),
            ""); 
        ModSettingsManager.AddOption(new ColorOption(buttonNorm));
            
        buttonHigh = config.Bind("BNR - UI",
            "highlighted button coor",
            BNRUtils.Color255(255, 177, 245),
            ""); 
        ModSettingsManager.AddOption(new ColorOption(buttonHigh));
            
        buttonPress = config.Bind("BNR - UI",
            "pressed button color",
            BNRUtils.Color255(192, 113, 182),
            ""); 
        ModSettingsManager.AddOption(new ColorOption(buttonPress));
            
        buttonSelect = config.Bind("BNR - UI",
            "selected button color",
            BNRUtils.Color255(255, 177, 238),
            ""); 
        ModSettingsManager.AddOption(new ColorOption(buttonSelect));
    }

    private ConfigEntry<bool> enabled;
    private static ConfigEntry<bool> colorButtons;
    private static ConfigEntry<Color> buttonNorm;
    private static ConfigEntry<Color> buttonHigh;
    private static ConfigEntry<Color> buttonPress;
    private static ConfigEntry<Color> buttonSelect;
}