using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BepInEx;
using equipdrone;
using IL.EntityStates.FalseSonBoss;
using Mono.Cecil;
using MonoMod.Cil;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using EquipmentSlot = On.RoR2.EquipmentSlot;
using Random = UnityEngine.Random;

namespace equipdrone
{
    [BepInDependency(ItemAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    public class equipdrone : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "icebro";
        public const string PluginName = "equipdrone";
        public const string PluginVersion = "1.0.0";
        

        public void Awake()
        {
            Log.Init(Logger);
            
            //On.RoR2.EquipmentSlot.PerformEquipmentAction += EquipmentSlotOnPerformEquipmentAction;
            On.RoR2.EquipmentSlot.FireDroneBackup += EquipmentSlotOnFireDroneBackup;
            
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
                    c.Emit(Mono.Cecil.Cil.OpCodes.Call, typeof(equipdrone).GetMethod("EquipmentDroneCheck"));

                    //Log.Debug(il.ToString());
                } else 
                {
                    Log.Error(il.Method.Name + " IL Hook failed!");
                }
            };
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
}