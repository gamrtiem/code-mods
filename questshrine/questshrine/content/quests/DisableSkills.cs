//NETWORKING TODO
//uses Run.instance.runRNG, send r2api networking to clients  
using System;
using BepInEx.Configuration;
using On.EntityStates.GoldGat;
using questshrine.bases;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace questshrine.content.quests;

public class DisableSkills : QuestBase<DisableSkills>
{ 
    public override string QuestName => "Disable Skills";
    public override string QuestTitle => "<style=cWorldEvent>the planet sent out a freakings emp .,,..</style>";
    public override string QuestDesc => "skills disabled for {0} seconds,.,.";
    public override string QuestDescRetired => "skills no longer disabled!!!";
    public override Sprite QuestIcon => questshrine.bundle.LoadAsset<Sprite>("noskills");
    public override string[] Tags => ["noStack"];
    public override Type Behavior => typeof(DisableSkillsBehaviorBase);

    public static ConfigEntry<int> minTimer;
    public static ConfigEntry<int> maxTimer;
    
    public override void CreateConfig(ConfigFile config)
    {
        minTimer = Utils.SliderConfig(ConfigHelper("min timer value", 10, "minimum timer value for skills being disabled .,,."));
        maxTimer = Utils.SliderConfig(ConfigHelper("max timer value", 25, "max timer value for skills being disabled .,,."));
    }
}

public class DisableSkillsBehaviorBase : QuestBehaviorBase
{
    public override QuestBase QuestBase => DisableSkills.instance;
    public override Type ObjectiveType => typeof(NoSkillsObjective);

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
            DisableSkillsForBody(body);
            return;
        }
        
        startingTime = Run.instance.runRNG.RangeFloat(DisableSkills.minTimer.Value, DisableSkills.maxTimer.Value) - 0.75f;
        timer = startingTime + 2;

        DisableSkillsForBody(body);
        charMaster.onBodyStart += DisableSkillsForBody; // fuck your dios <3 ,.,.
        
        QuestDescInternal = string.Format(DisableSkills.instance.QuestDesc, startingTime.ToString("0"));
        base.StartQuest();
    }
    
    public void DisableSkillsForBody(CharacterBody charBody)
    {
        if (!body.skillLocator) return;
        
        if (body.skillLocator.primary)
            body.skillLocator.primary.SetSkillOverride(this, CharacterBody.CommonAssets.disabledSkill, GenericSkill.SkillOverridePriority.Contextual);

        if (body.skillLocator.secondary)
            body.skillLocator.secondary.SetSkillOverride(this, CharacterBody.CommonAssets.disabledSkill, GenericSkill.SkillOverridePriority.Contextual);

        if (body.skillLocator.utility)
            body.skillLocator.utility.SetSkillOverride(this, CharacterBody.CommonAssets.disabledSkill, GenericSkill.SkillOverridePriority.Contextual);

        if (body.skillLocator.special)
            body.skillLocator.special.SetSkillOverride(this, CharacterBody.CommonAssets.disabledSkill, GenericSkill.SkillOverridePriority.Contextual);
    }

    public override void OnDisable()
    {
        charMaster.onBodyStart -= DisableSkillsForBody;

        if (body.skillLocator)
        {
            if (body.skillLocator.primary)
                body.skillLocator.primary.UnsetSkillOverride(this, CharacterBody.CommonAssets.disabledSkill, GenericSkill.SkillOverridePriority.Contextual);

            if (body.skillLocator.secondary)
                body.skillLocator.secondary.UnsetSkillOverride(this, CharacterBody.CommonAssets.disabledSkill, GenericSkill.SkillOverridePriority.Contextual);

            if (body.skillLocator.utility)
                body.skillLocator.utility.UnsetSkillOverride(this, CharacterBody.CommonAssets.disabledSkill, GenericSkill.SkillOverridePriority.Contextual);

            if (body.skillLocator.special)
                body.skillLocator.special.UnsetSkillOverride(this, CharacterBody.CommonAssets.disabledSkill, GenericSkill.SkillOverridePriority.Contextual);
        }
        
        base.OnDisable();
    }

    public void FixedUpdate()
    {
        if (!NetworkServer.active) return;
        
        timer -= Time.fixedDeltaTime;
        if (timer < 0.5 && !gaveReward)
        {
            gaveReward = true;
            DisableSkills.instance.GiveReward(body);
            RpcRetire();
        }
    }
}

public class NoSkillsObjective : QuestObjectiveBase
{ 
    private DisableSkillsBehaviorBase _disableSkillsQuest;
    
    public override string GenerateString()
    {
        if (!_disableSkillsQuest)
        {
            _disableSkillsQuest = (DisableSkillsBehaviorBase)sourceDescriptor.source;
            questBehavior = _disableSkillsQuest;
        }

        string text = string.Format(DisableSkills.instance.QuestDesc, _disableSkillsQuest.timer.ToString("0"));
        if (!_disableSkillsQuest.enabled)
        {
            retired = true;
            text = DisableSkills.instance.QuestDescRetired;
            Object.Destroy(_disableSkillsQuest);
        }
        return GetPlayerName() + text;
    }

    public override bool IsDirty()
    {
        return true;
    }
}