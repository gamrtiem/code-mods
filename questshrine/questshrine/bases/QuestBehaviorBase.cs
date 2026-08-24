using System;
using System.Collections.Generic;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Networking;
using CharacterMaster = RoR2.CharacterMaster;
using Object = UnityEngine.Object;

namespace questshrine.bases;

public abstract class QuestBehaviorBase : NetworkBehaviour
{
    public static List<QuestBehaviorBase> activeQuests = [];
    public abstract QuestBase QuestBase { get; }
    public abstract Type ObjectiveType { get; }
    public static int notificationEnum = 1225;
    
    [SyncVar]
    public string QuestDescInternal;
    [SyncVar(hook = nameof(OnSyncTarget))]
    public GameObject targetMasterObject;

    public CharacterBody body
    {
        get
        {
            if(charMaster)
                return charMaster.GetBody();
            if(targetMasterObject)
                charMaster = targetMasterObject.GetComponent<CharacterMaster>();
            return charMaster?.GetBody();
        }
    }
    public CharacterMaster charMaster;
    public QuestInterfaceListener listener;

    private int startingStage;
    private bool ranStart;
    
    public void OnSyncTarget(GameObject newTarget)
    {
        targetMasterObject = newTarget;
        charMaster = newTarget ? newTarget.gameObject.GetComponent<CharacterMaster>() : null;
        Log.Debug($"syncing target {newTarget} {charMaster}");
        
        if (!ranStart)
        {
            ranStart = true;
            StartQuest();
        }
    }
    
    private void Awake()
    {
        activeQuests.Add(this);
        if (targetMasterObject)
        {
            charMaster = targetMasterObject.GetComponent<CharacterMaster>();
        }
        startingStage = Run.instance.stageClearCount;
    }

    [ClientRpc]
    public void RpcStartQuest()
    {
        //StartQuest();
        Log.Debug("rpc ran");
    }
    
    public virtual void StartQuest()
    {
        Log.Debug(charMaster.GetBody().baseNameToken + " starting quest");
        Log.Debug(LocalUserManager.GetFirstLocalUser().cachedMaster.GetBody().baseNameToken + " local player");
        //if (LocalUserManager.GetFirstLocalUser().cachedMaster == charMaster)
        //{
        //    Log.Debug("local player was master !");
            ObjectivePanelController.collectObjectiveSources += OnCollectObjectiveSources;
        //}
        
        CharacterMasterNotificationQueue notificationQueueForMaster = CharacterMasterNotificationQueue.GetNotificationQueueForMaster(charMaster);
        var info = new CharacterMasterNotificationQueue.NotificationInfo(QuestBase, new CharacterMasterNotificationQueue.TransformationInfo((CharacterMasterNotificationQueue.TransformationType)notificationEnum, null), new CharacterMasterNotificationQueue.CustomOverrideInfo()
            {
                titleText = QuestBase.QuestTitle,
                descriptionText = QuestDescInternal,
                iconColor = new Color(1f, 1f, 1f, 1f)
            }, showExtra: false);

        if (notificationQueueForMaster)
        {
            if (notificationQueueForMaster.notifications.Count != 0)
            {
                notificationQueueForMaster.notifications.Add(
                    new CharacterMasterNotificationQueue.TimedNotificationInfo
                    {
                        notification = info,
                        startTime = Run.instance.fixedTime,
                        duration = 3f
                    });
            }
            else
            {
                notificationQueueForMaster.PushNotification(info, 3f);
            }
        }

        charMaster.onBodyStart += AddListenersNewStageCheck;
        AddListenersNewStageCheck(body);
    }
    
    private void AddListenersNewStageCheck(CharacterBody body)
    {
        if (startingStage != RoR2.Run.instance.stageClearCount)
        {
            Destroy(this);
            return;
        }
        
        if (!QuestBase.useListeners) return;
        
        if (body.gameObject.TryGetComponent(out listener))
        {
            listener.questListeners.Add(this);
            return;
        }
        
        listener = body.gameObject.AddComponent<QuestInterfaceListener>();
        listener.questListeners.Add(this);
    }

    protected Action<DamageReport> KilledOtherServer;
    protected Action<DamageReport> TakeDamageServer;

    public class QuestInterfaceListener : MonoBehaviour, IOnKilledOtherServerReceiver, IOnTakeDamageServerReceiver
    {
        public List<QuestBehaviorBase> questListeners = [];

        public void OnEnable()
        {
            HG.ArrayUtils.ArrayAppend(ref gameObject.GetComponent<CharacterBody>().healthComponent.onTakeDamageReceivers, this);
        }

        public void OnKilledOtherServer(DamageReport damageReport)
        {
            foreach (QuestBehaviorBase questBehaviorBase in questListeners)
            {
                questBehaviorBase?.KilledOtherServer?.Invoke(damageReport);
            }
        }

        public void OnTakeDamageServer(DamageReport damageReport)
        {
            foreach (QuestBehaviorBase questBehaviorBase in questListeners)
            {
                questBehaviorBase?.TakeDamageServer?.Invoke(damageReport);
            }
        }
    }

    [ClientRpc]
    public void RpcRetire()
    {
        enabled = false;
    }
    
    public virtual void OnDisable()
    {
        ObjectivePanelController.collectObjectiveSources -= OnCollectObjectiveSources;
        if (QuestBase.useListeners)
        {
            listener.questListeners.Remove(this);
            if (listener.questListeners.Count == 0)
            {
                Destroy(listener);
            }
        }
        charMaster.onBodyStart -= AddListenersNewStageCheck;
        activeQuests.Remove(this);
    }

    public virtual void OnCollectObjectiveSources(CharacterMaster master, List<ObjectivePanelController.ObjectiveSourceDescriptor> objectiveSourcesList)
    {
        var newObjective = new ObjectivePanelController.ObjectiveSourceDescriptor
        {
            master = master,
            objectiveType = ObjectiveType,
            source = this
        };

        objectiveSourcesList.Add(newObjective);
    }
}