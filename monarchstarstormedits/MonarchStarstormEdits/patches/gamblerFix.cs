using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;
using RoR2;
using RoR2.ExpansionManagement;
using SS2;

namespace MonarchStarstormEdits.patches;

public class gamblerFix : PatchBase<gamblerFix>
{
    [HarmonyPatch]
        public class Starstorm2GamblerFix
        {
            [HarmonyPatch(typeof(SS2.RewardDropper), "GeneratePickups")]
            [HarmonyPrefix]
            public static bool PickupPostFix(RewardDropper __instance)
            {
                if (injectorToCoupler.Value && Run.instance.IsExpansionEnabled(UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<ExpansionDef>("RoR2/DLC3/DLC3.asset").WaitForCompletion()))
                {
                    foreach (var pickupIndex in __instance.reward.pickups)
                    {
                        if (PickupCatalog.GetPickupDef(pickupIndex).equipmentIndex == EquipmentIndex.None)
                        {
                            Log.Debug("not equipmnet ,..,");
                            continue;
                        }
                    
                        Log.Debug("adding extra equip item to reward !!");
                        Array.Resize(ref __instance.reward.pickups, __instance.reward.pickups.Length + 1);
                        __instance.reward.pickups[^1] = PickupCatalog.FindPickupIndex(DLC3Content.Items.ExtraEquipment.itemIndex);
                    }
                }
                else
                {
                    Log.Debug("wow ,.., okay ,.,.");
                }
                
                __instance.pickupQueue = new Queue<RewardDropper.RewardInfo>();
                PickupIndex[] guaranteedPickups = __instance.reward.pickups;
                for(int i = 0; i < guaranteedPickups.Length; i++)
                {
                    __instance.pickupQueue.Enqueue(new RewardDropper.RewardInfo { pickupIndex = guaranteedPickups[i] });
                }

                for(int i = 0; i < __instance.reward.white; i++)
                {
                    PickupIndex pickupIndex = __instance.rng.NextElementUniform<PickupIndex>(Run.instance.availableTier1DropList);
                    __instance.pickupQueue.Enqueue(new RewardDropper.RewardInfo { pickupIndex = pickupIndex });
                }
                for (int i = 0; i < __instance.reward.whiteOption; i++)
                {
                    List<UniquePickup> pickups = new List<UniquePickup>();
                    RewardDropper.dtTier1.GenerateDistinctPickups(pickups, 3, __instance.rng);
                    __instance.pickupQueue.Enqueue(new RewardDropper.RewardInfo { prefabOverride = RewardDropper.optionPrefab, options = PickupPickerController.GenerateOptionsFromList(pickups), pickupIndex = PickupCatalog.FindPickupIndex(ItemTier.Tier1) });
                }
                for(int i = 0; i < __instance.reward.whiteCommand; i++)
                {
                    __instance.pickupQueue.Enqueue(new RewardDropper.RewardInfo { pickupIndex = Run.instance.availableTier1DropList[0], flag = GenericPickupController.PickupArtifactFlag.COMMAND });
                }

                for (int i = 0; i < __instance.reward.green; i++)
                {
                    PickupIndex pickupIndex = __instance.rng.NextElementUniform<PickupIndex>(Run.instance.availableTier2DropList);
                    __instance.pickupQueue.Enqueue(new RewardDropper.RewardInfo { pickupIndex = pickupIndex });
                }
                for (int i = 0; i < __instance.reward.greenOption; i++)
                {
                    List<UniquePickup> pickups = new List<UniquePickup>();
                    RewardDropper.dtTier2.GenerateDistinctPickups(pickups, 3, __instance.rng);
                    __instance.pickupQueue.Enqueue(new RewardDropper.RewardInfo { prefabOverride = RewardDropper.optionPrefab, options = PickupPickerController.GenerateOptionsFromList(pickups), pickupIndex = PickupCatalog.FindPickupIndex(ItemTier.Tier2) });
                }
                for (int i = 0; i < __instance.reward.greenCommand; i++)
                {
                    __instance.pickupQueue.Enqueue(new RewardDropper.RewardInfo { pickupIndex = Run.instance.availableTier2DropList[0], flag = GenericPickupController.PickupArtifactFlag.COMMAND });
                }

                for (int i = 0; i < __instance.reward.red; i++)
                {
                    PickupIndex pickupIndex = __instance.rng.NextElementUniform<PickupIndex>(Run.instance.availableTier3DropList);
                    __instance.pickupQueue.Enqueue(new RewardDropper.RewardInfo { pickupIndex = pickupIndex });
                }
                for (int i = 0; i < __instance.reward.redOption; i++)
                {
                    List<UniquePickup> pickups = new List<UniquePickup>();
                    RewardDropper.dtTier3.GenerateDistinctPickups(pickups, 3, __instance.rng);
                    __instance.pickupQueue.Enqueue(new RewardDropper.RewardInfo { prefabOverride = RewardDropper.optionPrefab, options = PickupPickerController.GenerateOptionsFromList(pickups), pickupIndex = PickupCatalog.FindPickupIndex(ItemTier.Tier3) });
                }
                for (int i = 0; i < __instance.reward.redCommand; i++)
                {
                    __instance.pickupQueue.Enqueue(new RewardDropper.RewardInfo { pickupIndex = Run.instance.availableTier3DropList[0], flag = GenericPickupController.PickupArtifactFlag.COMMAND });
                }
                
                return false;
            }
        }

    public override void Init(Harmony harmony)
    {
        if (!gamblerFixes.Value) return;
        harmony.CreateClassProcessor(typeof(Starstorm2GamblerFix)).Patch();
    }

    public override void Config(ConfigFile config)
    {
        gamblerFixes = config.Bind("lunar gambler fix s,s,.,.", 
            "try to fix lunar gambler errors when it gives void potentials!!! disable if its fixed already .,.,", 
            true,
            "byeah ,.,");
        injectorToCoupler = config.Bind("lunar gambler fix s,s,.,.", 
            "add a functional coupler (if dlc3 enabled( to rewards with equipments (ones that used to have composite injector .,.,", 
            true,
            "byeah ,.,");
    }
    
    private static ConfigEntry<bool> gamblerFixes;
    private static ConfigEntry<bool> injectorToCoupler;
}