using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BepInEx;
using BNR.patches;
using BepInEx.Configuration;
using JetBrains.Annotations;
using On.RoR2.UI;
using R2API;
using RoR2;
using RoR2.ContentManagement;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Debug = UnityEngine.Debug;
using Path = RoR2.Path;

namespace BNR;

public class skinrecolors : PatchBase<skinrecolors>
{
    private static SkinDef baseSkinName;
    private static string newSkinName;
    private static CharacterBody currentBody;
    private static float[] hsv = [0, 0, 0];
    public static string textureDirs;
    public static SkinDef baseSkin;
    public static Dictionary<string, Texture2D> recoloredTextures = new Dictionary<string, Texture2D>();
    public override async void Init()
    {
        textureDirs = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Paths.BepInExConfigPath)!, "skintextures");
        if (!Directory.Exists(textureDirs))
        {
            Directory.CreateDirectory(textureDirs);
        }

        baseSkin = Addressables.LoadAssetAsync<SkinDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Commando.skinCommandoDefault_asset).WaitForCompletion();
        applyHooks();
    }

    private void applyHooks()
    {
        if (enabled.Value)
        {
            On.RoR2.SkinCatalog.Init += SkinCatalogOnInit;
            //RoR2.BodyCatalog.availability.onAvailable += RecolorSkins;
        }
        else
        {
            On.RoR2.SkinCatalog.Init -= SkinCatalogOnInit;
            //RoR2.BodyCatalog.availability.onAvailable -= RecolorSkins;
        }
    }
    
    private IEnumerator SkinCatalogOnInit(On.RoR2.SkinCatalog.orig_Init orig)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        stopwatch.Start();

        RecolorSkins();
        
        Log.Debug($"recolored {skinRecolors.Value.Split(";;").Length} skins in {stopwatch.ElapsedMilliseconds}ms !!!");
        yield return orig();
    }

    private static void RecolorSkins()
    {
        string[] newSkinRecolors = skinRecolors.Value.Split(";;");
        foreach (string skinRecolorParams in newSkinRecolors)
        {
            string[] skinArgs = skinRecolorParams.Split(",");
      
            skinRecolor(skinArgs[0],
                skinArgs[1],
                float.Parse(skinArgs[2]),
                float.Parse(skinArgs[3]),
                float.Parse(skinArgs[4]),
                skinArgs[5],
                skinArgs.Length == 7 ? skinArgs[6] : "");
        }
    }
    
    //[CanBeNull]
    public static SkinDef skinRecolor(string baseSkinDefName, string bodyName, float hue, float saturation, float value, string skinName, string prefix = "", bool dontAdd = false)
    {
        SkinDef recoloredSkinDef = baseSkin;
        
        try
        {
            ModelSkinController skinController = Utils.GetModelLocator(BodyCatalog.FindBodyPrefab(bodyName));
            if (skinController == null)
            {
                return recoloredSkinDef;
            }
            SkinDef originalSkin = null;
            foreach (var iterateSkinDef in skinController.skins)
            {
                if (iterateSkinDef.name != baseSkinDefName) continue;
                originalSkin = iterateSkinDef;
                break;
            }
            if(originalSkin == null) return recoloredSkinDef;

            SkinDef skinDef = UnityEngine.Object.Instantiate(originalSkin);
            
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
                //Log.Debug($"skinDef.skinDefParamsAddress null ? {skinDef.skinDefParamsAddress == null}");
                //Log.Debug($"skinDef.skinDefParams null ? {skinDef.skinDefParams}");
                //Log.Debug($"SkinDefParams.FromSkinDef null ? {SkinDefParams.FromSkinDef(originalSkin)}");
                var newParams = UnityEngine.Object.Instantiate(skinDef.skinDefParamsAddress != null && skinDef.skinDefParamsAddress.ToString() != "[]" ? skinDef.skinDefParamsAddress.LoadAssetAsync().WaitForCompletion() : (skinDef.skinDefParams == null ? SkinDefParams.FromSkinDef(originalSkin) : skinDef.skinDefParams));

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
    
    [ConCommand(commandName = "skin_create", flags = ConVarFlags.None, helpText = "list internal skins.,,.")]
    public static void CreateSkin(ConCommandArgs args)
    {
        if (baseSkinName == null)
        {
            Log.Warning("base skin null !!");
            return;
        }
        
        string skinName = !newSkinName.IsNullOrWhiteSpace() ? newSkinName : "Generated Skin";
        skinRecolors.Value += $";;{baseSkinName.name},{currentBody.name[..^7]},{hsv[0]},{hsv[1]},{hsv[2]},{skinName}";
    }
    
    [ConCommand(commandName = "skin_list", flags = ConVarFlags.None, helpText = "list internal skins.,,.")]
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

    [ConCommand(commandName = "skin_clear", flags = ConVarFlags.None, helpText = "recolor current skins.,,.")]
    public static void clearSkin(ConCommandArgs args)
    {
        bodyNameToPrev.Clear();
    }
    
    private static Dictionary<string, int> bodyNameToPrev = [];
    [ConCommand(commandName = "skin_recolor", flags = ConVarFlags.None, helpText = "recolor current skins.,,.")]
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
        SkinDef baseSkin = skinController.skins[currentIndex];

        float HSVsat = 0;
        float HSVvalue = 0;
        if (float.TryParse(args[0], out float HSVhue))
        {
            if (args.Count >= 1)
            {
                HSVhue = float.Parse(args[0]);
            }
        
            if (args.Count >= 2)
            {
                HSVsat = float.Parse(args[1]);
            }
        
            if (args.Count >= 3)
            {
                HSVvalue = float.Parse(args[2]);
            }
        }
        else
        {
            newSkinName = args[0];
            
            if (args.Count >= 2)
            {
                HSVhue = float.Parse(args[1]);
            }
        
            if (args.Count >= 3)
            {
                HSVsat = float.Parse(args[2]);
            }
        
            if (args.Count >= 4)
            {
                HSVvalue = float.Parse(args[3]);
            }
        }
        
        
        SkinDef replacementSkin = skinRecolor(baseSkin.name, args.senderBody.name, HSVhue, HSVsat, HSVvalue, "temp");

        Array.Resize(ref skinController.skins, skinController.skins.Length + 1);
        skinController.skins[^1] = replacementSkin;
        skinController.currentSkinIndex = skinController.skins.Length - 1;
        args.senderBody.skinIndex = (uint)(skinController.skins.Length - 1);
#pragma warning disable CS0618 // Type or member is obsolete
        skinController.ApplySkin(skinController.currentSkinIndex);
#pragma warning restore CS0618 // Type or member is obsolete
        
        baseSkinName = baseSkin;
        hsv[0] = HSVhue;
        hsv[1] = HSVsat;
        hsv[2] = value;
        currentBody = args.senderBody;
        
        Log.Debug("bwaa");
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

    private static ConfigEntry<string> skinRecolors;
    private static ConfigEntry<bool> enabled;
}