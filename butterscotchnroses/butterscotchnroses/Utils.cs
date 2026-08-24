using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BepInEx;
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
using Path = System.IO.Path;

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
    
    public static void CheckboxConfig(ConfigEntry<bool> config, bool restartRequired = false)
    {
        CheckBoxConfig checkBoxConfig = new CheckBoxConfig();
        checkBoxConfig.restartRequired = restartRequired;
        CheckBoxOption checkBoxOption = new CheckBoxOption(config, checkBoxConfig);
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

    public static Texture2D hsvModifyTexture(Texture2D texture, float hueShift = 0, float saturation = 0, float value = 0, bool dontExport = false, bool multiplySaturation = false)
    {
        Texture2D returnTexture;

        string fileType = skinrecolors.fileType.Value switch
        {
            skinrecolors.fileTypes.png => "png",
            skinrecolors.fileTypes.jpg => "jpg",
            skinrecolors.fileTypes.tga => "tga",
            _ => "png"
        };
        string testFileName = $"{texture.name}_RecolorH{hueShift}S{saturation}V{value}.{fileType}";
        string testPath = $"{skinrecolors.textureDirs}\\{testFileName}";
        if (skinrecolors.recoloredTextures.TryGetValue($"{testFileName}", out Texture2D texture2D))
        {
            Log.Debug($"found {testFileName} in recolored texture dict !!");
            returnTexture = texture2D;
        }
        else
        {
            returnTexture = makeReadable(texture);
            Color[] texPixels = returnTexture.GetPixels(0, 0, returnTexture.width, returnTexture.height);

            for (int i = 0; i < texPixels.Length; i++)
            {
                Color pixelColor = texPixels[i];
                UnityEngine.Color.RGBToHSV(pixelColor, out float h, out float s, out float v);
            
                h = (h + hueShift / 360f) % 1f;
                if (h < 0f) h += 1f;
                v += value/100f;
                if (multiplySaturation)
                {
                    s *= saturation;
                }
                else
                {
                    s += saturation/100f;
                }
                
                Color newColor = UnityEngine.Color.HSVToRGB(h, s, v);
                newColor.a = pixelColor.a;
                texPixels[i] = newColor;
            }
        
            returnTexture.SetPixels(texPixels);
            returnTexture.Apply();

            if (!dontExport)
            {
                Log.Debug($"created return texture !!! {texture.name} {dontExport}"); 
                File.WriteAllBytes(testPath, returnTexture.EncodeToPNG());
            }
        }
        
        returnTexture.name = $"{texture.name}_RecolorH{hueShift}S{saturation}V{value}";
        returnTexture.anisoLevel = texture.anisoLevel;
        returnTexture.filterMode = texture.filterMode;
        returnTexture.wrapMode = texture.wrapMode;
        returnTexture.wrapModeU = texture.wrapModeU;
        returnTexture.wrapModeV = texture.wrapModeV;
        returnTexture.wrapModeW = texture.wrapModeW;
        
        return returnTexture;
    }
    
    public static Material RecolorMaterial(Material mat, float hue, float saturation, float value, bool dontAdd = false, bool multiplySaturation = false)
    {
        if (mat.HasTexture(MainTex) && mat.GetTexture(MainTex) != null)
            mat.SetTexture(MainTex, hsvModifyTexture(mat.GetTexture(MainTex) as Texture2D, hue, saturation/100f, value/100f, dontAdd, multiplySaturation));
        
        if (mat.HasTexture(EmTex) && mat.GetTexture(EmTex) != null)
            mat.SetTexture(EmTex, hsvModifyTexture(mat.GetTexture(EmTex) as Texture2D, hue, saturation/100f, value/100f, dontAdd, multiplySaturation));
        
        if (mat.HasTexture(RemapTex) && mat.GetTexture(RemapTex) != null)
            mat.SetTexture(RemapTex, hsvModifyTexture(mat.GetTexture(RemapTex) as Texture2D, hue, saturation/100f, value/100f, dontAdd, multiplySaturation));
        
        if (mat.HasTexture(FresnelRamp) && mat.GetTexture(FresnelRamp) != null)
            mat.SetTexture(FresnelRamp, hsvModifyTexture(mat.GetTexture(FresnelRamp) as Texture2D, hue, saturation/100f, value/100f, dontAdd, multiplySaturation));

        TryRecolorMat(mat, Color, hue, saturation, value, multiplySaturation);
        TryRecolorMat(mat, EmColor, hue, saturation, value, multiplySaturation);
        TryRecolorMat(mat, TintColor, hue, saturation, value, multiplySaturation);
        TryRecolorMat(mat, WireframeColor, hue, saturation, value, multiplySaturation);
        TryRecolorMat(mat, VertColor, hue, saturation, value, multiplySaturation);

        return mat;
    }

    private static void TryRecolorMat(Material mat, int nameID, float hue, float sat, float value, bool multiplySat = false)
    {
        if (!mat.HasColor(nameID)) return;
        
        //Log.Debug($"asdasdasd {mat}");
        
        Color origColor = mat.GetColor(nameID);
        UnityEngine.Color.RGBToHSV(origColor, out float colorHue, out float colorSaturation, out float colorValue);
                        
        colorHue = (colorHue + hue / 360f) % 1f;
        if (colorHue < 0f)
        {
            colorHue += 1f;
        }
        if (multiplySat)
        {
            colorSaturation *= sat / 100f;
        }
        else
        {
            colorSaturation += sat / 100f;
        }
        colorValue += value/100f;
        
        Color newColor = UnityEngine.Color.HSVToRGB(colorHue, colorSaturation, colorValue);
        newColor.a = origColor.a;
            
        mat.SetColor(nameID, newColor);
    }
    
    private static readonly int MainTex = Shader.PropertyToID("_MainTex");
    private static readonly int RemapTex = Shader.PropertyToID("_RemapTex");
    private static readonly int EmTex = Shader.PropertyToID("_EmTex");
    private static readonly int FresnelRamp = Shader.PropertyToID("_FresnelRamp");
    private static readonly int EmColor = Shader.PropertyToID("_EmColor");
    private static readonly int Color = Shader.PropertyToID("_Color");
    private static readonly int TintColor = Shader.PropertyToID("_TintColor");
    private static readonly int WireframeColor = Shader.PropertyToID("WireframeColor");
    private static readonly int VertColor = Shader.PropertyToID("_VertColor");

    public static ModelSkinController GetModelLocator(GameObject characterbody)
    {
        //stolen from keb skin builder script ,.,.
        if (!characterbody)
        {
            Log.Warning($"failed to get model locator from null ,.,.");
            return null;
        }

        var modelLocator = characterbody.GetComponent<ModelLocator>();
        if (!modelLocator)
        {
            Log.Warning($"failed to get model skin controller since couldnts find model locator on {characterbody.name},.,,. ");
            return null;
        }

        var mdl = modelLocator.modelTransform.gameObject;
        var skinController = mdl ? mdl.GetComponent<ModelSkinController>() : null;
        if (!skinController)
        {
            Log.Warning($"failed to get model skin controlelr since couldnts find model skin controller components on {characterbody.name} ,.,..");
            return null;
        }

        return skinController;
    }
}