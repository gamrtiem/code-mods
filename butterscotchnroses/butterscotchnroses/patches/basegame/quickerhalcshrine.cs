using BepInEx.Configuration;
using BNR.patches;
using static BNR.butterscotchnroses;
using HarmonyLib;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using BossGroup = On.RoR2.BossGroup;

namespace BNR;

public class quickerhalcshrine : PatchBase<quickerhalcshrine>
{
    private HalcyoniteShrineInteractable halcshrineinteractable;
    private GameObject bluePortalRef;
    private float startingTickRate;
    public override void Init(Harmony harmony)
    {
        if (!enabled.Value)
        {
            return;
        }
        
        GameObject shrineHalcyonite = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC2.ShrineHalcyonite_prefab).WaitForCompletion();
        bluePortalRef = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_PortalShop.PortalShop_prefab).WaitForCompletion();
        halcshrineinteractable = shrineHalcyonite.GetComponent<HalcyoniteShrineInteractable>();
        startingTickRate = halcshrineinteractable.tickRate;

        halcshrineinteractable.tickRate = startingTickRate * multiplier.Value;
        multiplier.SettingChanged += (_, _) =>
        {
            halcshrineinteractable.tickRate = startingTickRate * multiplier.Value;
        };
        
        On.RoR2.HalcyoniteShrineInteractable.Start += HalcyoniteShrineInteractableOnStart;
        On.RoR2.GoldSiphonNearbyBodyController.DrainGold += GoldSiphonNearbyBodyControllerOnDrainGold;
        On.RoR2.BossGroup.OnDefeatedServer += BossGroupOnOnDefeatedServer;
    }

    private void BossGroupOnOnDefeatedServer(BossGroup.orig_OnDefeatedServer orig, RoR2.BossGroup self)
    {
        orig(self);

        if (!greenToBlue.Value)
        {
            return;
        }
        
        GameObject greenportal = GameObject.Find("PortalColossus(Clone)");
        if (!greenportal)
        {
            return;
        }
        
        GameObject blueportal = Object.Instantiate(bluePortalRef);
        blueportal.transform.position = greenportal.transform.position;
        NetworkServer.Spawn(blueportal);
        Object.Destroy(greenportal);
    }

    private void GoldSiphonNearbyBodyControllerOnDrainGold(On.RoR2.GoldSiphonNearbyBodyController.orig_DrainGold orig, RoR2.GoldSiphonNearbyBodyController self)
    {
        orig(self);

        if (self.isTetheredToAtLeastOneObject && scaleTime.Value && Run.instance.participatingPlayerCount <= playerCount.Value)
        {
            Run.instance.SetRunStopwatch((Run.instance.GetRunStopwatch() + 1f / self.tickRate) * (multiplier.Value - 1));
        }
    }

    private void HalcyoniteShrineInteractableOnStart(On.RoR2.HalcyoniteShrineInteractable.orig_Start orig, HalcyoniteShrineInteractable self)
    {
        orig(self);
        
        if (Run.instance.participatingPlayerCount <= playerCount.Value)
        {
            self.tickRate = startingTickRate * multiplier.Value;
        }
    }

    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("BNR - Halyc Shrines",
            "enable patches for halyc shrines",
            true,
            "");
        BNRUtils.CheckboxConfig(enabled);
        enabled.SettingChanged += (_, _) =>
        {
            if (enabled.Value)
            {
                On.RoR2.HalcyoniteShrineInteractable.Start += HalcyoniteShrineInteractableOnStart;
                On.RoR2.GoldSiphonNearbyBodyController.DrainGold += GoldSiphonNearbyBodyControllerOnDrainGold;
            }
            else
            {
                On.RoR2.HalcyoniteShrineInteractable.Start -= HalcyoniteShrineInteractableOnStart;
                On.RoR2.GoldSiphonNearbyBodyController.DrainGold -= GoldSiphonNearbyBodyControllerOnDrainGold;
            }
        };
        
        multiplier = config.Bind("BNR - Halyc Shrines",
            "speed up halcyon shrine mutlipler",
            2f,
            "");
        BNRUtils.SliderConfig(1, 3, multiplier);

        playerCount = config.Bind("BNR - Halyc Shrines",
            "only apply multiplier under or equal to x amount of players",
            1,
            "eg. 1 = only singleplayer 2 = only 2 people multiplayer");
        BNRUtils.SliderConfig(1, 4, playerCount);
        
        scaleTime = config.Bind("BNR - Halyc Shrines",
            "speed up time scale as well",
            true,
            "make time scale with halcyon shrine speed as well");
        BNRUtils.CheckboxConfig(scaleTime);
        
        greenToBlue = config.Bind("BNR - Halyc Shrines",
            "turn green portal to blue",
            true,
            "makes green portals become blue after the boss has been defeated !!!");
        BNRUtils.CheckboxConfig(greenToBlue);
    }

    private ConfigEntry<bool> enabled;
    private ConfigEntry<float> multiplier;
    private ConfigEntry<int> playerCount;
    private ConfigEntry<bool> scaleTime;
    private ConfigEntry<bool> greenToBlue;
}