using System;
using System.Linq;
using BepInEx.Configuration;
using JetBrains.Annotations;
using On.RoR2.UI;
using R2API;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace BNR;

public class Utils
{
    public static Color Color255(int r, int g, int b, int a = 255)
    {
        return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
    }
    
    public static Color Color255(float r, float g, float b, float a = 255)
    {
        return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
    }
    
    public static void SliderConfig(float min, float max, ConfigEntry<float> config)
    {
        StepSliderConfig stepSliderConfig = new()
        {
            max = max,
            min = min,
            FormatString = "{0:0}"
        };
        StepSliderOption stepSliderOption = new(config, stepSliderConfig);
        ModSettingsManager.AddOption(stepSliderOption);
    }
    
    public static void SliderConfig(int min, int max, ConfigEntry<int> config)
    {
        IntSliderConfig intSliderConfig = new()
        {
            max = max,
            min = min,
            formatString = "{0:0}"
        };
        IntSliderOption intSliderOption = new(config, intSliderConfig);
        ModSettingsManager.AddOption(intSliderOption);
    }
    
    public static void CheckboxConfig(ConfigEntry<bool> config)
    {
        CheckBoxConfig checkBoxConfig = new();
        CheckBoxOption checkBoxOption = new(config, checkBoxConfig);
        ModSettingsManager.AddOption(checkBoxOption);
    }

    public static void StringConfig(ConfigEntry<string> config)
    {
        InputFieldConfig inputFieldConfig = new();
        StringInputFieldOption stringInputFieldOption = new(config, inputFieldConfig);
        ModSettingsManager.AddOption(stringInputFieldOption);
    }
    
    public static Texture2D makeReadable(Texture texture)
    {
        var tmp = RenderTexture.GetTemporary(texture.width, texture.height, 32);
        tmp.name = "Whatever";
        tmp.enableRandomWrite = true;
        tmp.Create();
            
        // Create a temporary RenderTexture of the same size as the texture
        // RenderTexture tmp = RenderTexture.GetTemporary(
        //     texture.width,
        //     texture.height,
        //     0,
        //     RenderTextureFormat.Default,
        //     RenderTextureReadWrite.Linear);

        // Blit the pixels on texture to the RenderTexture
        UnityEngine.Graphics.Blit(texture, tmp);
        // Backup the currently set RenderTexture
        RenderTexture previous = RenderTexture.active;
        // Set the current RenderTexture to the temporary one we created
        RenderTexture.active = tmp;
        // Create a new readable Texture2D to copy the pixels to it
        Texture2D myTexture2D = new Texture2D(texture.width, texture.height);
        // Copy the pixels from the RenderTexture to the new Texture
        myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
        myTexture2D.Apply();
        // Reset the active RenderTexture
        RenderTexture.active = previous;
        // Release the temporary RenderTexture
        RenderTexture.ReleaseTemporary(tmp);

        return myTexture2D;
    }

    public static Texture2D hsvModifyTexture(Texture2D texture, float hueShift = 0, float saturation = 0, float value = 0)
    {
        Texture2D readableTex = makeReadable(texture);
        Color[] texPixels = readableTex.GetPixels(0, 0, readableTex.width, readableTex.height);

        for (int i = 0; i < texPixels.Length; i++)
        {
            Color pixelColor = texPixels[i];
            Color.RGBToHSV(pixelColor, out float h, out float s, out float v);
            
            h = (h + hueShift / 360f) % 1f;
            if (h < 0f) h += 1f;
            s = (s + saturation);
            v = (v + value);
            
            texPixels[i] = Color.HSVToRGB(h, s, v);
        }

        readableTex.SetPixels(texPixels);
        readableTex.Apply();
        return readableTex;
    }

    [CanBeNull]
    public static SkinDef skinRecolor(string baseSkinDefName, string bodyName, float hue, float saturation, float value, string skinName, string prefix = "", bool dontAdd = false)
    {
        SkinDef recoloredSkinDef = null;
        
        try
        {
            //stolen from keb skin builder script ,.,.
            var bodyPrefab = BodyCatalog.FindBodyPrefab(bodyName);
            if (!bodyPrefab)
            {
                Log.Warning($"failed to recolor {skinName} since couldnts find body name {bodyName} ,.,.");
                return null;
            }

            var modelLocator = bodyPrefab.GetComponent<ModelLocator>();
            if (!modelLocator)
            {
                Log.Warning($"failed to recolor {skinName} since couldnts find model locator on {bodyName},.,,. ");
                return null;
            }

            var mdl = modelLocator.modelTransform.gameObject;
            var skinController = mdl ? mdl.GetComponent<ModelSkinController>() : null;
            if (!skinController)
            {
                Log.Warning($"failed to recolor {skinName} since couldnts find model skin controller components on {bodyName} ,.,..");
                return null;
            }
            
            SkinDef skinDef = UnityEngine.Object.Instantiate(skinController.skins.First(skindef => skindef.name == baseSkinDefName));
            
            Texture2D newIcon = hsvModifyTexture(skinDef.icon.texture, hue, saturation/100f, value/100f);
            Sprite newIconSprite = Sprite.Create(newIcon, new Rect(0, 0, newIcon.width, newIcon.height), new Vector2(newIcon.width / 2, newIcon.height / 2));
            skinDef.icon = newIconSprite;

            if (skinDef.skinDefParams == null && skinDef.skinDefParamsAddress == null)
            {
                //legacy skins use this i think .,.,
                CharacterModel.RendererInfo[] newRenderers = new CharacterModel.RendererInfo[skinDef.rendererInfos.Length];

                for (int i = 0; i < skinDef.rendererInfos.Length; i++)
                {
                    CharacterModel.RendererInfo renderer = skinDef.rendererInfos[i];
                    Material newMat = UnityEngine.Object.Instantiate(renderer.defaultMaterial);
                    newMat.SetTexture(MainTex, hsvModifyTexture(newMat.GetTexture(MainTex) as Texture2D, hue, saturation/100f, value/100f));
                    newMat.SetTexture(EmTex, hsvModifyTexture(newMat.GetTexture(EmTex) as Texture2D, hue, saturation/100f, value/100f));
                
                    Color.RGBToHSV(newMat.GetColor(EmColor), out float colorHue, out float colorSaturation, out float colorValue);
                    colorHue = (colorHue + hue / 360f) % 1f;
                    if (colorHue < 0f) colorHue += 1f;
                    colorSaturation = (colorSaturation + saturation/100f);
                    colorValue = (colorValue + value/100f);
                
                    newMat.SetColor(EmColor, Color.HSVToRGB(colorHue, colorSaturation, colorValue));
                    renderer.defaultMaterial = newMat;
                    newRenderers[i] = renderer;
                }

                skinDef.rendererInfos = newRenderers;
            }
            else
            {
                var newParams = UnityEngine.Object.Instantiate(skinDef.skinDefParams == null ? skinDef.skinDefParamsAddress.LoadAssetAsync().WaitForCompletion() : skinDef.skinDefParams);

                for (int i = 0; i < newParams.rendererInfos.Length; i++)
                {
                    Material newMat = UnityEngine.Object.Instantiate(newParams.rendererInfos[i].defaultMaterial == null ? newParams.rendererInfos[i].defaultMaterialAddress.LoadAssetAsync().WaitForCompletion() : newParams.rendererInfos[i].defaultMaterial);
                    
                    if (newMat.HasTexture(MainTex) && newMat.GetTexture(MainTex) != null)
                    {
                        newMat.SetTexture(MainTex, hsvModifyTexture(newMat.GetTexture(MainTex) as Texture2D, hue, saturation/100f, value/100f));
                    }
                    if (newMat.HasTexture(EmTex) && newMat.GetTexture(EmTex) != null)
                    {
                        newMat.SetTexture(EmTex, hsvModifyTexture(newMat.GetTexture(EmTex) as Texture2D, hue, saturation/100f, value/100f));
                    }

                    if (newMat.HasColor(EmColor))
                    {
                        Color.RGBToHSV(newMat.GetColor(EmColor), out float colorHue, out float colorSaturation, out float colorValue);
                        
                        colorHue = (colorHue + hue / 360f) % 1f;
                        if (colorHue < 0f) colorHue += 1f;
                        colorSaturation += saturation/100f;
                        colorValue += value/100f;
                
                        newMat.SetColor(EmColor, Color.HSVToRGB(colorHue, colorSaturation, colorValue));
                    }
                   
                    newParams.rendererInfos[i].defaultMaterial = newMat;
                    newParams.rendererInfos[i].defaultMaterialAddress = new AssetReferenceT<Material>("");
                }

                skinDef.optimizedSkinDefParams = newParams;
                skinDef.skinDefParams = newParams;
                skinDef.skinDefParamsAddress = new AssetReferenceT<SkinDefParams>("");
            }

            string internalName = skinName.Replace(" ", "");
            skinDef.name = skinDef.name.Replace("(Clone)", "");
            skinDef.name += $"Recolored{internalName}";
            skinDef.name = prefix + skinDef.name; // if someone wants ot add like Red or something to check for wolfo ,.,.
            skinDef.nameToken += $"_BNR_{internalName.ToUpper()}";
            LanguageAPI.Add(skinDef.nameToken, skinName);

            if (!dontAdd)
            {
                Array.Resize(ref skinController.skins, skinController.skins.Length + 1);
                skinController.skins[^1] = skinDef;
            }
            Log.Debug($"added {skinName} to {bodyName} !!!!");

            recoloredSkinDef = skinDef;
        }
        catch (Exception e)
        {
            Log.Warning($"faileds to add {skinName} skin to {bodyName} ,.,.,.");
            Log.Error(e);
        }

        return recoloredSkinDef;
    }
    
    private static readonly int MainTex = Shader.PropertyToID("_MainTex");
    private static readonly int EmTex = Shader.PropertyToID("_EmTex");
    private static readonly int EmColor = Shader.PropertyToID("_EmColor");
}