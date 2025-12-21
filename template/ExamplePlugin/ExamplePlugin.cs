using BepInEx;
using BepInEx.Bootstrap;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ExamplePlugin
{
    [BepInDependency("iDeathHD.UnityHotReload", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(ItemAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class ExamplePlugin : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "icebro";
        public const string PluginName = "ExamplePlugin";
        public const string PluginVersion = "1.0.0";
        
        public static bool UHRInstalled => Chainloader.PluginInfos.ContainsKey("iDeathHD.UnityHotReload");

        private static ItemDef myItemDef;

        public void Awake()
        {
            Log.Init(Logger);

            myItemDef = ScriptableObject.CreateInstance<ItemDef>();

            myItemDef.name = "EXAMPLE_CLOAKONKILL_NAME";
            myItemDef.nameToken = "EXAMPLE_CLOAKONKILL_NAME";
            myItemDef.pickupToken = "EXAMPLE_CLOAKONKILL_PICKUP";
            myItemDef.descriptionToken = "EXAMPLE_CLOAKONKILL_DESC";
            myItemDef.loreToken = "EXAMPLE_CLOAKONKILL_LORE";

            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier2Def.asset").WaitForCompletion();

            myItemDef.pickupIconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion();
            myItemDef.pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();

            myItemDef.canRemove = true;

            myItemDef.hidden = false;
            
            var displayRules = new ItemDisplayRuleDict(null);

            ItemAPI.Add(new CustomItem(myItemDef, displayRules));

            GlobalEventManager.onCharacterDeathGlobal += GlobalEventManager_onCharacterDeathGlobal;
        }

        private void GlobalEventManager_onCharacterDeathGlobal(DamageReport report)
        {
            if (!report.attacker || !report.attackerBody)
            {
                return;
            }

            var attackerCharacterBody = report.attackerBody;

            if (attackerCharacterBody.inventory)
            {
                var garbCount = attackerCharacterBody.inventory.GetItemCount(myItemDef.itemIndex);
                if (garbCount > 0 &&
                    Util.CheckRoll(50, attackerCharacterBody.master))
                {
                    attackerCharacterBody.AddTimedBuff(RoR2Content.Buffs.Cloak, 3 + garbCount);
                }
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;
                
                Log.Info($"Player pressed F2. Spawning our custom item at coordinates {transform.position}");
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(myItemDef.itemIndex), transform.position, transform.forward * 20f);
            }
            
#if DEBUG
            if (Input.GetKeyUp(KeyCode.F8))
            {
                if (UHRInstalled)
                {
                    UHRSupport.hotReload(typeof(ExamplePlugin).Assembly, System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location), "ExamplePlugin.dll"));
                }
                else
                {
                    Log.Debug("couldnt finds unity hot reload !!");
                }
            }
#endif  
        }
    }
}
