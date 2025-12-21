using BepInEx;
using BepInEx.Configuration;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace MalUrchinSpawner
{
    [BepInDependency("iDeathHD.UnityHotReload", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(ItemAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class MalUrchinSpawnerPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "icebro";
        public const string PluginName = "malurchinspawner";
        public const string PluginVersion = "1.0.0";
        
        public static ConfigEntry<float> urchinSeconds;

        public void Awake()
        {
            Log.Init(Logger);

            urchinSeconds = Config.Bind("Evil Urchin", "Evilness", 12f, "evil ,.,.,");
            
            On.RoR2.CharacterBody.OnBuffFirstStackGained += CharacterBodyOnOnBuffFirstStackGained;
        }

        private void CharacterBodyOnOnBuffFirstStackGained(On.RoR2.CharacterBody.orig_OnBuffFirstStackGained orig, CharacterBody self, BuffDef buffDef)
        {
            orig(self, buffDef);

            if (buffDef != RoR2Content.Buffs.AffixPoison) return;
            
            EvilUrchinSpawner spawner = self.gameObject.AddComponent<EvilUrchinSpawner>();
            spawner._characterBody = self;
        }
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

            if (!(urchinSpawnTimer > MalUrchinSpawnerPlugin.urchinSeconds.Value)) return;
            
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
            GameObject gameObject3 = UnityEngine.Object.Instantiate(
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
}
