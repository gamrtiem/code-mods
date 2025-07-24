using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BepInEx;
using EquipdroneStrikelings;
using IL.EntityStates.FalseSonBoss;
using Mono.Cecil;
using MonoMod.Cil;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using EquipmentSlot = On.RoR2.EquipmentSlot;
using MasterSummon = On.RoR2.MasterSummon;
using PurchaseInteraction = On.RoR2.PurchaseInteraction;
using Random = UnityEngine.Random;
using SummonMasterBehavior = On.RoR2.SummonMasterBehavior;

namespace EquipdroneStrikelings
{
    [BepInDependency(ItemAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    public class EquipdroneStrikelings : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "icebro";
        public const string PluginName = "EquipdroneStrikelings";
        public const string PluginVersion = "0.9.0";
        

        public void Awake()
        {
            Log.Init(Logger);
            
            //On.RoR2.EquipmentSlot.PerformEquipmentAction += EquipmentSlotOnPerformEquipmentAction;
            On.RoR2.EquipmentSlot.FireDroneBackup += EquipmentSlotOnFireDroneBackup;
            //On.RoR2.MasterSummon.Perform += MasterSummonOnPerform;
            
            IL.RoR2.EquipmentSlot.FireDroneBackup += il => 
            {
                ILCursor c = new ILCursor(il);

                if (c.TryGotoNext(
                        x => x.MatchLdloc(1),
                        x => x.MatchLdcR4(0.0f),
                        x => x.MatchLdcR4(3f)
                    ))
                {
                    //go back to the start of the charactermaster if statement 
                    c.Index -= 3;
                    
                    //remove the code that adds suicide timer (adding later !!
                    c.RemoveRange(9);

                    //pass through the equipslot and charactermaster of strike drone to equip drone check
                    c.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                    c.Emit(Mono.Cecil.Cil.OpCodes.Ldloc_S, c.Method.Body.Variables[12]); 
                    c.Emit(Mono.Cecil.Cil.OpCodes.Call, typeof(EquipdroneStrikelings).GetMethod("EquipmentDroneCheck"));

                    //Log.Debug(il.ToString());
                } else 
                {
                    Log.Error(il.Method.Name + " IL Hook failed!");
                }
            };
            
            On.RoR2.PurchaseInteraction.GetInteractability += PurchaseInteraction_GetInteractability; 
        }

        private Interactability PurchaseInteraction_GetInteractability(PurchaseInteraction.orig_GetInteractability orig, RoR2.PurchaseInteraction self, Interactor activator) //https://discord.com/channels/562704639141740588/562704639569428506/808109930913202217
        {
            var summonMasterBehavior = self.gameObject.GetComponent<RoR2.SummonMasterBehavior>();
            if (!summonMasterBehavior || !summonMasterBehavior.callOnEquipmentSpentOnPurchase)
                return orig(self, activator);
            
            CharacterBody characterBody = activator.GetComponent<CharacterBody>();
            if (!characterBody || !characterBody.inventory) return orig(self, activator);
            
            Log.Debug($"awesome epic {EquipmentCatalog.GetEquipmentDef(characterBody.inventory.currentEquipmentIndex).name}");
            if (EquipmentCatalog.GetEquipmentDef(characterBody.inventory.currentEquipmentIndex).name == ("DroneBackup"))
            {
                self.gameObject.AddComponent<MinionTracker>();
            }
            return orig(self, activator);
        }


        private CharacterMaster MasterSummonOnPerform(MasterSummon.orig_Perform orig, RoR2.MasterSummon self)
        {
            /*if (!summon.name.Contains("EquipmentDrone")) return summon;
            
            var summonname = summon.name;
            Log.Debug(summonname);
            var inventory = summon.GetComponent<Inventory>();
            var equipmentDef = EquipmentCatalog.GetEquipmentDef(inventory.currentEquipmentIndex);
            if (self.inventoryToCopy)
            {
                var buyerequip = EquipmentCatalog.GetEquipmentDef(self.inventoryToCopy.currentEquipmentIndex);
                Log.Debug($"buyer equip index {self.inventoryToCopy.currentEquipmentIndex}");
                Log.Debug($"buyerequip {buyerequip.name}");
                
            } else 
                Log.Debug("no buyerequip found");

            if (self.masterPrefab.GetComponent<Inventory>())
            {
                
                Log.Debug($"master equip {self.masterPrefab.GetComponent<Inventory>().currentEquipmentIndex}");
                    
            }
            else
            {
                Log.Debug("no master prefab found");
            }


            Log.Debug(equipmentDef ? equipmentDef.name : "null equip def ~!");
            Log.Debug($"summon equip index {inventory.currentEquipmentIndex}");
            Log.Debug(inventory ? inventory : "null inventroy def ~!");

            return summon;*/
            
            //Log.Debug(EquipmentCatalog.GetEquipmentDef(self.masterPrefab.GetComponent<Inventory>().currentEquipmentIndex).name);
            return orig(self);
        }
        

        public static void EquipmentDroneCheck(RoR2.EquipmentSlot equipslot, CharacterMaster master)
        {
            //Log.Debug(master.name);
            var mastername = equipslot.characterBody.name;
            //Log.Debug($"minion owenr is {mastername} !!!");
            
            if(!mastername.Contains("EquipmentDrone"))
                master.gameObject.AddComponent<MasterSuicideOnTimer>().lifeTimer = 25f + Random.Range(0f, 3f);
            else
            {
                var miniontracker = equipslot.characterBody.gameObject.GetComponent<MinionTracker>();
                
                if(!miniontracker)
                    equipslot.characterBody.gameObject.AddComponent<MinionTracker>().StrikeDrones.Add(master);
                else
                    miniontracker.StrikeDrones.Add(master);
            }
        }
        
        private bool EquipmentSlotOnFireDroneBackup(EquipmentSlot.orig_FireDroneBackup orig, RoR2.EquipmentSlot self)
        {
            var charactermaster = self.characterBody?.master;
            //reallly big if statement but just checks if its an equip drone or doesnt have a strike drone or no body ,.,.
            if (!charactermaster || !self.characterBody.name.Contains("EquipmentDrone") || self.characterBody.GetComponent<MinionTracker>()?.HasStrikeDrone() != true)
                return orig(self);
            
            //Log.Debug("still has strike drone ,..,. ");
            return false;
        }

        private bool EquipmentSlotOnPerformEquipmentAction(EquipmentSlot.orig_PerformEquipmentAction orig, RoR2.EquipmentSlot self, EquipmentDef equipmentDef)
        {
            Log.Debug($"base name {self.characterBody.name} name token {self.characterBody.baseNameToken}");
            return orig(self, equipmentDef);
        }
    }
}

public class MinionTracker : MonoBehaviour
{
    public List <CharacterMaster> StrikeDrones = [];
    private float count = 0;
    
    public bool HasStrikeDrone()
    {
        //remove dead strikedones
        foreach (var master in StrikeDrones.ToList().Where(master => !master))
        {
            StrikeDrones.Remove(master);
        }

        // foreach (var master in StrikeDrones)
        // {
        //     Log.Debug(master.name);
        // }
        // Log.Debug(StrikeDrones.Count);

        return StrikeDrones.Count > 0;
    }

    private void FixedUpdate()
    {
        count += Time.fixedDeltaTime;
        if (count <= 1f) return; 
        Log.Debug($"count is {count}");
        count = 0;
        this.GetComponent<RoR2.EquipmentSlot>().PerformEquipmentAction(RoR2Content.Equipment.DroneBackup);
    }
}