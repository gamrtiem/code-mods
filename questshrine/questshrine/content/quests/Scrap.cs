//NETWORKING TODO
//sync scrapped state (send a packet when the component is done 
using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using questshrine.bases;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;
using static RoR2.ItemTier;
using Object = UnityEngine.Object;

namespace questshrine.content.quests;

public class Scrap : QuestBase<Scrap>
{
    public override string QuestName => "Scrap Item";
    public override string QuestTitle => "<style=cWorldEvent>the planet hungers .,,..</style>";
    public override string QuestDesc => "scrap a {0} item,.,.";
    public override string QuestDescRetired => "scrapped a {0} item !!!";
    public string questDescSpecific = "scrap a {0},.,.";
    public string questDescSpecificRetired => "scrapped a {0} !!!";
    public override Sprite QuestIcon => questshrine.bundle.LoadAsset<Sprite>("scrapper");
    public override string[] Tags => ["requireScrapper"];
    public override Type Behavior => typeof(ScrapItemBehaviorBase);
    
    public static ConfigEntry<bool> specificItem;

    public override void CreateConfig(ConfigFile config)
    {
        specificItem = config.Bind($"Quest Shrine - {QuestName}", 
            "Should the quest be a specific item to scrap .,.,", 
            false, 
            "");
        Utils.CheckboxConfig(specificItem);
    }

    public override void Hooks()
    {
        On.RoR2.ScrapperController.BeginScrapping_UniquePickup += QuestScrapper;
    }

    private void QuestScrapper(On.RoR2.ScrapperController.orig_BeginScrapping_UniquePickup orig, ScrapperController self, UniquePickup pickuptotake)
    {
        CharacterMaster interactorMaster = self.interactor?.gameObject.GetComponent<CharacterBody>()?.master;
        if (!interactorMaster || !NetworkServer.active)
        {
            orig(self, pickuptotake);
            return;
        }
        
        QuestBehaviorBase[] scrapQuests = QuestBehaviorBase.activeQuests.Where(quest => quest.charMaster == interactorMaster && quest is ScrapItemBehaviorBase).ToArray();
        if (scrapQuests.Length == 0)
        {
            orig(self, pickuptotake);
            return;
        }

        foreach (QuestBehaviorBase questBehaviorBase in scrapQuests)
        {
            ScrapItemBehaviorBase scrapQuest = (ScrapItemBehaviorBase)questBehaviorBase;
            if (specificItem.Value && scrapQuest.targetItemIndex != (int)ItemIndex.None)
            {
                if ((ItemIndex)scrapQuest.targetItemIndex != PickupCatalog.GetPickupDef(pickuptotake.pickupIndex)!.itemIndex)
                    continue;
                
            }
            else
            {
                if ((ItemTier)scrapQuest.targetTier != PickupCatalog.GetPickupDef(pickuptotake.pickupIndex)!.itemTier)
                    continue;
            }

            GiveReward(scrapQuest.body);
            scrapQuest.RpcRetire();

            int prevMaxScraps = self.maxItemsToScrapAtATime;
            self.maxItemsToScrapAtATime = 1;
            orig(self, pickuptotake);
            self.maxItemsToScrapAtATime = prevMaxScraps;
            return;
        }
        
        orig(self, pickuptotake);
    }
}

public class ScrapItemBehaviorBase : QuestBehaviorBase
{
    public override QuestBase QuestBase => Scrap.instance;
    public override Type ObjectiveType => typeof(ScrapItemObjective);
    
    [SyncVar]
    public int targetTier;
    [SyncVar]
    public int targetItemIndex = -1;
    
    public override void StartQuest()
    {
        if (!NetworkServer.active)
        {
            base.StartQuest();
            return;
        }
        
        BasicPickupDropTable dropTable = ScriptableObject.CreateInstance<BasicPickupDropTable>();
        dropTable.tier1Weight = 0f;
        dropTable.tier2Weight = 0f;
        dropTable.tier3Weight = 0f;

        ReadOnlySpan<ItemIndex> spanStacks = body.inventory.effectiveItemStacks.GetNonZeroIndicesSpan();
        foreach (ItemIndex index in spanStacks)
        {
            ItemDef itemDef = ItemCatalog.GetItemDef(index);
            ItemTier tier = ItemCatalog.GetItemDef(index).tier;
            if (!itemDef.canRemove || itemDef.hidden || itemDef.ContainsTag(ItemTag.Scrap) || itemDef.ContainsTag(ItemTag.CannotCopy)) continue;
            switch (tier)
            {
                case Tier1:
                    //Log.Debug($"had item {itemDef.name} with tier {tier}");
                    dropTable.tier1Weight = 0.8f;
                    break;
                case Tier2:
                    //Log.Debug($"had item {itemDef.name} with tier {tier}");
                    dropTable.tier2Weight = 0.2f;
                    break;
                case Tier3:
                    //Log.Debug($"had item {itemDef.name} with tier {tier}");
                    dropTable.tier3Weight = 0.01f;
                    break;
            }
        }
        Log.Debug($"weights {dropTable.tier1Weight} {dropTable.tier2Weight} {dropTable.tier3Weight}");

        if (dropTable.tier1Weight == 0 && dropTable.tier2Weight == 0 && dropTable.tier3Weight == 0)
        {
            targetTier = (int)Tier1;
        }
        else
        {
            targetTier = (int)PickupCatalog.GetPickupDef(dropTable.GeneratePickup(Run.instance.runRNG).pickupIndex)!.itemTier;
        }

        if (Scrap.specificItem.Value)
        {
            WeightedSelection<ItemIndex> indexSelection = new WeightedSelection<ItemIndex>();
            
            bool availchoice = false;
            ReadOnlySpan<ItemIndex> span = body.inventory.effectiveItemStacks.GetNonZeroIndicesSpan();
            foreach (ItemIndex index in span)
            {
                ItemDef itemDef = ItemCatalog.GetItemDef(index);
                ItemTier tier = ItemCatalog.GetItemDef(index).tier;
                if (tier is not Tier1 and not Tier2 and not Tier3) continue;
                if (!itemDef.canRemove || itemDef.hidden || !itemDef.DoesNotContainTag(ItemTag.Scrap) || !itemDef.DoesNotContainTag(ItemTag.CannotCopy)) continue;
                
                int weight = tier switch
                {
                    Tier1 => 80,
                    Tier2 => 20,
                    Tier3 => 1,
                };
                indexSelection.AddChoice(index, weight);
                availchoice = true;
            }

            Log.Debug($"choice length {indexSelection.choices.Length}");
            if (availchoice)
            {
                targetItemIndex = (int)indexSelection.Evaluate(Run.instance.treasureRng.nextNormalizedFloat);
            }
        }

        string tierName = targetTier switch
        {
            (int)Tier1 => "Common",
            (int)Tier2 => "Uncommon",
            (int)Tier3 => "Legendary",
        };
        
        QuestDescInternal = string.Format(Scrap.instance.QuestDesc, tierName);
        if (Scrap.specificItem.Value && targetItemIndex != (int)ItemIndex.None)
        {
            QuestDescInternal = string.Format(Scrap.instance.questDescSpecific, Language.GetString(ItemCatalog.GetItemDef((ItemIndex)targetItemIndex).nameToken));
        }
        
        base.StartQuest();
    }
}

public class ScrapItemObjective : QuestObjectiveBase
{
    private ScrapItemBehaviorBase scrapQuest;
        
    public override string GenerateString()
    {
        if (!scrapQuest)
        {
            scrapQuest = (ScrapItemBehaviorBase)sourceDescriptor.source;
            questBehavior = scrapQuest;
        }

        string tierName = scrapQuest.targetTier switch
        {
            (int)Tier1 => "Common",
            (int)Tier2 => "Uncommon",
            (int)Tier3 => "Legendary",
        };
        string text = string.Format(Scrap.instance.QuestDesc, tierName);
            
        if (Scrap.specificItem.Value && scrapQuest.targetItemIndex != (int)ItemIndex.None)
        {
            text = string.Format(Scrap.instance.questDescSpecific, Language.GetString(ItemCatalog.GetItemDef((ItemIndex)scrapQuest.targetItemIndex).nameToken));
        }

        if (!scrapQuest.enabled)
        {
            retired = true;
            
            text = string.Format(Scrap.instance.QuestDescRetired, tierName);
            if (Scrap.specificItem.Value && scrapQuest.targetItemIndex != (int)ItemIndex.None)
            {
                text = string.Format(Scrap.instance.questDescSpecificRetired, Language.GetString(ItemCatalog.GetItemDef((ItemIndex)scrapQuest.targetItemIndex).nameToken));
            }
            
            Object.Destroy(scrapQuest);
        }
        return GetPlayerName() + text;
    }

    public override bool IsDirty()
    {
        return true;
    }
}