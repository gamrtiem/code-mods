//NETWORKING TODO
//Run.instance.runRNG isnt available on clients; make sure 
using System;
using BepInEx.Configuration;
using questshrine.bases;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace questshrine.content.quests;

public class NoDamage : QuestBase<NoDamage>
{
    public override string QuestName => "No Damage";
    public override string QuestTitle => "<style=cWorldEvent>the planet seeks entertainment .,,..</style>";
    public override string QuestDesc => "take no damage for {0} seconds,.,.";
    public override string QuestDescRetired => "took no damage for {0} seconds !!";
    public override Sprite QuestIcon => questshrine.bundle.LoadAsset<Sprite>("nodamage");
    public override bool useListeners => true;
    public override string[] Tags => ["noStack"];
    public override Type Behavior => typeof(NoDamageBehaviorBase);

    public static ConfigEntry<int> minTimer;
    public static ConfigEntry<int> maxTimer;
    public static ConfigEntry<bool> ss1Behavior;
    
    public override void CreateConfig(ConfigFile config)
    {
        minTimer = Utils.SliderConfig(ConfigHelper("min timer value", 15, "minimum timer value for damage not being taken .,,."));
        maxTimer = Utils.SliderConfig(ConfigHelper("max timer value", 45, "max timer value for damage not being taken .,,."));
        ss1Behavior = Utils.CheckboxConfig(ConfigHelper("starstorm 1 behavior", true, "should use starstorm 1 behavior (reset timer on hit),., disabling will cause the quest to fail upon taking damage with no reward,.,."));
    }
}

public class NoDamageBehaviorBase : QuestBehaviorBase
{
    public override QuestBase QuestBase => NoDamage.instance;
    public override Type ObjectiveType => typeof(NoDamageObjective);
    
    [SyncVar]
    public float timer;
    [SyncVar]
    public float startingTime;

    public bool gaveReward;
    
    public override void StartQuest()
    {
        if (!NetworkServer.active)
        {
            base.StartQuest();
            return;
        }

        TakeDamageServer += OnTakeDamageServer;
        
        startingTime = Run.instance.runRNG.RangeFloat(NoDamage.minTimer.Value, NoDamage.maxTimer.Value) - 0.75f;
        timer = startingTime + 2;
        QuestDescInternal = string.Format(NoDamage.instance.QuestDesc, startingTime.ToString("0"));
        base.StartQuest();
    }

    public void FixedUpdate()
    {
        if (!NetworkServer.active) return;
        
        timer -= Time.fixedDeltaTime;
        if (timer < 0.5 && !gaveReward)
        {
            gaveReward = true;
            NoDamage.instance.GiveReward(body);
            RpcRetire();
        }
    }

    public void OnTakeDamageServer(DamageReport damageReport)
    {
        if (NoDamage.ss1Behavior.Value)
        {
            timer = startingTime;
        }
        else
        {
            RpcRetire();
        }
    }

    public override void OnDisable()
    {
        TakeDamageServer -= OnTakeDamageServer;
        base.OnDisable();
    }
}

public class NoDamageObjective : QuestObjectiveBase
{
    private NoDamageBehaviorBase noDamageQuest;
    
    public override string GenerateString()
    {
        if (!noDamageQuest)
        {
            noDamageQuest = (NoDamageBehaviorBase)sourceDescriptor.source;
            questBehavior = noDamageQuest;
        }

        string text = string.Format(NoDamage.instance.QuestDesc, noDamageQuest.timer.ToString("0"));
        if (!noDamageQuest.enabled)
        {
            retired = true;
            text = string.Format(NoDamage.instance.QuestDescRetired, noDamageQuest.startingTime.ToString("0"));
            Object.Destroy(noDamageQuest);
        }
        return GetPlayerName() + text;
    }

    public override bool IsDirty()
    {
        return true;
    }
}