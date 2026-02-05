using static BNR.butterscotchnroses;
using BNR.patches;
using BepInEx.Configuration;
using HarmonyLib;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace BNR;

public class malachite : PatchBase<malachite>
    {
    public override void Init(Harmony harmony)
    {     
        applyHooks();
    }
        
    private void applyHooks()
    {
        if (enabled.Value)
        {
            On.RoR2.CharacterBody.OnBuffFirstStackGained += CharacterBodyOnOnBuffFirstStackGained;
        }
        else
        {
            On.RoR2.CharacterBody.OnBuffFirstStackGained += CharacterBodyOnOnBuffFirstStackGained;
        }
    }

    private void CharacterBodyOnOnBuffFirstStackGained(On.RoR2.CharacterBody.orig_OnBuffFirstStackGained orig, CharacterBody self, BuffDef buffDef)
    {
        orig(self, buffDef);

        if (buffDef != RoR2Content.Buffs.AffixPoison) return;
                
        EvilUrchinSpawner spawner = self.gameObject.AddComponent<EvilUrchinSpawner>();
        spawner._characterBody = self;
    }

    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("BNR - malachite",
            "enable patches for malachite (have them naturally spawn urchins ,.,.",
            true,
            "");
        BNRUtils.CheckboxConfig(enabled);
        enabled.SettingChanged += (_, _) =>
        {
            applyHooks();
        };
        
        timeBetweenSpawns = config.Bind("BNR - malachite",
            "time between urchin spawns .,,.",
            12f,
            "byeah.,,");
        BNRUtils.SliderConfig(1, 30, timeBetweenSpawns);
    }

    private ConfigEntry<bool> enabled;
    public static ConfigEntry<float> timeBetweenSpawns;
}

public class EvilUrchinSpawner : MonoBehaviour
{
    public CharacterBody _characterBody;
    private float urchinSpawnTimer;

    private void OnEnable()
    {
        Log.Debug("added urchin spawner !!");
    }

    private void FixedUpdate()
    {
        urchinSpawnTimer += Time.fixedDeltaTime;

        if (!(urchinSpawnTimer > malachite.timeBetweenSpawns.Value)) return;
            
        urchinSpawnTimer = 0f;
        Log.Debug("spawning urgin !!");

        if (!_characterBody || !_characterBody.HasBuff(RoR2Content.Buffs.AffixPoison))
        {
            Log.Debug("uh oh,,");
            Destroy(this);
            return;
        }

        Vector3 position2 = transform.position;
        var ray = (_characterBody.inputBank
            ? _characterBody.inputBank.GetAimRay()
            : new Ray(transform.position, transform.rotation * Vector3.forward));
        Quaternion rotation = Quaternion.LookRotation(ray.direction);
        GameObject gameObject3 = Instantiate(
            LegacyResourcesAPI.Load<GameObject>("Prefabs/CharacterMasters/UrchinTurretMaster"), position2,
            rotation);
        CharacterMaster component3 = gameObject3.GetComponent<CharacterMaster>();
        if ((bool)component3)
        {
            component3.teamIndex = _characterBody.teamComponent.teamIndex;
            NetworkServer.Spawn(gameObject3);
            component3.SpawnBodyHere();
        }
    }
}
