using static BNR.butterscotchnroses;
using BNR.patches;
using BepInEx.Configuration;
using HarmonyLib;
using RoR2;
using SS2.Components;
using UnityEngine;
using UnityEngine.SceneManagement;
using CraftingController = On.RoR2.CraftingController;
using WormBodyPositions2 = On.RoR2.WormBodyPositions2;

namespace BNR;

public class ChefBazaar : PatchBase<ChefBazaar>
{
    public override void Init(Harmony harmony)
    {     
        applyHooks();
            
        if(!enabled.Value)
        {
            return;
        }
    }
        
    private void applyHooks()
    {
        if (enabled.Value)
        {
            On.RoR2.CraftingController.AllSlotsFilled += CraftingControllerOnAllSlotsFilled;
            On.RoR2.CraftingController.AttemptFindPossibleRecipes += CraftingControllerOnAttemptFindPossibleRecipes;
            On.RoR2.CraftingController.SendToMealprep += CraftingControllerOnSendToMealprep;
        }
        else
        {
            On.RoR2.CraftingController.AllSlotsFilled -= CraftingControllerOnAllSlotsFilled;
            On.RoR2.CraftingController.AttemptFindPossibleRecipes -= CraftingControllerOnAttemptFindPossibleRecipes;
            On.RoR2.CraftingController.SendToMealprep -= CraftingControllerOnSendToMealprep;
        }
    }

    private void CraftingControllerOnSendToMealprep(CraftingController.orig_SendToMealprep orig, RoR2.CraftingController self, Interactor activator)
    {
        ItemDef cookedItem = ItemCatalog.GetItemDef(self.result.pickupDef.itemIndex);
        if (!cookedItem)
        {
            Log.Debug("null CraftingControllerOnSendToMealprep");
            orig(self, activator);
            return;
        }

        if (cookedItem.tier == ItemTier.FoodTier)
        {
            Log.Debug("nuh uh !");
        }
        else
        {
            Log.Debug("yuh uh !");
            orig(self, activator);
        }
    }

    private void CraftingControllerOnConfirmSelection(CraftingController.orig_ConfirmSelection orig, RoR2.CraftingController self)
    {
        ItemDef cookedItem = ItemCatalog.GetItemDef(self.result.pickupDef.itemIndex);
        if (!cookedItem)
        {
            Log.Debug("null CraftingControllerOnConfirmSelection");
            orig(self);
            return;
        }
        
        if (cookedItem.tier == ItemTier.FoodTier)
        {
            Log.Debug("nuh uh ");
        }
        else
        {
            Log.Debug($"chefbazaar - {cookedItem}");
            orig(self);
        }
    }

    private void CraftingControllerOnConfirmButtonHit(CraftingController.orig_ConfirmButtonHit orig, RoR2.CraftingController self, int index)
    {
        ItemDef cookedItem = ItemCatalog.GetItemDef(self.result.pickupDef.itemIndex);
        if (!cookedItem)
        {
            Log.Debug("null CraftingControllerOnConfirmButtonHit");
            orig(self, index);
            return;
        }
        
        if (cookedItem.tier == ItemTier.FoodTier)
        {
            Log.Debug("bwaa ");
        }
        else
        {
            Log.Debug($"chefbazaar - {cookedItem}");
            orig(self, index);
        }
    }

    private void CraftingControllerOnAttemptFindPossibleRecipes(CraftingController.orig_AttemptFindPossibleRecipes orig, RoR2.CraftingController self)
    {

        orig(self);

        if (!self.AllSlotsFilled())
        {
            return;
        }

        ItemDef cookedItem = ItemCatalog.GetItemDef(self.result.pickupDef.itemIndex);
        if (!cookedItem)
        {
            return;
        }
        
        Log.Debug($"chefbazaar - {Language.GetStringFormatted(cookedItem.nameToken)}");

        if (cookedItem.tier == ItemTier.FoodTier)
        {
            Log.Debug($"chefbazaar - caughts trying to cook a food item !! checking for if computational exchange .,,. ");
            Scene scene = SceneManager.GetActiveScene();
            Log.Debug($"chefbazaar - {scene.name}");

            if (scene.name != "computationalexchange")
            {
                Log.Debug("rahh not exchange dont cook !! ");
                self.result = PickupIndex.none;
            }
            else
            {
                Log.Debug("ok nvm keep cooking !! ");
            }
        }
    }

    private bool CraftingControllerOnAllSlotsFilled(CraftingController.orig_AllSlotsFilled orig, RoR2.CraftingController self)
    {

        /*if (PickupCatalog.GetPickupDef(PickupIndex.none).itemTier == ItemTier.FoodTier)
        {
            if (cookedItem.tier == ItemTier.FoodTier)
            {
                Log.Debug($"chefbazaar - caughts trying to cook a food item !! checking for if computational exchange .,,. ");
                Scene scene = SceneManager.GetActiveScene();
                Log.Debug($"chefbazaar - {scene.name}");

                if (scene.name != "computationalexchange")
                {
                    Log.Debug("rahh not exchange dont cook !! ");
                }
                else
                {
                    Log.Debug("ok nvm keep cooking !! ");
                }
            }
            self.result = PickupIndex.none;
        }*/
        return orig(self);
    }

    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("Mods - ChefBazaar",
            "enable patches for ChefBazaar",
            true,
            "");
        BNRUtils.CheckboxConfig(enabled);
        enabled.SettingChanged += (_, _) =>
        {
            applyHooks();
        };
    }

    private ConfigEntry<bool> enabled;
}