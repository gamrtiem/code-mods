using System.Collections.Generic;
using System.Linq;
using BNR.patches;
using BepInEx.Configuration;
using BNR.items;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BNR;

public class whodiddamage : PatchBase<whodiddamage>
{
    private static Texture CrownIcon;
    private static List<GameObject> crownImages = [];
    private Dictionary<BossGroup, Dictionary<CharacterMaster, DamageCredit>> bossGroupsToDamages = [];
    private Dictionary<CharacterMaster, float> mastersTotalDamage = [];
    
    public override void Init()
    {
        CrownIcon = butterscotchnroses.bnrBundle.LoadAsset<Texture>("texCrownIcon");
        applyHooks();
    }

    private void applyHooks()
    {
        if (enabled.Value)
        {
            BossGroup.onBossGroupStartServer += StartTracking;
            BossGroup.onBossGroupDefeatedServer += PrintDamage;
            Stage.onStageStartGlobal += StageOnonStageStartGlobal;
            On.RoR2.UI.AllyCardController.InfoOverride += AllyCardControllerOnInfoOverride;
        }
        else
        {
            BossGroup.onBossGroupStartServer -= StartTracking;
            BossGroup.onBossGroupDefeatedServer -= PrintDamage;
            Stage.onStageStartGlobal -= StageOnonStageStartGlobal;
            On.RoR2.UI.AllyCardController.InfoOverride -= AllyCardControllerOnInfoOverride;
        }
    }
    
    public static Color GetHex(string hex)
    {
        if (!hex.StartsWith("#"))
        {
            hex = "#" + hex;
        }
        
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }
    
    private void AllyCardControllerOnInfoOverride(On.RoR2.UI.AllyCardController.orig_InfoOverride orig, RoR2.UI.AllyCardController self)
    {
        orig(self);

        if (!useCrown.Value || butterscotchnroses.clientSide.Value) return;
        
        GameObject oldCrownObj = self.portraitIconImage.transform.Find("crown")?.gameObject;
        bool hasCrownItem = self.sourceMaster.inventory.GetItemCountEffective(Crown.instance.ItemDef) > 0;
        
        if (oldCrownObj && !hasCrownItem)
        {
            Object.Destroy(oldCrownObj);
            return;
        }
        if (!hasCrownItem) return;
        
        GameObject crownObj = new GameObject("crown");
        crownObj.transform.parent = self.portraitIconImage.transform;
        crownObj.transform.localPosition = Vector3.zero;
        
        RawImage rawImage = crownObj.AddComponent<RawImage>();
        rawImage.texture = CrownIcon;//LocalUserManager.GetFirstLocalUser().userProfile.portraitTexture;
        if (crownColorBasedOffBody.Value)
        {
            rawImage.color = self.sourceMaster.bodyPrefab.GetComponent<CharacterBody>().bodyColor;
        }
        if (crownColorOverrides.Value != "")
        {
            string? steamID = self.sourceMaster?.playerCharacterMasterController?.networkUser?.id.steamId.ToSteamID();
            if (steamID != null)
            {
                Log.Debug($"target steamID: {steamID.Split(':')[^1]}");
                string[] values = crownColorOverrides.Value.Split(',');
                foreach (var value in values)
                {
                    Log.Debug($"config: {value.Split(':')[^1]}");
                }
                for (int i = 0; i < values.Length; i += 2)
                {
                    values[i] = values[i].Trim();
                    if (values[i].Split(':')[^1] == steamID.Split(':')[^1])
                    {
                        rawImage.color = GetHex(values[i + 1]);
                    }
                }
            }
        }
        
        RectTransform rectTransform = crownObj.GetComponent<RectTransform>();
        rectTransform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        rectTransform.pivot = new Vector2(0.5f, -0.5f);
            
        crownImages.Add(crownObj);
    }

    //catch strays .,
    private void StageOnonStageStartGlobal(Stage stage)
    {
        bossGroupsToDamages = [];
        mastersTotalDamage = [];
        GlobalEventManager.onServerDamageDealt -= GlobalEventManagerOnonServerDamageDealt;
    }
    
    private void PrintDamage(BossGroup bossGroup)
    {
        GlobalEventManager.onServerDamageDealt -= GlobalEventManagerOnonServerDamageDealt;

        if (!bossGroupsToDamages.TryGetValue(bossGroup, out Dictionary<CharacterMaster, DamageCredit> bossGroupsToDamage)) return;
        if (Run.instance.participatingPlayerCount == 1 && onlyInMultiplayer.Value) return;

        List<KeyValuePair<CharacterMaster, DamageCredit>> damageOrdered = bossGroupsToDamage.ToList();
        damageOrdered.Sort((kvp, kvp2) => (kvp2.Value.damage + kvp2.Value.minionDamage).CompareTo(kvp.Value.damage + kvp.Value.minionDamage));

        if (useCrown.Value && !butterscotchnroses.clientSide.Value)
        {
            foreach (CharacterMaster master in CharacterMaster.readOnlyInstancesList)
            {
                master?.inventory?.RemoveItemPermanent(Crown.instance.ItemDef, 999);
            }
            foreach (GameObject crownObj in crownImages)
            {
                Object.DestroyImmediate(crownObj);
            }
        }
        
        bool printmvp = mastersTotalDamage.Count != 0;
        
        foreach (KeyValuePair<CharacterMaster, DamageCredit> kvp in damageOrdered)
        {
            if (!kvp.Key) continue;
            
            string name = TryGetName(kvp.Key);
            
            Chat.SendBroadcastChat(new Chat.SimpleChatMessage() { baseToken = $"<color=#e5eefc><style=cIsUtility>{name}</style> dealt <style=cIsDamage>{(kvp.Value.damage + kvp.Value.minionDamage):0} damage</style>!" + ((kvp.Value.minionDamage != 0) ? $" <style=cStack>({kvp.Value.damage:0} self, {kvp.Value.minionDamage:0} minion)</style>" : "" ) + "</color>"});
            Log.Debug($"{name} - {kvp.Value.damage:0} - {kvp.Value.minionDamage:0}");

            if (mastersTotalDamage.TryGetValue(kvp.Key, out float _))
            { 
                mastersTotalDamage[kvp.Key] += kvp.Value.damage + kvp.Value.minionDamage;
            }
            else
            {
                mastersTotalDamage.Add(kvp.Key, kvp.Value.damage + kvp.Value.minionDamage);
            }
        }

        List<KeyValuePair<CharacterMaster, float>> test = mastersTotalDamage.ToList();
        test.Sort((kvp, kvp2) => (kvp2.Value).CompareTo(kvp.Value));
        
        if (useCrown.Value && !butterscotchnroses.clientSide.Value)
        {
            test[0].Key.inventory.GiveItemPermanent(Crown.instance.ItemDef);
        }

        foreach (HUD hud in HUD.instancesList)
        {
            foreach (AllyCardController cardController in hud.allyCardManager.cardAllocator.elements)
            {
                cardController.InfoOverride();
            }
        }
        
        if (!printmvp) return;
        Chat.SendBroadcastChat(new Chat.SimpleChatMessage() { baseToken = $"<color=#e5eefc><style=cIsUtility>{TryGetName(test[0].Key)}</style> is the highest stage damage dealer with <style=cIsDamage>{(test[0].Value):0} damage</style>!</color>"});
    }

    private static string TryGetName(CharacterMaster characterMaster)
    {
        if (characterMaster.playerCharacterMasterController)
        {
            return characterMaster.playerCharacterMasterController.GetDisplayName();
        }
        
        string name = characterMaster.GetBody().baseNameToken;

        name = Language.GetString(name ?? characterMaster.bodyPrefab.GetComponent<CharacterBody>().baseNameToken);

        return name;

    }

    private void StartTracking(BossGroup bossGroup)
    {
        if (Run.instance.participatingPlayerCount == 1 && onlyInMultiplayer.Value) return;
        
        bossGroupsToDamages.Add(bossGroup, new Dictionary<CharacterMaster, DamageCredit>());
        GlobalEventManager.onServerDamageDealt += GlobalEventManagerOnonServerDamageDealt;
    }

    private class DamageCredit(float damage, float minionDamage, CharacterMaster master)
    {
        public float damage = damage;
        public float minionDamage = minionDamage;
        public CharacterMaster master = master;
    }

    //private Dictionary<CharacterMaster, DamageCredit> totalDamages = [];
    private void GlobalEventManagerOnonServerDamageDealt(DamageReport damageReport)
    {
        if (!damageReport.attackerMaster)
            return;
        if (!damageReport.victimIsBoss)
            return;

        bool exit = true;
        KeyValuePair<BossGroup, Dictionary<CharacterMaster, DamageCredit>> saved = default;
        foreach (KeyValuePair<BossGroup, Dictionary<CharacterMaster, DamageCredit>> kvp in bossGroupsToDamages)
        {
            if (!kvp.Key.combatSquad.membersList.Contains(damageReport.victimMaster)) continue;
            
            exit = false;
            saved = kvp;
            break;
        }
        if(exit)
            return;

        //attackerOwner logic
        if (damageReport.attackerOwnerMaster)
        {
            if (saved.Value.TryGetValue(damageReport.attackerOwnerMaster, out DamageCredit damageCreditMinion))
            {
                damageCreditMinion.minionDamage += damageReport.damageDealt;
            }
            else
            {
                saved.Value.Add(damageReport.attackerOwnerMaster, new DamageCredit(0, damageReport.damageDealt, damageReport.attackerOwnerMaster));
            }

            return;
        }

        if (saved.Value.TryGetValue(damageReport.attackerMaster, out DamageCredit damageCredit))
        {
            damageCredit.damage += damageReport.damageDealt;
        }
        else
        {
            saved.Value.Add(damageReport.attackerMaster, new DamageCredit(damageReport.damageDealt, 0, damageReport.attackerOwnerMaster));
        }
    }

    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("BNR - whodiddamage",
            "enable patches for whodiddamage",
            true,
            "");
        Utils.CheckboxConfig(enabled);
        
        useCrown = config.Bind("BNR - whodiddamage",
            "use crown icon",
            true,
            "adds a crown icon to the highest damage dealer on the scoreboard that persists going into the next stage");
        Utils.CheckboxConfig(useCrown);
        
        crownColorBasedOffBody = config.Bind("BNR - whodiddamage",
            "base crown icon color off body color",
            true,
            "makes the crown use body color ,.,.");
        Utils.CheckboxConfig(crownColorBasedOffBody);
        
        crownColorOverrides = config.Bind("BNR - whodiddamage",
            "crown color overrides",
            "",
            "crown color overrides for specific players steamids, formatted \"STEAM_0:1:174533492,#F3D2F7\"");
        Utils.StringConfig(crownColorOverrides);
        
        onlyInMultiplayer = config.Bind("BNR - whodiddamage",
            "disable in singleplayer",
            true,
            "whether to only enable in multiplayer or not .,.,");
        Utils.CheckboxConfig(onlyInMultiplayer);
    }

    private ConfigEntry<bool> enabled;
    private ConfigEntry<bool> useCrown;
    private ConfigEntry<bool> crownColorBasedOffBody;
    private ConfigEntry<bool> onlyInMultiplayer;
    private ConfigEntry<string> crownColorOverrides;
}