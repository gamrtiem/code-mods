using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using questshrine.bases;
using questshrine.content.componentns;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.UI;
using ShaderSwapper;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace questshrine
{
    [BepInDependency("iDeathHD.UnityHotReload", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class questshrine : BaseUnityPlugin
    {
        private const string PluginGUID = PluginAuthor + "." + PluginName;
        private const string PluginAuthor = "kina";
        private const string PluginName = "questshrine";
        private const string PluginVersion = "0.1.0";

        private static bool UHRInstalled => Chainloader.PluginInfos.ContainsKey("iDeathHD.UnityHotReload");

        public static questshrine instance;
        public static AssetBundle bundle;
        private static Material shrineMat = Addressables.LoadAssetAsync<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_35_0.RoR2_Base_ShrineBlood.matShrineBlood_mat).WaitForCompletion();
        private static GameObject notifPrefab;
        public void Awake()
        {
            Log.Init(Logger);

            instance = this;
            bundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location)!, "questshrinebundle"));
            StartCoroutine(bundle.UpgradeStubbedShadersAsync());

            LoadBases();
            LoadShrine();

            notifPrefab = bundle.LoadAsset<GameObject>("NotificationPanel2");
            IL.RoR2.UI.NotificationUIController.SetUpNotification += AddCustomNotificationIL;
        }

        public void AddCustomNotificationIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            
            try
            { 
                /*
                 * // currentNotification = Object.Instantiate(lowerPricedChestsRegenTransformationNotificationPrefab).GetComponent<GenericNotification>();
                   IL_00eb: ldarg.0
                   IL_00ec: ldloc.0
                   IL_00ed: call !!0 [UnityEngine.CoreModule]UnityEngine.Object::Instantiate<class [UnityEngine.CoreModule]UnityEngine.GameObject>(!!0)
                   IL_00f2: callvirt instance !!0 [UnityEngine.CoreModule]UnityEngine.GameObject::GetComponent<class RoR2.UI.GenericNotification>()
                   IL_00f7: stfld class RoR2.UI.GenericNotification RoR2.UI.NotificationUIController::currentNotification
                 */

                if (c.TryGotoNext(x => x.MatchLdarg(0),
                        x => x.MatchLdloc(0),
                        x => x.MatchCallOrCallvirt<UnityEngine.Object>("Instantiate"),
                        x => x.MatchCallOrCallvirt<GameObject>("GetComponent"),
                        x => x.MatchStfld<NotificationUIController>("currentNotification")))
                {
                    Log.Debug("matched !! yayy");
                    c.Index++; // stupid and dumb but previously would add the il right before the spot where all the switch statements jumped to so TT ,.,.
                    c.Emit(OpCodes.Ldarg_0); // NotificationUIController
                    c.Emit(OpCodes.Ldarg_1); // NotificationInfo
                    c.Emit(OpCodes.Ldloc_S, (byte)0); //current transofrm prefab
                    c.EmitDelegate<Func<NotificationUIController, CharacterMasterNotificationQueue.NotificationInfo, GameObject, GameObject>>(
                    (notifUIControl, notifInfo, prevNotifPrefab) =>
                    {
                        if (notifInfo?.transformation?.transformationType != (CharacterMasterNotificationQueue.TransformationType)(QuestBehaviorBase.notificationEnum)) return prevNotifPrefab;
                        
                        //Debug.Log($"transform type was same as enu m! {notifInfo?.transformation?.transformationType}");
                        notifUIControl.LowerPricedChestsRegenTransformationNotificationPrefab = notifPrefab;
                        return notifPrefab;

                    });
                    c.Emit(OpCodes.Stloc_0);
                    
                    //go after the instantiating
                    c.Index += 4;
                    c.Emit(OpCodes.Ldarg_0); // NotificationUIController
                    c.Emit(OpCodes.Ldarg_1); // NotificationInfo
                    c.Emit(OpCodes.Ldarg_0); // NotificationUIController
                    c.Emit(OpCodes.Ldfld, typeof(NotificationUIController).GetField("currentNotification", BindingFlags.Instance | BindingFlags.Default | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.GetField | BindingFlags.NonPublic)); //GenericNotification
                    c.EmitDelegate<Action<NotificationUIController, CharacterMasterNotificationQueue.NotificationInfo, GenericNotification>>(
                        (notifUIControl, notifInfo, genericNotif) =>
                        {
                            if (notifInfo?.transformation?.transformationType != (CharacterMasterNotificationQueue.TransformationType)(QuestBehaviorBase.notificationEnum)) return;
                            //Debug.Log($"transform type was same as enu m! {notifInfo?.transformation?.transformationType}");
                            QuestBase questBase = notifInfo.data as QuestBase;
                            genericNotif.titleText.token = Language.GetString(questBase.QuestTitle);
                            genericNotif.descriptionText.token = Language.GetString(questBase.QuestDesc);
                            genericNotif.iconImage.texture = questBase.QuestIcon.texture;
                        });
                }
                else
                {
                    Log.Error("fuck !!!!!!!!!!!!!!! couldnt match notifictation il .,. dlc4 probablys idk.,,..,");
                    QuestBehaviorBase.notificationEnum = (int)CharacterMasterNotificationQueue.TransformationType.Default;
                }
            }
            catch (Exception e)
            {
                Log.Error("error while il patching notifiction ! dlc4 probably killed me,.,.");
                Log.Error(e);
                QuestBehaviorBase.notificationEnum = (int)CharacterMasterNotificationQueue.TransformationType.Default;
            }
        }

        public readonly Dictionary<QuestBase, GameObject> questObjectCatalog = [];
        private void LoadBases()
        {
            IEnumerable<Type> quests = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(QuestBase)));
            foreach (Type questItem in quests)
            {
                QuestBase quest = (QuestBase)Activator.CreateInstance(questItem);
                quest.Init(Config);
                if (!quest.enabled) continue;
                
                GameObject questPrefab = PrefabAPI.CreateEmptyPrefab($"quest prefab {quest.QuestName}", true);
                questPrefab.AddComponent(quest.Behavior);
                questObjectCatalog.Add(quest, questPrefab);
            }
        }

        private void LoadShrine()
        {
            GameObject questshrine = bundle.LoadAsset<GameObject>("questshrineprefab");
            questshrine.transform.Find("chanceshrine").Find("shrine").GetComponent<MeshRenderer>().material = shrineMat;
            
            QuestShrineComponent qsc = questshrine.AddComponent<QuestShrineComponent>();
            PurchaseInteraction interaction = questshrine.GetComponent<PurchaseInteraction>();
            qsc.purchaseInteraction = interaction;

            ConfigEntry<int> shrineWeight = instance.Config.Bind("Quest Shrine", "Shrine selection weight", 3, "selection weight of how common quest shrine should spawn .,., for reference mountain shrines are usually weight of 1 ,., chance a weight of 2 .,,. and combat a weight of 3 ,..,");
            InteractableSpawnCard questisc = bundle.LoadAsset<InteractableSpawnCard>("questshrineisc");
            DirectorCard directorCard = new DirectorCard
            {
                selectionWeight = shrineWeight.Value, 
                spawnCard = questisc,
            };

            DirectorAPI.DirectorCardHolder directorCardHolder = new DirectorAPI.DirectorCardHolder
            {
                Card = directorCard,
                InteractableCategory = DirectorAPI.InteractableCategory.Shrines
            };
            
            DirectorAPI.Helpers.AddNewInteractable(directorCardHolder);
            ContentAddition.AddNetworkedObject(questshrine);
            /*List<DirectorAPI.Stage> stageList =
            [
                DirectorAPI.Stage.DistantRoost,
                DirectorAPI.Stage.TitanicPlains,
                DirectorAPI.Stage.TitanicPlainsSimulacrum,
                DirectorAPI.Stage.AbandonedAqueduct,
                DirectorAPI.Stage.AbandonedAqueductSimulacrum,
                DirectorAPI.Stage.AbyssalDepths,
                DirectorAPI.Stage.AbyssalDepthsSimulacrum,
                DirectorAPI.Stage.AphelianSanctuary,
                DirectorAPI.Stage.AphelianSanctuarySimulacrum,
                DirectorAPI.Stage.CommencementSimulacrum,
                DirectorAPI.Stage.RallypointDelta,
                DirectorAPI.Stage.RallypointDeltaSimulacrum,
                DirectorAPI.Stage.ScorchedAcres,
                DirectorAPI.Stage.SiphonedForest,
                DirectorAPI.Stage.SirensCall,
                DirectorAPI.Stage.SkyMeadow,
                DirectorAPI.Stage.SkyMeadowSimulacrum,
                DirectorAPI.Stage.SulfurPools,
                DirectorAPI.Stage.SunderedGrove,
                DirectorAPI.Stage.WetlandAspect,
            ];

            foreach (DirectorAPI.Stage stage in stageList)
            {
                DirectorAPI.Helpers.AddNewInteractableToStage(directorCardHolder, stage);
            }*/
        }
         
        private void Update()
        {
#if DEBUG
            if (Input.GetKeyUp(KeyCode.F7))
            {
                if (UHRInstalled)
                {
                    Log.Debug(nameof(questshrine) + ".dll");
                    UHRSupport.hotReload(typeof(questshrine).Assembly, System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location)!, nameof(questshrine) + ".dll"));
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
