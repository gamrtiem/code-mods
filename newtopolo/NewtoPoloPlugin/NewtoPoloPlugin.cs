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
    [BepInDependency(ItemAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    public class NewtoPoloPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Coffee";
        public const string PluginName = "NewtoPolo";
        public const string PluginVersion = "0.1.1";
        
        public void Awake()
        {
            Log.Init(Logger);
            
            Chat.UserChatMessage.OnProcessed += UserChatMessageOnProcessed;
        }

        private void UserChatMessageOnProcessed(Chat.UserChatMessage.orig_OnProcessed orig, RoR2.Chat.UserChatMessage self)
        {
            Log.Debug("AAAAAAAAAAA");
            var newt = self.text.ToLower().Contains("newto");
            var ethereal = self.text.ToLower().Contains("saplo");
            if (self.sender && self.sender.GetComponent<NetworkUser>() && (newt || ethereal))
            {
                int num = 0;
                foreach (PurchaseInteraction instances in InstanceTracker.GetInstancesList<PurchaseInteraction>())
                {
                    if ((instances.displayNameToken.ToUpper() == "NEWT_STATUE_NAME" && newt || instances.displayNameToken.ToUpper().Contains("SS2_SHRINE_ETHEREAL") && ethereal) && instances.Networkavailable)
                    {
                        CharacterBody playerBody = self.sender.GetComponent<NetworkUser>().GetCurrentBody();
                        Transform transform = playerBody.transform;
                        Vector3 vector = instances.transform.position - transform.localPosition;
                        Vector3 effectSpawn = transform.localPosition + 25f * new Vector3
                        {
                            x = vector.x,
                            y = 0f,
                            z = vector.z
                        }.normalized;
                        string text = "";
                        text = ((vector.magnitude > 350f) ? (text + "that sounded like it came from very far away") : ((!(vector.magnitude > 75f)) ? (text + "that sounded like it was very nearby") : (text + "that sounded like it was in the distance")));
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
                        if (instances.displayNameToken.ToUpper().Contains("SS2_SHRINE_ETHEREAL"))
                        {
                            color = "#02f77d";
                            polo = "SAPOLO";
                        }
                        string chatMessage = "<color=" + color + ">" + instances.GetDisplayName() + "</color>: " + polo + " (" + text + ")";
                        this.Invoke(delegate
                        {
                            announceAltar(chatMessage, instances, effectSpawn, playerBody);
                        }, 0.5f);                        
                        num++;
                    }
                }
                if (num == 0)
                {
                    RoR2.Chat.SendBroadcastChat(new RoR2.Chat.SimpleChatMessage
                    {
                        baseToken = "You hear nothing but the void and it's deafening silence."
                    });
                }
            }
        }

        public void announceAltar(string chatMessage, PurchaseInteraction altar, Vector3 effectSpawn, CharacterBody player)
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
