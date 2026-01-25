using System;
using BepInEx;
using R2API;
using RoR2;
using UnityEngine;
using Chat = On.RoR2.Chat;
using LightningOrb = RoR2.Orbs.LightningOrb;
using OrbManager = RoR2.Orbs.OrbManager;

namespace NewtoPoloPlugin
{
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    public class NewtoPoloPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Coffee";
        public const string PluginName = "NewtoPolo";
        public const string PluginVersion = "0.1.2";
        
        public void Awake()
        {
            Log.Init(Logger);
            
            Chat.UserChatMessage.OnProcessed += UserChatMessageOnProcessed;
        }

        private void UserChatMessageOnProcessed(Chat.UserChatMessage.orig_OnProcessed orig, RoR2.Chat.UserChatMessage self)
        {
            bool newt = self.text.ToLower().Contains("newto");
            bool ethereal = self.text.ToLower().Contains("saplo");
            bool node = self.text.ToLower().Contains("nodo");
            
            if (!self.sender || !self.sender.GetComponent<NetworkUser>() || (!newt && !ethereal && !node)) return;
            
            int num = 0;

            if (AccessCodesMissionController.instance != null)
            {
                foreach (var instances in AccessCodesMissionController.instance.nodes)
                {
                    //Log.Debug("GenericDisplayNameProvider" + instances.node.gameObject.name);
                    if (instances.node.gameObject.name.Contains("Access Codes Node") && node)
                    {
                        sendElectricity(self.sender.GetComponent<NetworkUser>().GetCurrentBody(), instances.node.transform.position, "ACCESSCODES", instances.node.gameObject, "Access Node");
                        num++;
                    }
                }
            }
            
            foreach (PurchaseInteraction instances in InstanceTracker.GetInstancesList<PurchaseInteraction>())
            {
                //Log.Debug(instances.displayNameToken.ToUpper());
                if (((instances.displayNameToken.ToUpper() != "NEWT_STATUE_NAME" || !newt) 
                     && (!instances.displayNameToken.ToUpper().Contains("SS2_SHRINE_ETHEREAL") || !ethereal) 
                    && (!instances.displayNameToken.ToUpper().Contains("ACCESSCODES") || !node)) 
                    || !instances.Networkavailable) continue;
                sendElectricity(self.sender.GetComponent<NetworkUser>().GetCurrentBody(), instances.transform.position, instances.displayNameToken.ToUpper(), instances.gameObject, instances.GetDisplayName());

               
                num++;

                
            }
            if (num == 0)
            {
                RoR2.Chat.SendBroadcastChat(new RoR2.Chat.SimpleChatMessage
                {
                    baseToken = "You hear nothing but the void and it's deafening silence."
                });
            }
        }

        public void sendElectricity(CharacterBody playerBody, Vector3 pos, string token, GameObject gameObject, string displayName)
        {
            Transform transform = playerBody.transform;
            Vector3 vector = pos - transform.localPosition;
            Vector3 effectSpawn = transform.localPosition + 25f * new Vector3
            {
                x = vector.x,
                y = 0f,
                z = vector.z
            }.normalized;
            string text = "";
            text = ((vector.magnitude > 350f) ? (text + "that sounded like it came from very far away") : ((!(vector.magnitude > 75f)) ? (text + "that sounded like it was nearby") : (text + "that sounded like it was in the distance")));
            if (vector.y > 75f)
            {
                text += ", up above the clouds";
            }
            else if (vector.y < -75f)
            {
                text += ", in dark depths of the world";
            }
            
            var color = "#02f7e7";
            var polo = "POLO";
            if (token.Contains("SS2_SHRINE_ETHEREAL"))
            {
                color = "#02f77d";
                polo = "SAPOLO";
            }
            else if (token.Contains("ACCESSCODES"))
            {
                color = "#ff4c76";
                polo = "ACCESSOLO";
            }
            string chatMessage = "<color=" + color + ">" + displayName + "</color>: " + polo + " (" + text + ")";
            this.Invoke(delegate
            {
                announceAltar(chatMessage, gameObject, effectSpawn, playerBody);
            }, 0.5f);                        
        }

        public void announceAltar(string chatMessage, GameObject altar, Vector3 effectSpawn, CharacterBody player)
        {
            RoR2.Chat.SendBroadcastChat(new RoR2.Chat.SimpleChatMessage
            {
                baseToken = chatMessage
            });
            
            EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ShrineUseEffect"), new EffectData
            {
                origin = altar.transform.position,
                rotation = Quaternion.identity,
                scale = 10f,
                color = ColorCatalog.GetColor(ColorCatalog.ColorIndex.LunarItem)
            }, transmit: true);
            
            OrbManager.instance.AddOrb(new LightningOrb()
            {
                origin = effectSpawn,
                attacker = altar.gameObject,
                target = player.mainHurtBox,
                damageValue = 25f + Run.instance.difficultyCoefficient * (float)Math.Pow(1.8, Run.instance.shopPortalCount) * 5f,
                damageColorIndex = DamageColorIndex.Void,
                damageType = DamageType.LunarSecondaryRootOnHit,
                isCrit = true,
                procCoefficient = 1f
            });
        }
    }
}
