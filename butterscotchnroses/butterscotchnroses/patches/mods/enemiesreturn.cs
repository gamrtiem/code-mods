using System;
using System.Collections.Generic;
using System.Linq;
using BNR.patches;
using BepInEx.Configuration;
using HarmonyLib;
using R2API;
using RoR2;
using RoR2.Items;
using UnityEngine;
using UnityEngine.Networking;

// ReSharper disable Unity.PerformanceCriticalCodeInvocation

namespace BNR;

public class enemiesreturn : PatchBase<enemiesreturn>
{
    public override string chainLoaderKey => "com.Viliger.EnemiesReturns";

    private static readonly int MainTex = Shader.PropertyToID("_MainTex");
    private static readonly int EmTex = Shader.PropertyToID("_EmTex");
    private static readonly int EmColor = Shader.PropertyToID("_EmColor");
    private static HashSet<SkinDef> skinDefs = [];
    private static ItemDef annointedItemDef;
    private static Material replacedCrown;

    public override void Init()
    {
        applyHooks();
    }
    
    private void applyHooks()
    {
        if (enabled.Value)
        {
            BodyCatalog.availability.onAvailable += AddAnointedPink;
            CharacterBody.onBodyStartGlobal += CharacterBodyOnonBodyStartGlobal;
            ItemCatalog.availability.onAvailable += () => { annointedItemDef = ItemCatalog.GetItemDef(ItemCatalog.FindItemIndex("HiddenAnointed")); };
        }
        else
        {
            BodyCatalog.availability.onAvailable += AddAnointedPink;
            CharacterBody.onBodyStartGlobal += CharacterBodyOnonBodyStartGlobal;
        }
    }

    public sealed class HiddenAnointedBehavior : BaseItemBodyBehavior
    {
        [ItemDefAssociation]
        private static ItemDef GetItemDef() => annointedItemDef;

        private bool runOnce;
        private ModelSkinController modelSkinController;

        private void FixedUpdate()
        {
            // on enable ,.., too early ,.,. 
            if (runOnce) return;
            
            modelSkinController = body.modelLocator.modelTransform.GetComponent<ModelSkinController>();
            if (!modelSkinController) return;

            if (body.skinIndex >= modelSkinController.skins.Length) return;

            var skin = modelSkinController.skins[body.skinIndex];
            if (skinDefs.Contains(skin))
            { 
                GameObject display = body.gameObject.GetComponent<ModelLocator>()._modelTransform.gameObject.GetComponent<ChildLocator>().FindChild("Head").transform.Find("DisplayAeonian(Clone)").gameObject;
                MeshRenderer crownRenderer = display.transform.Find("ArraignCrown").GetComponent<MeshRenderer>();

                if (!replacedCrown)
                {
                    replacedCrown = UnityEngine.Object.Instantiate(crownRenderer.material);
                    replacedCrown.SetTexture(MainTex, Utils.hsvModifyTexture(replacedCrown.GetTexture(MainTex) as Texture2D, hue.Value, saturation.Value, value.Value));
                    replacedCrown.SetTexture(EmTex, Utils.hsvModifyTexture(replacedCrown.GetTexture(EmTex) as Texture2D, hue.Value, saturation.Value, value.Value));
                    replacedCrown.SetColor(EmColor, Utils.Color255(255, 195, 185));
                }
                
                display.GetComponent<ItemDisplay>().rendererInfos[0].defaultMaterial = replacedCrown;
                crownRenderer.material = replacedCrown;
            }
            
            runOnce = true;
        }
    }
    

    //stolen from enemies return :3 ., ,.
    private static void CharacterBodyOnonBodyStartGlobal(CharacterBody body)
    {
        if (!NetworkServer.active) return;
        if (!body.isPlayerControlled) return;
    
        if (body.inventory.GetItemCountPermanent(annointedItemDef) > 0) return;
        if (!body.modelLocator || !body.modelLocator.modelTransform) return;

        var modelSkinController = body.modelLocator.modelTransform.GetComponent<ModelSkinController>();
        if (!modelSkinController) return;

        if (body.skinIndex >= modelSkinController.skins.Length) return;
 
        var skin = modelSkinController.skins[body.skinIndex];
        if (skinDefs.Contains(skin))
        {
            body.inventory.GiveItemPermanent(annointedItemDef);
        }
    }

    private static void AddAnointedPink()
    {
        string bodyName = "CommandoBody";
        string skinName = "testSkin";
        
        try
        {
            //stolen from keb skin builder script ,.,.
            var bodyPrefab = BodyCatalog.FindBodyPrefab(bodyName);
            if (!bodyPrefab)
            {
                Log.Warning($"Failed to add \"{skinName}\" skin because \"{bodyName}\" doesn't exist");
                return;
            }

            var modelLocator = bodyPrefab.GetComponent<ModelLocator>();
            if (!modelLocator)
            {
                Log.Warning($"Failed to add \"{skinName}\" skin to \"{bodyName}\" because it doesn't have \"ModelLocator\" component");
                return;
            }

            var mdl = modelLocator.modelTransform.gameObject;
            var skinController = mdl ? mdl.GetComponent<ModelSkinController>() : null;
            if (!skinController)
            {
                Log.Warning($"Failed to add \"{skinName}\" skin to \"{bodyName}\" because it doesn't have \"ModelSkinController\" component");
                return;
            }
            
            foreach (SkinDef skinControllerSkinDef in skinController.skins)
            {
                Log.Info(skinControllerSkinDef.name);
            }
            Log.Debug("adding !");
            SkinDef skinDef = UnityEngine.Object.Instantiate(skinController.skins.First(skindef => skindef.name == "skinCommandoJudgementHidden"));
            
            //recolor
            Texture2D newIcon = Utils.hsvModifyTexture(skinDef.icon.texture, hue.Value, saturation.Value, value.Value);
            Sprite newIconSprite = Sprite.Create(newIcon, new Rect(0, 0, newIcon.width, newIcon.height), new Vector2(newIcon.width / 2, newIcon.height / 2));
            skinDef.icon = newIconSprite;
            
            //////////////////////////////////////////

            Material newMat = UnityEngine.Object.Instantiate(skinDef.skinDefParams.rendererInfos[0].defaultMaterial);
            newMat.SetTexture(MainTex, Utils.hsvModifyTexture(newMat.GetTexture(MainTex) as Texture2D, hue.Value, saturation.Value, value.Value));
            newMat.SetTexture(EmTex, Utils.hsvModifyTexture(newMat.GetTexture(EmTex) as Texture2D, hue.Value, saturation.Value, value.Value));
            newMat.SetColor(EmColor, Utils.Color255(255, 0, 185));
            
            var newParams = UnityEngine.Object.Instantiate(skinDef.skinDefParams);
            for (int i = 0; i < newParams.rendererInfos.Length; i++)
            {
                newParams.rendererInfos[i].defaultMaterial = newMat;
                Log.Debug($"defualt address = {newParams.rendererInfos[i].defaultMaterialAddress}");
            }
            skinDef.skinDefParams = newParams;
            //
            
            skinDef.name = skinName;
            skinDef.nameToken += "_BNR";
            LanguageAPI.Add(skinDef.nameToken, "Annointed Pink");
            
            Array.Resize(ref skinController.skins, skinController.skins.Length + 1);
            skinController.skins[^1] = skinDef;
            Log.Debug($"added {skinDef} to commando ");
            skinDefs.Add(skinDef);
        }
        catch (Exception e)
        {
            Log.Warning($"Failed to add \"{skinName}\" skin to \"{bodyName}\"");
            Log.Error(e);
        }
    }
    
    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("BNR - enemiesreturn",
            "enable patches for enemiesreturn",
            true,
            "");
        Utils.CheckboxConfig(enabled);
        enabled.SettingChanged += (_, _) =>
        {
            applyHooks();
        };
        
        addAnointed = config.Bind("BNR - enemiesreturn",
            "add anointed pink skin !!",
            true,
            "enables commando pink annointed .,,. probablys doesnt show if you dont have original judgement unlocked ,....,.");
        Utils.CheckboxConfig(addAnointed);
        
        hue = config.Bind("BNR - enemiesreturn",
            "pink anointed skin hue ,...",
            100f,
            "pink anointed hue ,.,.");
        Utils.SliderConfig(0f, 360f, hue);
        
        saturation = config.Bind("BNR - enemiesreturn",
            "pink anointed skin saturation ,.,,",
            0f,
            "pink anointed saturation ,.,.");
        Utils.SliderConfig(0f, 100f, saturation);
        
        value = config.Bind("BNR - enemiesreturn",
            "pink anointed skin value ,..,",
            0f,
            "pink anointed value ,.,.");
        Utils.SliderConfig(0f, 100f, value);
    }
    
    private static ConfigEntry<float> hue;
    private static ConfigEntry<float> saturation;
    private static ConfigEntry<float> value;
    private static ConfigEntry<bool> addAnointed;
    private static ConfigEntry<bool> enabled;
}