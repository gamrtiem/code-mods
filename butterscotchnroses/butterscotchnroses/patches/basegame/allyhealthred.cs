using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BNR.patches;
using BepInEx.Configuration;
using HarmonyLib;
using On.RoR2;
using Rewired.Utils;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using Color = UnityEngine.Color;
using HealthComponent = RoR2.HealthComponent;
using Object = UnityEngine.Object;

namespace BNR;

public class allyhealthred : PatchBase<allyhealthred>
{
    public static class IconController
    {
        public static List<allyObject> cardObjects = [];
        public static Stack addQueue = new();
        public static Stack removeQueue = new();

        public class allyObject(Material newHurtMat, HealthComponent newHealthComponent, Texture2D newIcon, GameObject newAllyCard)
        {
            public Material hurtMat = newHurtMat;
            public HealthComponent healthComponent = newHealthComponent;
            public GameObject allyCard = newAllyCard;
            public Texture2D icon = newIcon;
            public float prevHealth;
        }
        
        public static void addCard(HealthComponent newHealthComponent, Transform transform, Texture2D texture, GameObject component)
        {
            Log.Debug($"adding new to iconcontroller  !!!");
            
            if (texture == null)
            {
                Log.Warning("tried to add health overlay thing to a icon that didnt exist ,., (null ,..,. gorp ,.,,..");
                return;
            }

            allyObject existingIcon = cardObjects.FirstOrDefault(card => card.icon != texture);
            if (existingIcon != null)
            {
                Log.Debug("alreadys there with a different icon,..,. replacing !!");
                
                //update the icon if its changed ,.,. 
                removeQueue.Push(existingIcon);
            }
            
            GameObject newObject = new GameObject("healthoverlay");
            Image newImage = newObject.AddComponent<Image>();
            
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            Rect tRect = new Rect(0,0, texture.width, texture.height);
            Sprite spriteOveride = Sprite.Create(texture, tRect, pivot);
            newImage.sprite = spriteOveride;
            newImage.overrideSprite = spriteOveride;
            
            newImage.material = Object.Instantiate(hurtOverlayMat);
            newImage.UpdateMaterial();
            newObject.transform.SetParent(transform, false);
            
            //basically hide them behind elite overlays whenever those get around to being adde d,..,,. 
            int siblingIndex = 0;
            if (newObject.transform.Find("EliteOverlay"))
            {
                siblingIndex = 1;
            }
            newObject.transform.SetSiblingIndex(siblingIndex);
            
            newObject.GetComponent<RectTransform>().localScale = new Vector3(0.5f, 0.5f, 1);
            
            addQueue.Push(new allyObject(newImage.material, newHealthComponent, texture, component));
        }
    }
    
    
    public override void Init()
    {
        //RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Parent.matParentTeleportPortal_mat
        Addressables.LoadAssetAsync<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC3_SolusAmalgamator.matSolusAmalgamatorBombCoreExplode_mat).Completed += handle =>
        {
            hurtOverlayMat = Object.Instantiate(handle.Result);
            //hurtOverlayMat.SetVector(CutoffScroll, new Vector4(5, 0, 0, 0));
            hurtOverlayMat.SetColor(TintColor, new Color(1, 0, 0, 0));
            //hurtOverlayMat.SetFloat(AlphaBias, 0);
            
            //Addressables.LoadAssetAsync<Texture>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common.texRampDeathBomb_png).Completed += handle =>
            //{
            //    hurtOverlayMat.SetTexture(RemapTex, handle.Result);
            //};
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
            On.RoR2.UI.AllyCardController.UpdateInfo += AllyCardControllerOnUpdateInfo;
            On.RoR2.UI.ScoreboardStrip.EnterStrip += ScoreboardStripOnEnterStrip;
            On.RoR2.Stage.Start += StageOnStart;
            // on equip gained hook that iterates through card object health component body and if its there and equal and equip def has a dictionary entry add a elite overlay to it; equip removed same but get rid of it ,,.
        }
        else
        {
            On.RoR2.UI.AllyCardController.UpdateInfo -= AllyCardControllerOnUpdateInfo;
            On.RoR2.UI.ScoreboardStrip.EnterStrip -= ScoreboardStripOnEnterStrip;
            On.RoR2.Stage.Start -= StageOnStart;
        }
    }

    private IEnumerator StageOnStart(Stage.orig_Start orig, RoR2.Stage self)
    {
        IconController.cardObjects.Clear();
        
        return orig(self);
    }

    private static void AllyCardControllerOnUpdateInfo(On.RoR2.UI.AllyCardController.orig_UpdateInfo orig, RoR2.UI.AllyCardController self)
    {
        orig(self);
        
        IconController.addCard(self.cachedHealthComponent, self.portraitIconImage.transform, self.portraitIconImage.texture as Texture2D, self.gameObject);
    }
    
    private static void ScoreboardStripOnEnterStrip(On.RoR2.UI.ScoreboardStrip.orig_EnterStrip orig, RoR2.UI.ScoreboardStrip self)
    {
        //this hook currently doesnt work .,,. look into later !! 
        orig(self);
        
        IconController.addCard(self.master.GetBody().healthComponent, self.classIcon.transform, self.classIcon.texture as Texture2D, self.gameObject);
    }

    public override void FixedUpdate()
    {
        if (!enabled.Value) return;
        if (!RoR2.Run.instance) return;
        
        //performance debugging ,.,. start the stopwatch !!!
        //Stopwatch stopwatch = Stopwatch.StartNew();
        //stopwatch.Start();
        
        while (IconController.addQueue.Count != 0)
        {
            IconController.allyObject newCard = (IconController.allyObject)IconController.addQueue.Pop();
            
            IconController.cardObjects.Add(newCard);
        }
        
        foreach (IconController.allyObject card in IconController.cardObjects)
        {
            if (!card.allyCard)
            {
                Log.Debug("removing card !!");
                IconController.removeQueue.Push(card);
                Object.Destroy(card.hurtMat);
                continue;
            }

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (card.prevHealth != card.healthComponent.health)
            {
                Color current = card.hurtMat.GetColor(TintColor);
                current.a = 1 - card.healthComponent.healthFraction;
                current.a = Math.Clamp(current.a, 0, 1.5f); // this was used to ramp it more (a *= 1.5 but idk .,., 
                card.hurtMat.SetColor(TintColor, current);
                card.prevHealth = card.healthComponent.health;
            }
        }
        
        while (IconController.removeQueue.Count != 0)
        {
            IconController.cardObjects.Remove((IconController.allyObject)IconController.removeQueue.Pop());
        }

        //if (stopwatch.ElapsedTicks > 300)
        //{
        //    Log.Debug(stopwatch.ElapsedTicks + " ticks");
        //}
    }

    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("BNR - allyhealthred",
            "enable patches for allyhealthred",
            true,
            "");
        Utils.CheckboxConfig(enabled);
        enabled.SettingChanged += (_, _) =>
        {
            applyHooks();
        };
    }

    private static Material hurtOverlayMat;
    private static readonly int TintColor = Shader.PropertyToID("_TintColor"); // rider is telling me to do this <//3 ,.., 
    private ConfigEntry<bool> enabled;
}