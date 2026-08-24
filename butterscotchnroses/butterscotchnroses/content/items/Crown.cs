using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using BNR;
using BNR.items;
using GoldenCoastPlusRevived.Buffs;
using R2API;
using RoR2;
using RoR2.Items;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using BuffBase = BNR.items.BuffBase;
using Object = System.Object;
using ShrineHealingBehavior = On.RoR2.ShrineHealingBehavior;

namespace BNR.items;

public class Crown : ItemBase<Crown>
{
    public override string ItemName => "crown";
    public override string ItemLangTokenName => "crown";
    public override string ItemPickupDesc => "";
    public override string ItemFullDescription => "";
    public override string ItemLore => "";
    public override ItemTier Tier => ItemTier.NoTier;
    public override GameObject ItemModel => butterscotchnroses.carvingKitBundle.LoadAsset<GameObject>("carvingkit.prefab");
    public override Sprite ItemIcon => butterscotchnroses.carvingKitBundle.LoadAsset<Sprite>("carvingkit.png");
    public override ItemTag[] ItemTags => [ItemTag.WorldUnique];
    
    public override void Init(ConfigFile config)
    {
        CreateConfig(config);
        CreateLang();
        CreateItem();
        Hooks();
        
        instance.ItemDef.hidden = true;
    }

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        return null;
    }
}