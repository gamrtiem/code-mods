using System;
using System.Collections.Generic;
using System.Linq;
using BNR.patches;
using BepInEx.Configuration;
using JetBrains.Annotations;
using R2API;
using RoR2;
using RoR2.ContentManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace BNR;

public class skinrecolors : PatchBase<skinrecolors>
{
    public override void Init()
    {
        applyHooks();
    }

    private void applyHooks()
    {
        if (enabled.Value)
        {
            RoR2.BodyCatalog.availability.onAvailable += RecolorSkins;
        }
        else
        {
            RoR2.BodyCatalog.availability.onAvailable -= RecolorSkins;
        }
    }

    private void RecolorSkins()
    {
        string[] newSkins = skinRecolors.Value.Split(";;");
        foreach (string skin in newSkins)
        {
            string[] skinArgs = skin.Split(",");
            try
            {
                skinRecolor(skinArgs[0],
                    skinArgs[1], 
                    float.Parse(skinArgs[2]),
                    float.Parse(skinArgs[3]),
                    float.Parse(skinArgs[4]),
                    skinArgs[5], 
                    skinArgs.Length == 7 ? skinArgs[6] : "");
            }
            catch (Exception e)
            {
                Log.Warning(e);
            }
        }
    }
    
    [ConCommand(commandName = "list_skin", flags = ConVarFlags.None, helpText = "list internal skins.,,.")]
    public static void ListSkins(ConCommandArgs args)
    {
        Debug.Log("args = " + args[0] + " " );
        var bodyPrefab = BodyCatalog.FindBodyPrefab(args[0]);
        if (!bodyPrefab)
        {
            Log.Warning("body no existey ,,.");
            return;
        }

        var modelLocator = bodyPrefab.GetComponent<ModelLocator>();
        if (!modelLocator)
        {
            Log.Warning("model locator no existey .,.,");
            return;
        }

        var mdl = modelLocator.modelTransform.gameObject;
        var skinController = mdl ? mdl.GetComponent<ModelSkinController>() : null;
        if (!skinController)
        {
            Log.Warning("model skin controller no existey .,,.");
            return;
        }
        
        foreach (SkinDef skinControllerSkinDef in skinController.skins)
        {
            Debug.Log(skinControllerSkinDef.name);
        }
    }

    [ConCommand(commandName = "clear_skin", flags = ConVarFlags.None, helpText = "recolor current skins.,,.")]
    public static void clearSkin(ConCommandArgs args)
    {
        bodyNameToPrev.Clear();
    }
    
    private static Dictionary<string, int> bodyNameToPrev = [];
    [ConCommand(commandName = "recolor_skin", flags = ConVarFlags.None, helpText = "recolor current skins.,,.")]
    public static void recolorSkin(ConCommandArgs args)
    {
        ModelSkinController skinController = Utils.GetModelLocator(args.GetSenderBody().gameObject);

        int currentIndex = skinController.currentSkinIndex;
        if (bodyNameToPrev.TryGetValue(args.senderBody.name, out int value))
        {
            currentIndex = value;
        }
        else
        {
            bodyNameToPrev.Add(args.senderBody.name, skinController.currentSkinIndex);
        }
        SkinDef currentSkin = skinController.skins[currentIndex];
        float sat = 0;
        if (args.Count > 2)
        {
            sat = float.Parse(args[1]);
        }
        float HSVvalue = 0;
        if (args.Count > 3)
        {
            HSVvalue = float.Parse(args[3]);
        }
        SkinDef replacementSkin = skinRecolor(currentSkin.name, args.senderBody.name, float.Parse(args[0]), sat, HSVvalue, "temp");

        Array.Resize(ref skinController.skins, skinController.skins.Length + 1);
        skinController.skins[^1] = replacementSkin;
        skinController.currentSkinIndex = skinController.skins.Length - 1;
        args.senderBody.skinIndex = (uint)(skinController.skins.Length - 1);
        skinController.ApplySkin(skinController.currentSkinIndex);
        
        Log.Debug("bwaa");
    }
    
    [CanBeNull]
    public static SkinDef skinRecolor(string baseSkinDefName, string bodyName, float hue, float saturation, float value, string skinName, string prefix = "", bool dontAdd = false)
    {
        SkinDef recoloredSkinDef = null;
        
        try
        {
            ModelSkinController skinController = Utils.GetModelLocator(BodyCatalog.FindBodyPrefab(bodyName));
            SkinDef skinDef = UnityEngine.Object.Instantiate(skinController.skins.First(skindef => skindef.name == baseSkinDefName));
            
            Texture2D newTexture = Utils.hsvModifyTexture(skinDef.icon.texture, hue, saturation/100f, value/100f);
            Texture2D newIcon = new Texture2D(Mathf.FloorToInt(skinDef.icon.textureRect.width), Mathf.FloorToInt(skinDef.icon.textureRect.height));
            
            var pixels = newTexture.GetPixels(  
                Mathf.FloorToInt(skinDef.icon.textureRect.x), 
                Mathf.FloorToInt(skinDef.icon.textureRect.y), 
                Mathf.FloorToInt(skinDef.icon.textureRect.width), 
                Mathf.FloorToInt(skinDef.icon.textureRect.height) );
            newIcon.SetPixels(pixels);
            newIcon.Apply();
            
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
                    renderer.defaultMaterial = Utils.RecolorMaterial(newMat, hue, saturation, value);
                    renderer.defaultMaterialAddress = new AssetReferenceT<Material>("");
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
                    newParams.rendererInfos[i].defaultMaterial = Utils.RecolorMaterial(newMat, hue, saturation, value);
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

    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("BNR - skinrecolors",
            "enable patches for skinrecolors",
            true,
            "");
        Utils.CheckboxConfig(enabled);
        enabled.SettingChanged += (_, _) => { applyHooks(); };
        
        skinRecolors = config.Bind("BNR - skinrecolors",
            "skin recolors",
            "skinCommandoAlt,CommandoBody,100,0,0,Test Skin;;skinCommandoAlt,CommandoBody,200,0,0,Test Skin 2;;skinCommandoDefault,CommandoBody,290,-40,-10,Awesome Skin !!!!",
            "follows \"string baseSkinDefName, string bodyName, float hue, float saturation, float value, string skinName, string prefix\" where prefix is optional (used for like ,., Red on wolfo qol merc.,., use list_skins to get internal names or prodz debugging mod ,., split with ;; ..,,. you can temporarily try out recolors with recolor_skin hue saturation value ,.,,.");
    }

    private ConfigEntry<string> skinRecolors;
    private ConfigEntry<bool> enabled;
}