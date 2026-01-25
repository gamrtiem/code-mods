using System;
using System.Linq;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;
using HarmonyLib;
using On.EntityStates.AffixVoid;
using RoR2;
using RoR2BepInExPack.GameAssetPaths;
using SS2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.TextCore.Text;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace MonarchStarstormEdits.patches;

public class etherealSaplingChanges : PatchBase<etherealSaplingChanges>
{
    [HarmonyPatch]
    public class Starstorm2EtherealChanges
    {
        [HarmonyPatch(typeof(SS2.Components.TraderController), "BeginTrade")]
        [HarmonyPrefix]
        public static void BeginTradePrefix(SS2.Components.TraderController __instance, int intPickupIndex)
        {
            Log.Debug(intPickupIndex);
        }
        
        [HarmonyPatch(typeof(EtherealBehavior), "SpawnShrine")]
        [HarmonyPrefix]
        public static bool SpawnShrinePostFix(EtherealBehavior __instance)
        {
            float chance = Run.instance.runRNG.RangeFloat(0, 1);
            if (chance <= spawnChance.Value)
            {
                //Log.Debug("spawning shrine !!");
                
                if (sendChatMessage.Value)
                {
                    Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                    {
                        baseToken = chatMessage.Value
                    });
                }
                
                return true;
            }
            
            //Log.Debug("not spawning ethereal shrine !! ");
            
            return false;
        }
        
        [HarmonyPatch(typeof(EtherealBehavior), "Start")]
        [HarmonyPrefix]
        public static void StartPostFix(EtherealBehavior __instance)
        {
            EtherealBehavior.alwaysReplaceNewts = false;
        }
        
        [HarmonyPatch(typeof(EtherealBehavior), "ReplaceRandomNewtStatue")]
        [HarmonyPrefix]
        public static bool ReplaceRandomNewtStatuePostFix(EtherealBehavior __instance)
        {
            PortalStatueBehavior[] statues = Object.FindObjectsOfType<PortalStatueBehavior>(true).Where(p => p.portalType == PortalStatueBehavior.PortalType.Shop).ToArray();
            PortalStatueBehavior[] disabledStatues = statues.Where(p => !p.gameObject.activeInHierarchy).ToArray();
           
            if (disabledStatues.Length > 0)
            {
                Transform newt = disabledStatues[Random.Range(0, disabledStatues.Length)].transform;
                GameObject term = Object.Instantiate(EtherealBehavior.shrinePrefab, newt.position + Vector3.up * -1.2f, newt.rotation);
                NetworkServer.Spawn(term);     
            }
            else
            {
                Log.Debug("idk something bad happened ,..,,.");
            }
            
            return false;
        }
        
        [HarmonyPatch(typeof(ShrineEtherealBehavior), "ActivateEtherealTerminal")]
        [HarmonyPrefix]
        public static bool ActivateEtherealTerminalPrefix(ShrineEtherealBehavior __instance, Interactor interactor)
        {
            if (!awesomeEtherealVFX.Value)
            {
                return true;
            }
            
            if (__instance.purchaseCount >= 1)
            {
                Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
                {
                    baseToken = "<style=cIsHealing>The gods feel as if mocked...</style>"
                });
                
                __instance.gameObject.GetComponent<EtherealShrineActivator>().kill = true;
                if (__instance.childLocator != null)
                {
                    __instance.childLocator.FindChild("Loop").gameObject.SetActive(false);
                }
                __instance.waitingForRefresh = false;
                __instance.purchaseInteraction.SetAvailable(false);
                
                GameObject VFX = __instance.transform.Find("ChargeBFG(Clone)").gameObject;
                ParticleSystem[] VFXparticles =
                [
                    VFX.transform.GetChild(1).GetChild(0).GetComponent<ParticleSystem>(), // Find("Sparks")
                    VFX.transform.GetChild(1).GetChild(1).GetComponent<ParticleSystem>(), // Find("ChargeRing")
                    VFX.transform.GetChild(1).GetChild(2).GetComponent<ParticleSystem>(), // Find("Distortion")
                    VFX.transform.GetChild(1).GetChild(3).GetComponent<ParticleSystem>(), // Find("DistortionRim")
                    VFX.transform.GetChild(1).GetChild(4).GetComponent<ParticleSystem>()  // Find("Lightning")
                ];

                foreach (ParticleSystem particleSystem in VFXparticles)
                {
                    particleSystem.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                }
                
                ShakeEmitter theshaker = __instance.gameObject.AddComponent<ShakeEmitter>();
                theshaker.shakeOnStart = true;
                theshaker.duration = 0.5f;
                theshaker.radius = 200f;
                theshaker.startDelay = 0;
                theshaker.wave = new Wave()
                {
                    amplitude = 0.5f,
                    frequency = 3f,
                    cycleOffset = 0f
                };
                theshaker.amplitudeTimeDecay = true;
                theshaker.StartShake();
                
                __instance.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<ParticleSystem>().Stop(true, ParticleSystemStopBehavior.StopEmitting);
                return false;
            }
            
            //__instance.purchaseInteraction.SetAvailable(false);
            __instance.purchaseInteraction.contextToken = "SS2_ETHEREAL_WARNING2";
            __instance.purchaseInteraction.displayNameToken = "SS2_ETHEREAL_NAME2";
            __instance.purchaseCount++;
            __instance.refreshTimer = 2;

            Util.PlaySound("Play_UI_shrineActivate", __instance.gameObject);

            CharacterBody body = interactor.GetComponent<CharacterBody>();
            Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
            {
                subjectAsCharacterBody = body,
                baseToken = "SS2_SHRINE_ETHEREAL_WARN_MESSAGE",
            });

            if (__instance.childLocator != null)
            {
                __instance.childLocator.FindChild("Loop").gameObject.SetActive(true);
                __instance.childLocator.FindChild("Symbol").gameObject.SetActive(false);
            }

            __instance.waitingForRefresh = true;
            
            #region vfx
            GameObject chargeVFX = Object.Instantiate(Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_35_0.RoR2_Base_BFG.ChargeBFG_prefab).WaitForCompletion(), __instance.transform);
            ParticleSystem[] particles =
            [
                chargeVFX.transform.GetChild(1).GetChild(0).GetComponent<ParticleSystem>(), // Find("Sparks")
                chargeVFX.transform.GetChild(1).GetChild(1).GetComponent<ParticleSystem>(), // Find("ChargeRing")
                chargeVFX.transform.GetChild(1).GetChild(2).GetComponent<ParticleSystem>(), // Find("Distortion")
                chargeVFX.transform.GetChild(1).GetChild(3).GetComponent<ParticleSystem>(), // Find("DistortionRim")
                chargeVFX.transform.GetChild(1).GetChild(4).GetComponent<ParticleSystem>()  // Find("Lightning")
            ];
          
            particles[3].gameObject.SetActive(true); // ring distortion is usually disabled .,,. 
           
            for(int i = 0; i < particles.Length; i++)
            {
                ParticleSystem.MainModule main = particles[i].main;
                main.duration *= 1 / 0.3f;
                particles[i].transform.localScale *= 3f;

                switch (i)
                {
                    case 0: // sparks
                        main.simulationSpeed = 1f;
                        main.maxParticles = 400;
                        main.startSizeMultiplier = 1.2f;
                        ParticleSystem.EmissionModule emissionModule =  particles[0].emission;
                        ParticleSystem.MinMaxCurve minMaxCurve = emissionModule.rateOverTime;
                        Log.Debug(minMaxCurve.curve.keys[1].value);
                        minMaxCurve.curve.keys[0].value = 0.6f;
                        Log.Debug(minMaxCurve.curve.keys[0].value);

                        //ParticleSystem.ShapeModule shape = particles[0].shape;
                        //shape.radius = 1f;
                        //main.startLifetimeMultiplier = 1.5f;
                        
                        break;
                    case 1: // ring
                        main.startSize = 2f;
                        main.simulationSpeed = 0.3f;
                        main.duration = 2;
                        break;
                    case 2: // distortion
                        main.startSize = 4f;
                        main.simulationSpeed = 0.3f;
                        main.duration = 2;
                        break;
                    case 3: // distortion rim
                        main.startSize = 4f;
                        main.startLifetime = 2f;
                        main.simulationSpeed = 0.3f;
                        main.duration = 2;
                        main.maxParticles = 1;

                        Gradient newLifetimeColorKeys = particles[3].colorOverLifetime.color.gradient;
                        GradientColorKey[] keys = newLifetimeColorKeys.colorKeys;
                        keys[0].color = new Color(0, 1, 0, 1);
                        newLifetimeColorKeys.colorKeys = keys;
                        ParticleSystem.ColorOverLifetimeModule colOverLifetimeModule = particles[3].colorOverLifetime;
                        colOverLifetimeModule.color = newLifetimeColorKeys;
                        break;
                    case 4: // little lightning
                        main.startSizeMultiplier = 2f;
                        break;
                }
                    
            }
            
            Vector3 pos = chargeVFX.transform.localPosition;
            pos.y += 2.3f; 
            pos.z -= 0.3f; 
            chargeVFX.transform.localPosition = pos; 
            #endregion
            
            __instance.gameObject.AddComponent<EtherealShrineActivator>().interactor = interactor;
            
            return false;
        }
    }

    public class EtherealShrineActivator : MonoBehaviour
    {
        public float timer;
        public Interactor interactor;
        public bool kill;

        private void FixedUpdate()
        {
            //bfg vfx usually end around 2~ seconds in ? 
            timer += Time.fixedDeltaTime;
            if (timer >= 2f * (1 / 0.3f))
            {
                if (kill)
                {
                    Destroy(this);
                    return;
                }
                
                ShrineEtherealBehavior shrineBehavior = this.GetComponent<ShrineEtherealBehavior>();
                //second activate behavior .,., 
                shrineBehavior.DisableShrine(null);
                shrineBehavior.waitingForRefresh = true;

                if (TeleporterUpgradeController.instance)
                    TeleporterUpgradeController.instance.UpgradeEthereal();

                CharacterBody charBody = interactor.GetComponent<CharacterBody>();
                Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
                {
                    subjectAsCharacterBody = charBody,
                    baseToken = "SS2_SHRINE_ETHEREAL_USE_MESSAGE",
                });
                
                Util.PlaySound("EtherealBell", shrineBehavior.gameObject);

                shrineBehavior.purchaseCount++;
                shrineBehavior.refreshTimer = 2;

                ShakeEmitter theshaker = this.gameObject.AddComponent<ShakeEmitter>();
                theshaker.shakeOnStart = true;
                theshaker.duration = 0.25f;
                theshaker.radius = 200f;
                theshaker.startDelay = 0;
                theshaker.wave = new Wave()
                {
                    amplitude = 0.5f,
                    frequency = 3f,
                    cycleOffset = 0f
                };
                theshaker.amplitudeTimeDecay = true;
                theshaker.StartShake();
                
                transform.Find("ChargeBFG(Clone)").gameObject.SetActive(false);
                
                Destroy(this);
            }
        }
    }

    public override void Init(Harmony harmony)
    {
        if (!etherealChanges.Value) return;
        harmony.CreateClassProcessor(typeof(Starstorm2EtherealChanges)).Patch();
    }

    public override void Config(ConfigFile config)
    {
        etherealChanges = config.Bind("ethereal shrine change s,s,.,.", 
            "apply any ethereal sapling changes at al l !!", 
            true,
            "byeah ,.,");
        spawnChance = config.Bind("ethereal shrine change s,s,.,.", 
            "chance in percent to spawn ethereal shrine !! 1 is 100% ,.,,.", 
            0.5f,
            "byeah ,.,");
        sendChatMessage = config.Bind("ethereal shrine change s,s,.,.", 
            "should if a shrine spawn send a chat message !!", 
            true,
            "byeah ,.,");
        chatMessage = config.Bind("ethereal shrine change s,s,.,.", 
            "said chat message if chat message enabled ,,.", 
            "<style=\"cIsHealing\">Unknown forces start to coalesce...</style>",
            "byeah ,.,");
        awesomeEtherealVFX = config.Bind("ethereal shrine change s,s,.,.", 
            "change ethereal shrines to hit only once and again to cancel !! cool vfx...,,.", 
            true,
            "byeah ,.,");
    }
    
    private static ConfigEntry<bool> etherealChanges;
    private static ConfigEntry<float> spawnChance;
    private static ConfigEntry<bool> sendChatMessage;
    private static ConfigEntry<string> chatMessage;
    private static ConfigEntry<bool> awesomeEtherealVFX;
}