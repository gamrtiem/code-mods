//NETWORKING TODO
//Run.instance.runRNG isnt available on clients; send a r2api networking packet to them .,
using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using questshrine.bases;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace questshrine.content.quests;

public class KillEnemies : QuestBase<KillEnemies>
{ 
    public override string QuestName => "Kill Enemies";
    public override string QuestTitle => "<style=cWorldEvent>the planet's malice grows .,,..</style>";
    public override string QuestDesc => "kill {0} {1}{2},.,.";
    public override string QuestDescRetired => "killed {0} {1}{2}!!!";
    public override Sprite QuestIcon => questshrine.bundle.LoadAsset<Sprite>("killenemies");
    public override bool useListeners => true;
    public override Type Behavior => typeof(KillEnemiesBehaviorBase);

    public override void CreateConfig(ConfigFile config)
    {
    }
}

public class KillEnemiesBehaviorBase : QuestBehaviorBase
{
    public override QuestBase QuestBase => KillEnemies.instance;
    public override Type ObjectiveType => typeof(KillEnemiesObjective);

    [SyncVar]
    public int bodyIndex;
    [SyncVar]
    public int killAmount;
    [SyncVar]
    public int startingKillAmount;
    
    public override void StartQuest()
    {
        if (!NetworkServer.active)
        {
            base.StartQuest();
            return;
        }
        
        KilledOtherServer += OnKilledOtherServer;

        ClassicStageInfo classicStageInfo = GameObject.Find("SceneInfo").GetComponent<ClassicStageInfo>();
        List<WeightedSelection<DirectorCard>.ChoiceInfo> availableChoices = [];
        List<CharacterMaster> availableMasters = [];
        foreach (WeightedSelection<DirectorCard>.ChoiceInfo choice in classicStageInfo.monsterSelection.choices)
        {
            if (choice.value?.minimumStageCompletions > Run.instance.stageClearCount) continue;
            if (choice.value?.spawnCard?.prefab?.TryGetComponent(out CharacterMaster master) != true) continue;
            
            if (master.bodyPrefab.GetComponent<CharacterBody>().isChampion)
            {
                //Log.Debug($"{master.name} is champion");
                continue;
            }

            if (choice.value.cost > 100)
            {
                //Log.Debug($"card cost {choice.value.cost} greater than 100 skipping {choice.value.spawnCard.name}");
                continue;
            }
            
            availableChoices.Add(choice);
            availableMasters.Add(master);
        }

        WeightedSelection<CharacterMaster> weightedSelection = new WeightedSelection<CharacterMaster>();
        for (int i = 0; i < availableChoices.Count; i++)
        {
            weightedSelection.AddChoice(availableMasters[i], availableChoices[i].value.cost);
        }
        CharacterMaster chosenMaster = weightedSelection.Evaluate(Run.instance.runRNG.nextNormalizedFloat);

        bodyIndex = (int)BodyCatalog.FindBodyIndex(chosenMaster.bodyPrefab);
        killAmount = (int)((50f/availableChoices[availableMasters.IndexOf(chosenMaster)].value.cost) * Run.instance.runRNG.RangeFloat(1, 2));
        if (killAmount <= 3)
            killAmount += 2;
        startingKillAmount = killAmount;
        
        QuestDescInternal = string.Format(KillEnemies.instance.QuestDesc, killAmount, Language.GetString(BodyCatalog.GetBodyPrefab((BodyIndex)bodyIndex).GetComponent<CharacterBody>().baseNameToken), (killAmount > 1 ? "s" : ""));
        base.StartQuest();
    }

    public void OnKilledOtherServer(DamageReport damageReport)
    {
        if ((int)damageReport.victimBodyIndex != bodyIndex) return;
        
        killAmount--;
        if (killAmount != 0) return;
        
        KillEnemies.instance.GiveReward(body);
        RpcRetire();
    }

    public override void OnDisable()
    {
        KilledOtherServer += OnKilledOtherServer;
        base.OnDisable();
    }
}

public class KillEnemiesObjective : QuestObjectiveBase 
{
    private KillEnemiesBehaviorBase _killEnemiesBehaviorBase;
    private int localKillAmount;
    private string name;
    
    public override string GenerateString()
    {
        if (!_killEnemiesBehaviorBase)
        {
            _killEnemiesBehaviorBase = (KillEnemiesBehaviorBase)sourceDescriptor.source;
            questBehavior = _killEnemiesBehaviorBase;
            name = Language.GetString(BodyCatalog.GetBodyPrefab((BodyIndex)_killEnemiesBehaviorBase.bodyIndex).GetComponent<CharacterBody>().baseNameToken);
        }

        localKillAmount = _killEnemiesBehaviorBase.killAmount;
        string text = string.Format(KillEnemies.instance.QuestDesc, _killEnemiesBehaviorBase.killAmount, name, (localKillAmount > 1 ? "s" : ""));
        if (!_killEnemiesBehaviorBase.enabled)
        {
            retired = true;
            text = string.Format(KillEnemies.instance.QuestDescRetired, _killEnemiesBehaviorBase.startingKillAmount, name, "s");
            Object.Destroy(_killEnemiesBehaviorBase);
        }
        return GetPlayerName() + text;
    }

    public override bool IsDirty()
    {
        return true;
    }
}