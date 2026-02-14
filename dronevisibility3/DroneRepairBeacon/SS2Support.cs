using System.Collections.Generic;
using HarmonyLib;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using SS2;

namespace DroneRepairBeacon;

public class SS2Support
{
    [HarmonyPatch]
    public class Starstorm2DronePatches
    {
        [HarmonyPatch(typeof(SS2.Interactables.CloneDroneDamaged), "SpawnCloneCorpse")]
        [HarmonyPrefix]
        public static void SpawnCloneCorpsePrefix(SS2.Interactables.CloneDroneDamaged __instance, On.EntityStates.Drone.DeathState.orig_OnImpactServer orig, EntityStates.Drone.DeathState self, Vector3 contactPoint)
        {
            Log.Debug("spawning indicator clone !! ");
            SpawnIndicator(self.characterBody.name.Replace("Body", "").Replace("(Clone)", ""), self.gameObject);
        }
        
        [HarmonyPatch(typeof(SS2.Interactables.ShockDroneDamaged), "SpawnShockCorpse")]
        [HarmonyPrefix]
        public static void SpawnShockCorpsePrefix(SS2.Interactables.CloneDroneDamaged __instance, On.EntityStates.Drone.DeathState.orig_OnImpactServer orig, EntityStates.Drone.DeathState self, Vector3 contactPoint)
        {
            Log.Debug("spawning indicator shock !! ");
            SpawnIndicator(self.characterBody.name.Replace("Body", "").Replace("(Clone)", ""), self.gameObject);
        }

    }
    
    public void initCompat(Harmony harmony)
    {
        Log.Debug("patching starstorms !! !!");

        harmony.CreateClassProcessor(typeof(Starstorm2DronePatches)).Patch();
    }

    private static void SpawnIndicator(string name, GameObject gameObject)
    {
        if(!NetworkServer.active) return;
        
        int rng = -1;
        bool usingSpecificSprite = false;
        if (DroneRepairBeacon.droneIndicatorSprites.Count != 0 || DroneRepairBeacon.specificDroneIndicatorSprites.Count != 0)
        {
            List<int> specificSpriteIndexes = [];
    
            for(int i = 0; i < DroneRepairBeacon.specificDroneIndicatorSprites.Count; i++)
            {
                if (DroneRepairBeacon.specificDroneIndicatorSprites[i].name.Split("_")[0].Replace("Body", "") != name) continue;
                Log.Debug($"adding {DroneRepairBeacon.specificDroneIndicatorSprites[i].name}");
                specificSpriteIndexes.Add(i);
            }
            if (DroneRepairBeacon.droneIndicatorSprites.Count != 0 || specificSpriteIndexes.Count != 0)
            {
                rng = Run.instance.runRNG.RangeInt(0, DroneRepairBeacon.droneIndicatorSprites.Count + specificSpriteIndexes.Count);
                if (rng >= DroneRepairBeacon.droneIndicatorSprites.Count)
                {
                    rng = specificSpriteIndexes[Run.instance.runRNG.RangeInt(0, specificSpriteIndexes.Count)];
                    usingSpecificSprite = true;
                }
            }
        }
            
        GameObject indicator = Object.Instantiate(DroneRepairBeacon.DroneIndicatorHologram, gameObject.transform);
        deadDroneTracker tracker = indicator.GetComponent<deadDroneTracker>();
        tracker.messageID = rng;
        tracker.usingSpecificSprite = usingSpecificSprite;

        NetworkServer.Spawn(indicator);
    }
}