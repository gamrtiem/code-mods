using System;
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
using ShrineHealingBehavior = On.RoR2.ShrineHealingBehavior;

namespace BNR.items;

public class WoodToolkit : ItemBase<WoodToolkit>
{
    public override string ItemName => "Carving Kit";
    public override string ItemLangTokenName => "WoodenToolkit";
    public override string ItemPickupDesc => "Interacting with <style=\"cIsHealing\">Shrines of the Wood</style> gives you <style=\"cIsUtility\">bark</style>, droping after 2 seconds, creating a <style=\"cIsHealing\">healing aura</style>. Guarantee a Shrine of the Woods spawn.";
    public override string ItemFullDescription => "After interacting with a <style=\"cIsHealing\">Shrine of the Woods</style>, gain a piece of <style=\"cIsUtility\">bark</style> that drops after standing still for <style=\"cIsHealing\">2</style> seconds, healing for <style=\"cIsHealing\">6%</style> <style=\"cStack\">(+4% per stack)</style> of your health every second to all allies within <style=\"cIsHealing\">7m</style> <style=\"cStack\">(+4m per stack)</style>. Guarantee a Shrine of the Woods spawn.";
    public override string ItemLore => "WoodenToolkit";
    public override ItemTier Tier => ItemTier.Tier2;
    public override GameObject ItemModel => Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();
    public override Sprite ItemIcon => Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion();

    public override void Init(ConfigFile config)
    {
        CreateConfig(config);
        CreateLang();
        CreateItem();
        Hooks();
    }

    public override void CreateConfig(ConfigFile config)
    {
        ConfigEntry<bool> enabled = config.Bind("BNR - items",
            "enable wooden toolkit",
            true,
            "");
        BNRUtils.CheckboxConfig(enabled);
    }

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        return new ItemDisplayRuleDict();
    }
    
    
    public sealed class Behavior : BaseItemBodyBehavior
    {
        [ItemDefAssociation]
        private static ItemDef GetItemDef() => instance?.ItemDef;

        public float bufftimer;
        public float decay;
        public float outofcombattimer;
        public float explosiontimer;

        private void OnEnable()
        {
            On.RoR2.ShrineHealingBehavior.AddShrineStack += ShrineHealingBehaviorOnAddShrineStack;
            SceneDirector.onPostPopulateSceneServer += SceneDirectorOnonPostPopulateSceneServer;
        }

        private void SceneDirectorOnonPostPopulateSceneServer(SceneDirector obj)
        {
            if (!SceneInfo.instance.countsAsStage && !SceneInfo.instance.sceneDef.allowItemsToSpawnObjects)
            {
                return;
            }
            bool flag = false;
            Xoroshiro128Plus rng = new Xoroshiro128Plus(obj.rng.nextUlong);
            foreach (CharacterMaster readOnlyInstances in CharacterMaster.readOnlyInstancesList)
            {
                int itemCountEffective = readOnlyInstances.inventory.GetItemCountEffective(WoodToolkit.instance.ItemDef);
                if (itemCountEffective > 0)
                {
                    flag = true;
                }
            }
            if (!flag)
            {
                return;
            }
            GameObject gameObject = DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(Addressables.LoadAssetAsync<InteractableSpawnCard>("RoR2/Base/ShrineHealing/iscShrineHealing.asset").WaitForCompletion(), new DirectorPlacementRule
            {
                placementMode = DirectorPlacementRule.PlacementMode.Random
            }, rng));
            if (gameObject != null)
            {
                PurchaseInteraction component = gameObject.GetComponent<PurchaseInteraction>();
                if (component != null)
                {
                    component.Networkcost = Run.instance.GetDifficultyScaledCost(0);
                }
            }
        }

        private void ShrineHealingBehaviorOnAddShrineStack(ShrineHealingBehavior.orig_AddShrineStack orig, RoR2.ShrineHealingBehavior self, Interactor activator)
        {
            orig(self, activator);
            
            body.AddBuff(WoodToolkitBuff.instance.BuffDef.buffIndex);
            timer = 0;
        }

        private void OnDisable()
        {
            On.RoR2.ShrineHealingBehavior.AddShrineStack -= ShrineHealingBehaviorOnAddShrineStack;
        }

        private void FixedUpdate()
        {
            if (!NetworkServer.active)
            {
                return;
            }

            if (base.body.GetNotMoving() && body.GetBuffCount(WoodToolkitBuff.instance.BuffDef.buffIndex) > 0)
            {
                timer += Time.deltaTime;
            }
            else
            {
                timer = 0;
            }
            
            if (!(timer > 2f) || body.GetBuffCount(WoodToolkitBuff.instance.BuffDef.buffIndex) <= 0) return;

            timer = 0;
            int itemCount = instance.GetCount(body);
            float networkradius = body.radius + 7f;
            networkradius += 3f * (itemCount - 1);
            
            mushroomWardGameObject = Instantiate(mushroomWardPrefab, body.footPosition, Quaternion.identity);
            mushroomWardTeamFilter = mushroomWardGameObject.GetComponent<TeamFilter>();
            mushroomHealingWard = mushroomWardGameObject.GetComponent<HealingWard>();
            mushroomWardGameObject.transform.Find("Indicator").transform.Find("MushroomMeshes").gameObject.SetActive(false);
            GameObject gameObject = new  GameObject("BarkStatue");
            gameObject.AddComponent<ModelLocator>();
            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.mesh = Addressables.LoadAssetAsync<Mesh>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_ShrineHealing.mdlShrineHealing_fbx).WaitForCompletion();
            mushroomWardGameObject.name = "BarkHealWard";
            
            gameObject.AddComponent<MeshRenderer>().material = Addressables.LoadAssetAsync<Material>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_ShrineHealing.matShrineHealing_mat).WaitForCompletion();
            gameObject.transform.SetParent(mushroomWardGameObject.transform, false);
            gameObject.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            gameObject.transform.position = gameObject.transform.localPosition;
            gameObject.transform.localPosition = new Vector3(0f, 0f, 0f);
            Vector3 target;
            target.x = 270;
            target.y = 0;
            target.z = 0; 
            gameObject.transform.rotation = Quaternion.Euler(target);
            
            NetworkServer.Spawn(mushroomWardGameObject);
            
            if ((bool)mushroomHealingWard)
            {
                mushroomHealingWard.interval = 0.25f;
                mushroomHealingWard.healFraction = (0.06f + 0.04f * (itemCount - 1)) * mushroomHealingWard.interval;
                mushroomHealingWard.healPoints = 0f;
                mushroomHealingWard.Networkradius = networkradius;
            }
            if ((bool)mushroomWardTeamFilter)
            {
                mushroomWardTeamFilter.teamIndex = base.body.teamComponent.teamIndex;
            }
            
            body.SetBuffCount(WoodToolkitBuff.instance.BuffDef.buffIndex, body.GetBuffCount(WoodToolkitBuff.instance.BuffDef.buffIndex) - 1);
        }

        private GameObject mushroomWardPrefab = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Mushroom.MushroomWard_prefab).WaitForCompletion();
        private GameObject mushroomWardGameObject;
        private TeamFilter mushroomWardTeamFilter;
        private HealingWard mushroomHealingWard;
        private float timer;
    }
}