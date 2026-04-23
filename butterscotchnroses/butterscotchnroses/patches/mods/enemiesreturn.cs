using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BNR.patches;
using BepInEx.Configuration;
using HarmonyLib;
using HG;
using PhotoMode;
using R2API;
using Rewired;
using RiskOfOptions;
using RiskOfOptions.Options;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Items;
using Unity.Hierarchy;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Inventory = On.RoR2.Inventory;
using Object = System.Object;
using SkinCatalog = IL.RoR2.SkinCatalog;

namespace BNR;

public class enemiesreturn : PatchBase<enemiesreturn>
{
    private static readonly int MainTex = Shader.PropertyToID("_MainTex");
    private static readonly int EmTex = Shader.PropertyToID("_EmTex");
    private static readonly int EmColor = Shader.PropertyToID("_EmColor");
    private static HashSet<SkinDef> skinDefs = new HashSet<SkinDef>();
    private static ItemDef annointedItemDef;
    private static Material replacedCrown;

    public override void Init(Harmony harmony)
    {
        applyHooks();
    }
    
    private void applyHooks()
    {
        if (enabled.Value)
        {
            //BodyCatalog.availability.CallWhenAvailable(AddDrifterBodyLiTDrifterSkin);
            //RoR2.RoR2Application.onLoad += AddDrifterBodyLiTDrifterSkin;
            BodyCatalog.availability.onAvailable += AddDrifterBodyLiTDrifterSkin;
            RoR2.CharacterBody.onBodyStartGlobal += CharacterBodyOnonBodyStartGlobal;
            ItemCatalog.availability.onAvailable += () => { annointedItemDef = ItemCatalog.GetItemDef(ItemCatalog.FindItemIndex("HiddenAnointed")); };
        }
        else
        {

        }
    }

    public sealed class HiddenAnointedBehavior : BaseItemBodyBehavior
    {
        [ItemDefAssociation]
        private static ItemDef GetItemDef() => annointedItemDef;

        private bool runOnce = false;
        private ModelSkinController modelSkinController;
        private void OnEnable()
        {
        }
        
        private void OnDisable()
        {
        }

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
                    replacedCrown.SetTexture(MainTex, BNRUtils.hueShiftTexture(replacedCrown.GetTexture(MainTex) as Texture2D, 100f));
                    replacedCrown.SetTexture(EmTex, BNRUtils.hueShiftTexture(replacedCrown.GetTexture(EmTex) as Texture2D, 100f));
                    replacedCrown.SetColor(EmColor, BNRUtils.Color255(255, 195, 185));
                }
                
                display.GetComponent<ItemDisplay>().rendererInfos[0].defaultMaterial = replacedCrown;
                crownRenderer.material = replacedCrown;
            }

            
            runOnce = true;
        }
    }
    

    //stolen from enemies return :3 ., ,.
    private void CharacterBodyOnonBodyStartGlobal(CharacterBody body)
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

    public override void FixedUpdate()
    {
        if (Input.GetKeyUp(KeyCode.F7))
        {
            AddDrifterBodyLiTDrifterSkin();
        }

    }

    private void AddDrifterBodyLiTDrifterSkin()
    {
        var bodyName = "CommandoBody";
        var skinName = "testSkin";
        try
        {
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
            
            
            
            foreach (SkinDef skinDefs in skinController.skins)
            {
                Log.Info(skinDefs.name);
            }
            Log.Debug("adding !!");
            SkinDef skinDef = UnityEngine.Object.Instantiate(skinController.skins.First(skindef => skindef.name == "skinCommandoJudgementHidden"));
            
            //recolor
            Texture2D newIcon = BNRUtils.hueShiftTexture(skinDef.icon.texture, 100f);
            Sprite newIconSprite = Sprite.Create(newIcon, new Rect(0, 0, newIcon.width, newIcon.height), new Vector2(newIcon.width / 2, newIcon.height / 2));
            skinDef.icon = newIconSprite;
            
            //////////////////////////////////////////

            Material newMat = UnityEngine.Object.Instantiate(skinDef.skinDefParams.rendererInfos[0].defaultMaterial);
            newMat.SetTexture(MainTex, BNRUtils.hueShiftTexture(newMat.GetTexture(MainTex) as Texture2D, 100f));
            newMat.SetTexture(EmTex, BNRUtils.hueShiftTexture(newMat.GetTexture(EmTex) as Texture2D, 100f));
            newMat.SetColor(EmColor, BNRUtils.Color255(255, 0, 185));
            
            var newParams = UnityEngine.Object.Instantiate(skinDef.skinDefParams);
            for (int i = 0; i < newParams.rendererInfos.Length; i++)
            {
                newParams.rendererInfos[i].defaultMaterial = newMat;
            }
            skinDef.skinDefParams = newParams;
            //
            
            skinDef.name = skinName;
            skinDef.nameToken = "sillyiceskin";
            
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
        BNRUtils.CheckboxConfig(enabled);
        enabled.SettingChanged += (_, _) =>
        {
            applyHooks();
        };
    }
    
    private ConfigEntry<bool> enabled;
}