using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using EntityStates.Executioner2;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.ExpansionManagement;
using SS2;
using UnityEngine;

namespace MonarchStarstormEdits.patches;

public class gamblerTweaks : PatchBase<gamblerTweaks>
{
    [HarmonyPatch]
    public class Starstorm2GamblerTweaks
    {
        [HarmonyPatch(typeof(CurseManager), "OnBodyStartGlobal")]
        [HarmonyILManipulator]
        public static void OnEnterPostFix(ILContext il)
        {
            if (baseCloakDuration.Value == 0.0f) return;
            
            Log.Debug("tryings il hook curse cloak unevilify ,,.");
            var c = new ILCursor(il);

            /*
            //characterBody.AddBuff(RoR2Content.Buffs.Cloak);
            IL_0124: ldarg.0
            IL_0125: ldsfld class [RoR2]RoR2.BuffDef [RoR2]RoR2.RoR2Content/Buffs::Cloak
            IL_012a: callvirt instance void [RoR2]RoR2.CharacterBody::AddBuff(class [RoR2]RoR2.BuffDef)
            */

            if (c.TryGotoNext(x => x.MatchLdarg(0),
                    x => x.MatchLdsfld("RoR2.RoR2Content/Buffs", "Cloak"),
                    x => x.MatchCallOrCallvirt(typeof(CharacterBody).GetMethod("AddBuff", [typeof(BuffDef)]))))
            {
                c.RemoveRange(3);

                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Action<CharacterBody>>(
                    (cb) =>
                    {
                        cb.AddTimedBuff(RoR2Content.Buffs.Cloak, baseCloakDuration.Value * CurseManager.GetCurseIntensity() * CurseManager.GetActiveCurseCount(CurseIndex.MonsterCloak));
                    }
                );

                Log.Debug("unevilified cloak !!");
            }
            else
            {
                Log.Warning("failed il hook curse cloak !!");
            }
        }
        
        [HarmonyPatch(typeof(SS2.RewardDropper), "GeneratePickups")]
        [HarmonyPrefix]
        public static bool PickupPostFix(RewardDropper __instance)
        {
            if (injectorToCoupler.Value && Run.instance.IsExpansionEnabled(UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<ExpansionDef>("RoR2/DLC3/DLC3.asset").WaitForCompletion()))
            {
                foreach (PickupIndex pickupIndex in __instance.reward.pickups)
                {
                    if (PickupCatalog.GetPickupDef(pickupIndex)?.equipmentIndex == EquipmentIndex.None)
                    {
                        Log.Debug("not equipmnet ,..,");
                        continue;
                    }
                
                    Log.Debug("adding extra equip item to reward !!");
                    Array.Resize(ref __instance.reward.pickups, __instance.reward.pickups.Length + 1);
                    __instance.reward.pickups[^1] = PickupCatalog.FindPickupIndex(DLC3Content.Items.ExtraEquipment.itemIndex);
                }
            }
            
            return true;
        }
    }

    public override void Init(Harmony harmony)
    {
        harmony.CreateClassProcessor(typeof(Starstorm2GamblerTweaks)).Patch();
    }

    public override void Config(ConfigFile config)
    {
        baseCloakDuration = config.Bind("lunar gambler tweaks !!!", 
            "makes cloak curse have a timer instead of always .,., i hate them so much ,.,..,,. 0 to disable !!", 
            15.0f,
            "byeah ,.,");
        injectorToCoupler = config.Bind("lunar gambler tweaks !!!", 
            "add a functional coupler (if dlc3 enabled( to rewards with equipments (ones that used to have composite injector .,.,", 
            true,
            "byeah ,.,");
    }
    
    private static ConfigEntry<float> baseCloakDuration;
    private static ConfigEntry<bool> injectorToCoupler;
}