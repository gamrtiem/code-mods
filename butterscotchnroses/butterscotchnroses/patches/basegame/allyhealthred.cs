using System;
using System.Collections.Generic;
using System.Linq;
using BNR.patches;
using BepInEx.Configuration;
using HarmonyLib;
using On.RoR2;
using On.RoR2.UI;
using R2API;
using RiskOfOptions;
using RiskOfOptions.Options;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using HealthComponent = RoR2.HealthComponent;
using Object = UnityEngine.Object;

namespace BNR;

public class allyhealthred : PatchBase<allyhealthred>
{
    public static class IconController
    {
        public static Dictionary<Material, RoR2.HealthComponent> cards = [];
    }

    public static Material hurtOverlayMat;
    
    public override void Init(Harmony harmony)
    {
        Addressables.LoadAssetAsync<Material>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Parent.matParentTeleportPortal_mat).Completed += handle =>
        {
            hurtOverlayMat = Object.Instantiate(handle.Result);
            hurtOverlayMat.SetVector("_CutoffScroll", new Vector4(5, 0, 0, 0));
            hurtOverlayMat.SetColor("_TintColor", new Color(1, 1, 1, 0));
            hurtOverlayMat.SetFloat("_AlphaBias", 0);
            
            Addressables.LoadAssetAsync<Texture>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common.texRampDeathBomb_png).Completed += handle =>
            {
                hurtOverlayMat.SetTexture("_RemapTex", handle.Result);
            };
        };
        
        applyHooks();
        //matParentTeleportPortal
        //texRampDeathBomb
        
        //cutoff scroll speed -> 5
    }

    private void applyHooks()
    {
        if (enabled.Value)
        {
            //On.RoR2.UI.AllyCardController.Awake += AllyCardControllerOnAwake;
            On.RoR2.UI.AllyCardController.UpdateInfo += AllyCardControllerOnUpdateInfo;
            On.RoR2.UI.ScoreboardStrip.EnterStrip += ScoreboardStripOnEnterStrip;
        }
        else
        {
            On.RoR2.UI.AllyCardController.UpdateInfo -= AllyCardControllerOnUpdateInfo;
            On.RoR2.UI.ScoreboardStrip.EnterStrip -= ScoreboardStripOnEnterStrip;
        }
    }

    private void AllyCardControllerOnUpdateInfo(AllyCardController.orig_UpdateInfo orig, RoR2.UI.AllyCardController self)
    {
        orig(self);
        
        Log.Debug("bwa");


        if (true)
        {
            GameObject newObject = new GameObject("healthoverlay");
            Image newImage = newObject.AddComponent<Image>();
        
            Log.Debug($"texture null ? {self.portraitIconImage.texture == null}");
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            Texture2D realIcon = (self.portraitIconImage.texture as Texture2D);
            Log.Debug($"silly nre ? {realIcon == null}");
            Rect tRect = new Rect(0,0, realIcon.width, realIcon.height);
            Sprite spriteOveride = Sprite.Create( realIcon, tRect, pivot);
            Log.Debug($"sprite override ? {spriteOveride}");
            
            newImage.sprite = spriteOveride;
            newImage.overrideSprite = spriteOveride;
            
            newObject.GetComponent<RectTransform>().localScale = new Vector3(0.5f, 0.5f, 1);
            
            newImage.material = Object.Instantiate(hurtOverlayMat);
            newImage.UpdateMaterial();
            newObject.transform.SetParent(self.portraitIconImage.transform, false);
            newObject.transform.SetSiblingIndex(0);
            
            IconController.cards.Add(newImage.material, self.cachedHealthComponent);
        }
    }

    public override void FixedUpdate()
    {
        if (!enabled.Value) return;
        
        foreach ((Material icon, HealthComponent health) in IconController.cards.ToArray())
        {
            if (icon != null && health != null)
            {
                Color current = icon.GetColor("_TintColor");
                current.a = 1 - health.healthFraction;
                current.a *= 2;
                current.a = Math.Clamp(current.a, 0, 1);
                icon.SetColor("_TintColor", current);
                //icon.material.mainTexture = icon.material.mainTexture;
            }
            else
            { 
                IconController.cards.Remove(icon);
                
                if (health == null)
                {
                    Object.Destroy(icon);
                }

            }
        }
    }
    
    private void ScoreboardStripOnEnterStrip(ScoreboardStrip.orig_EnterStrip orig, RoR2.UI.ScoreboardStrip self)
    {
        orig(self);
        
    }

    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("BNR - allyhealthred",
            "enable patches for allyhealthred",
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