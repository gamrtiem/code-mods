using System;
using System.Collections.Generic;
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
using Object = System.Object;
using ShrineHealingBehavior = On.RoR2.ShrineHealingBehavior;

namespace BNR.items;

public class WoodToolkit : ItemBase<WoodToolkit>
{
    public override string ItemName => "Carving Kit";
    public override string ItemLangTokenName => "WoodenToolkit";
    public override string ItemPickupDesc => "Interacting with <style=\"cIsHealing\">Shrines of the Wood</style> gives you <style=\"cIsUtility\">bark</style>, droping after 2 seconds, creating a <style=\"cIsHealing\">healing aura</style>. Guarantee a free <style=\"cIsHealing\">Shrine of the Woods</style> spawn.";
    public override string ItemFullDescription => "After interacting with a <style=\"cIsHealing\">Shrine of the Woods</style>, gain a piece of <style=\"cIsUtility\">bark</style> that drops after standing still for <style=\"cIsHealing\">2</style> seconds, healing for <style=\"cIsHealing\">6%</style> <style=\"cStack\">(+4% per stack)</style> of your health every second to all allies within <style=\"cIsHealing\">7m</style> <style=\"cStack\">(+4m per stack)</style>. Guarantee a free <style=\"cIsHealing\">Shrine of the Woods</style> spawn.";
    public override string ItemLore => "umm ,..,., \n uhhhhhh\n uhhhhhhhh .,..,,. \n yeah .,.,.,.,,. \n umm .,.,. \n \n \n \n \n \n the .,,,. um m.,.,,., thats .,,.. uhhhhhhhhh ,..,.,.,, something ..,,. uhh .,., cookie crumbles !!! ";
    public override ItemTier Tier => ItemTier.Tier2;
    public override GameObject ItemModel => butterscotchnroses.carvingKitBundle.LoadAsset<GameObject>("carvingkit.prefab");
    public override Sprite ItemIcon => butterscotchnroses.carvingKitBundle.LoadAsset<Sprite>("carvingkit.png");

    private ConfigEntry<bool> enabled;
    private InteractableSpawnCard spawnCard;
    //private static GameObject mushroomWardGameObject = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Mushroom.MushroomWard_prefab).WaitForCompletion();
    private static GameObject mushroomWardPrefab = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Mushroom.MushroomWard_prefab).WaitForCompletion().InstantiateClone("mushWard");

    public override void Init(ConfigFile config)
    {
        CreateConfig(config);
        if(!enabled.Value) return;
        
        CreateLang();
        CreateItem();
        Hooks();

        spawnCard = UnityEngine.Object.Instantiate(Addressables.LoadAssetAsync<InteractableSpawnCard>(RoR2BepInExPack.GameAssetPaths.Version_1_35_0.RoR2_Base_ShrineHealing.iscShrineHealing_asset).WaitForCompletion());
        spawnCard.prefab = spawnCard.prefab.InstantiateClone("WoodToolkitShrine", true);
        if (spawnCard.prefab.TryGetComponent(out ModelLocator modelLocator))
        {
            if (modelLocator.modelTransform.gameObject.TryGetComponent(out MeshRenderer meshRenderer))
            {
                meshRenderer.sharedMaterial = woodsMaterial;
            }
        }
        
        mushroomWardPrefab.transform.Find("Indicator").transform.Find("MushroomMeshes").gameObject.SetActive(false);
        GameObject gameObject = PrefabAPI.CreateEmptyPrefab("silly", true);
        gameObject.transform.SetParent(mushroomWardPrefab.transform);
        gameObject.AddComponent<ModelLocator>();
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = Addressables.LoadAssetAsync<Mesh>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_ShrineHealing.mdlShrineHealing_fbx).WaitForCompletion();
        mushroomWardPrefab.name = "BarkHealWard";
            
        gameObject.AddComponent<MeshRenderer>().material = woodsMaterial;
        gameObject.transform.SetParent(mushroomWardPrefab.transform, false);
        gameObject.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        gameObject.transform.position = gameObject.transform.localPosition;
        gameObject.transform.localPosition = new Vector3(0f, 0f, 0f);
        Vector3 target;
        target.x = 270;
        target.y = 0;
        target.z = 0; 
        gameObject.transform.rotation = Quaternion.Euler(target);
        
        mushroomWardPrefab.RegisterNetworkPrefab();
    }
    
    public override void CreateConfig(ConfigFile config)
    {
        enabled = config.Bind("BNR - items",
            "enable wooden toolkit",
            true,
            "");
        BNRUtils.CheckboxConfig(enabled);
    }

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        return new ItemDisplayRuleDict();
    }

    public override void Hooks()
    {
        SceneDirector.onPostPopulateSceneServer += SceneDirectorOnonPostPopulateSceneServer;
    }

    private void SceneDirectorOnonPostPopulateSceneServer(SceneDirector obj)
    {
        if ((!SceneInfo.instance.countsAsStage && !SceneInfo.instance.sceneDef.allowItemsToSpawnObjects) || SceneInfo.instance.sceneDef.cachedName == "moon2") 
        {
            return;
        }
        
        bool enabled = false;
        Xoroshiro128Plus rng = new(obj.rng.nextUlong);
        foreach (CharacterMaster readOnlyInstances in CharacterMaster.readOnlyInstancesList)
        {
            int itemCountEffective = readOnlyInstances.inventory.GetItemCountEffective(WoodToolkit.instance.ItemDef);
            if (itemCountEffective > 0)
            {
                enabled = true;
            }
        }
        if (!enabled)
        {
            return;
        }
        
        GameObject gameObject = DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(spawnCard, new DirectorPlacementRule
        {
            placementMode = DirectorPlacementRule.PlacementMode.Random
        }, rng));
        if (gameObject == null) return;
        
        if (gameObject.TryGetComponent(out PurchaseInteraction interaction))
        {
            interaction.Networkcost = Run.instance.GetDifficultyScaledCost(0);
        }
    }
    
    public sealed class Behavior : BaseItemBodyBehavior
    {
        [ItemDefAssociation]
        private static ItemDef GetItemDef() => instance?.ItemDef;
        
        private void OnEnable()
        {
            On.RoR2.ShrineHealingBehavior.AddShrineStack += ShrineHealingBehaviorOnAddShrineStack;
        }

        private void ShrineHealingBehaviorOnAddShrineStack(ShrineHealingBehavior.orig_AddShrineStack orig, RoR2.ShrineHealingBehavior self, Interactor activator)
        {
            orig(self, activator);

            if (!activator.TryGetComponent(out CharacterBody characterBody)) return;
            
            if(characterBody.inventory.GetItemCountEffective(instance.ItemDef) <= 0) return;
            characterBody.AddBuff(WoodToolkitBuff.instance.BuffDef.buffIndex);
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

        private GameObject mushroomWardGameObject;
        private TeamFilter mushroomWardTeamFilter;
        private HealingWard mushroomHealingWard;
        private float timer;
    }

    private Material woodsMaterial = Addressables.LoadAssetAsync<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_35_0.RoR2_DLC3_Drone_Tech.matDroneTechSwarmAOE_Indicator2_mat_a01f2b91).WaitForCompletion();
}