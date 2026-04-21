using System;
using System.Collections;
using System.Collections.Generic;
using BNR.patches;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using Color = UnityEngine.Color;
using HealthComponent = RoR2.HealthComponent;
using Object = UnityEngine.Object;

namespace BNR;

public class allyhealthred : PatchBase<allyhealthred>
{
    private static class IconController
    {
        public static List<allyObject> cardObjects = [];
        public static Stack addQueue = new();
        public static Stack removeQueue = new();

        internal class allyObject(Material newHurtMat, HealthComponent newHealthComponent, RoR2.UI.AllyCardController newAllyCard)
        {
            public Material hurtMat = newHurtMat;
            public HealthComponent healthComponent = newHealthComponent;
            public RoR2.UI.AllyCardController allyCard = newAllyCard;
        }
    }
    
    
    public override void Init(Harmony harmony)
    {
        Addressables.LoadAssetAsync<Material>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Parent.matParentTeleportPortal_mat).Completed += handle =>
        {
            hurtOverlayMat = Object.Instantiate(handle.Result);
            hurtOverlayMat.SetVector(CutoffScroll, new Vector4(5, 0, 0, 0));
            hurtOverlayMat.SetColor(TintColor, new Color(1, 0, 0, 0));
            hurtOverlayMat.SetFloat(AlphaBias, 0);
            
            Addressables.LoadAssetAsync<Texture>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common.texRampDeathBomb_png).Completed += handle =>
            {
                hurtOverlayMat.SetTexture(RemapTex, handle.Result);
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

    private static void AllyCardControllerOnUpdateInfo(On.RoR2.UI.AllyCardController.orig_UpdateInfo orig, RoR2.UI.AllyCardController self)
    {
        orig(self);

        if (true)
        {
            Log.Debug($"adding new !!");
            GameObject newObject = new GameObject("healthoverlay");
            Image newImage = newObject.AddComponent<Image>();
        
            Texture2D realIcon = (self.portraitIconImage.texture as Texture2D);
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            Rect tRect = new Rect(0,0, realIcon.width, realIcon.height);
            Sprite spriteOveride = Sprite.Create( realIcon, tRect, pivot);
            
            newImage.sprite = spriteOveride;
            newImage.overrideSprite = spriteOveride;
            
            newImage.material = Object.Instantiate(hurtOverlayMat);
            newImage.UpdateMaterial();
            newObject.transform.SetParent(self.portraitIconImage.transform, false);
            newObject.transform.SetSiblingIndex(0);
            newObject.GetComponent<RectTransform>().localScale = new Vector3(0.5f, 0.5f, 1);
            
            IconController.addQueue.Push(new IconController.allyObject(newImage.material, self.cachedHealthComponent, self));
        }
    }

    public override void FixedUpdate()
    {
        if (!enabled.Value) return;

        while (IconController.addQueue.Count != 0)
        {
            IconController.allyObject newCard = (IconController.allyObject)IconController.addQueue.Pop();
            
            //allyObject existingCard = IconController.cardObjects.Find(allyCard => allyCard.allyCard == newCard.allyCard);
            
            //incase ally card exists (i dunno why i coded this it shouldnt be necessary .,,.
            //if (existingCard != null)
            //{
            //    IconController.cardObjects.Remove(existingCard);
            //}
            
            IconController.cardObjects.Add(newCard);
        }
        
        foreach (var card in IconController.cardObjects)
        {
            if (!card.allyCard)
            {
                IconController.removeQueue.Push(card);
                Object.Destroy(card.hurtMat);
                continue;
            }
            
            Color current = card.hurtMat.GetColor(TintColor);
            current.a = 1 - card.healthComponent.healthFraction;
            current.a *= 2;
            current.a = Math.Clamp(current.a, 0, 1.5f);
            card.hurtMat.SetColor(TintColor, current);
        }
        
        while (IconController.removeQueue.Count != 0)
        {
            IconController.cardObjects.Remove((IconController.allyObject)IconController.removeQueue.Pop());
        }
    }
    
    private static void ScoreboardStripOnEnterStrip(On.RoR2.UI.ScoreboardStrip.orig_EnterStrip orig, RoR2.UI.ScoreboardStrip self)
    {
        orig(self);
        
        //add them here too
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

    private static Material hurtOverlayMat;
    private static readonly int TintColor = Shader.PropertyToID("_TintColor"); // rider is telling me to do this <//3 ,.., 
    private static readonly int CutoffScroll = Shader.PropertyToID("_CutoffScroll");
    private static readonly int AlphaBias = Shader.PropertyToID("_AlphaBias");
    private static readonly int RemapTex = Shader.PropertyToID("_RemapTex"); 
    private ConfigEntry<bool> enabled;
}