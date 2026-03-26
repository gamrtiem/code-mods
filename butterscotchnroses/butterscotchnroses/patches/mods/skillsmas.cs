using System;
using BNR.patches;
using BepInEx.Configuration;
using EntityStates;
using EntityStates.Merc;
using HarmonyLib;
using RoR2;
using R2API;
using Skillsmas;
using Skillsmas.Skills.Merc;
using UnityEngine;
using UnityEngine.Networking;
using EvisDash = On.EntityStates.Merc.EvisDash;
using Random = UnityEngine.Random;

namespace BNR;

public class skillsmas : PatchBase<skillsmas>
{
	/*[HarmonyPatch]
	public class SkillsmasChanges
	{
		[HarmonyPatch(typeof(Skillsmas.Skills.Merc.Zandatsu.ZandatsuDash), "OnExit")]
		[HarmonyPrefix]
		public static bool DashOnEnterPostFix(Skillsmas.Skills.Merc.Zandatsu.ZandatsuDash __instance)
		{
			Log.Debug($" bewww34 {__instance.characterMotor.rootMotion}");
			if (NetworkServer.active)
			{
				__instance.characterBody.AddBuff(RoR2Content.Buffs.HiddenInvincibility);
			}

			Util.PlaySound(EntityStates.Merc.EvisDash.endSoundString, __instance.gameObject);
			//__instance.characterMotor.velocity *= 0.1f;
			//__instance.SmallHop(__instance.characterMotor, EntityStates.Merc.EvisDash.smallHopVelocity);
			__instance.aimRequest?.Dispose();
			__instance.PlayAnimation("FullBody, Override", EntityStates.Merc.EvisDash.EvisLoopExitStateHash);
			if (NetworkServer.active)
			{
				__instance.characterBody.RemoveBuff(RoR2Content.Buffs.HiddenInvincibility);
			}
			Log.Debug($" bewww {__instance.characterMotor.velocity}");
			//EntityStates.BaseState.
			return false;
		}
		
		
		[HarmonyPatch(typeof(Skillsmas.Skills.Merc.Zandatsu.ZandatsuDash), "ModifyBodyNextState")]
		[HarmonyPrefix]
		public static bool modifyBodyState(Skillsmas.Skills.Merc.Zandatsu.ZandatsuDash __instance,EntityStateMachine entityStateMachine, ref EntityState newNextState)
		{
			EntityStateMachine entityStateMachine2 = __instance.outer;
			entityStateMachine2.nextStateModifier = (EntityStateMachine.ModifyNextStateDelegate)Delegate.Remove(entityStateMachine2.nextStateModifier, new EntityStateMachine.ModifyNextStateDelegate(__instance.ModifyBodyNextState));
			newNextState = new NewZandatsuHit();
			return false;
		}
	}

	public class NewZandatsuHit : BaseState
	{
		public static GameObject explosionEffectPrefab;

		public bool hitDone = false;

		public float attackStopwatch2 = 0f;

		public float stopwatch2 = 0f;
		public float stopwatch = 0f;
		public float attackStopwatch = 0f;
		public bool crit;
		
		private Transform modelTransform;

		public static GameObject blinkPrefab;

		public static float duration = 2f;

		public static float damageCoefficient;

		public static float damageFrequency;

		public static float procCoefficient;

		public static string beginSoundString;

		public static string endSoundString;

		public static float maxRadius;

		public static GameObject hitEffectPrefab;

		public static string slashSoundString;

		public static string impactSoundString;

		public static string dashSoundString;

		public static float slashPitch;

		public static float smallHopVelocity;

		public static float lingeringInvincibilityDuration;

		private Animator animator;

		private CharacterModel characterModel;
		

		private static float minimumDuration = 0.5f;

		private CameraTargetParams.AimRequest aimRequest;

		public override void OnEnter()
		{
			//this.LoadConfiguration(typeof(Evis));
			base.OnEnter();
			if (NetworkServer.active)
			{
				base.characterBody.RemoveBuff(RoR2Content.Buffs.HiddenInvincibility);
			}
			
			EffectData effectData = new();
			effectData.rotation = Util.QuaternionSafeLookRotation(Vector3.up);
			effectData.origin = base.gameObject.transform.position;
			EffectManager.SpawnEffect(Evis.blinkPrefab, effectData, transmit: false);
			
			Util.PlayAttackSpeedSound(Evis.beginSoundString, base.gameObject, 1.2f);
			crit = Util.CheckRoll(critStat, base.characterBody.master);
			modelTransform = GetModelTransform();
			if ((bool)modelTransform)
			{
				animator = modelTransform.GetComponent<Animator>();
				characterModel = modelTransform.GetComponent<CharacterModel>();
			}
			if ((bool)characterModel)
			{
				characterModel.invisibilityCount++;
			}
			if ((bool)base.cameraTargetParams)
			{
				aimRequest = base.cameraTargetParams.RequestAimType(CameraTargetParams.AimType.Aura);
			}
			if (NetworkServer.active)
			{
				base.characterBody.AddBuff(RoR2Content.Buffs.HiddenInvincibility);
			}
			Log.Debug($" beww2w {characterMotor.velocity}");
		}

		public override void FixedUpdate()
		{
			Log.Debug($" beww22w {characterMotor.velocity}");
			
			stopwatch = 0f - Time.fixedDeltaTime;
			attackStopwatch = 0f - Time.fixedDeltaTime;
			base.FixedUpdate();
			stopwatch2 += Time.fixedDeltaTime;
			attackStopwatch2 += Time.fixedDeltaTime;
			if (!NetworkServer.active)
			{
				return;
			}

			float num = 1f / Evis.damageFrequency / attackSpeedStat;

			HurtBox hurtBox = SearchForTarget();
			if ((bool)hurtBox)
			{
				Util.PlayAttackSpeedSound(Evis.slashSoundString, base.gameObject, Evis.slashPitch);
				Util.PlaySound(Evis.dashSoundString, base.gameObject);
				Util.PlaySound(Evis.impactSoundString, base.gameObject);
				HurtBoxGroup hurtBoxGroup = hurtBox.hurtBoxGroup;

				Vector2 normalized = UnityEngine.Random.insideUnitCircle.normalized;
				EffectManager.SimpleImpactEffect(Evis.hitEffectPrefab, hurtBox.transform.position, new Vector3(normalized.x, 0f, normalized.y), transmit: true);
				Transform transform = hurtBoxGroup.transform;
				TemporaryOverlayInstance temporaryOverlayInstance = TemporaryOverlayManager.AddOverlay(base.transform.gameObject);
				temporaryOverlayInstance.duration = num;
				temporaryOverlayInstance.animateShaderAlpha = true;
				temporaryOverlayInstance.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
				temporaryOverlayInstance.destroyComponentOnEnd = true;
				temporaryOverlayInstance.originalMaterial = LegacyResourcesAPI.Load<Material>("Materials/matMercEvisTarget");
				temporaryOverlayInstance.AddToCharacterModel(base.transform.GetComponent<CharacterModel>());
				if (NetworkServer.active)
				{
					DamageInfo damageInfo = new DamageInfo
					{
						damage = Zandatsu.damage / 100f * damageStat,
						damageType = new DamageTypeCombo(DamageTypeCombo.Generic, DamageTypeExtended.Generic,
							DamageSource.Special),
						attacker = base.gameObject,
						procCoefficient = Evis.procCoefficient,
						position = hurtBox.transform.position,
						crit = crit
					};
					DamageAPI.AddModdedDamageType(damageInfo, Zandatsu.zandatsuDamageType);
					hurtBox.healthComponent.TakeDamage(damageInfo);
					GlobalEventManager.instance.OnHitEnemy(damageInfo, hurtBox.healthComponent.gameObject);
					GlobalEventManager.instance.OnHitAll(damageInfo, hurtBox.healthComponent.gameObject);
				}

				hitDone = true;
			}
			else if (base.isAuthority && stopwatch2 > Evis.minimumDuration)
			{
				outer.SetNextStateToMain();
			}
			

			if (base.isAuthority && hitDone)
			{
				outer.SetNextStateToMain();
			}
		}

		public override void OnExit()
		{
			Util.PlaySound(endSoundString, base.gameObject);
			
			EffectData effectData = new();
			effectData.rotation = Util.QuaternionSafeLookRotation(Vector3.up);
			effectData.origin = base.gameObject.transform.position;
			EffectManager.SpawnEffect(Evis.blinkPrefab, effectData, transmit: false);
			
			modelTransform = GetModelTransform();
			if ((bool)modelTransform)
			{
				TemporaryOverlayInstance temporaryOverlayInstance = TemporaryOverlayManager.AddOverlay(modelTransform.gameObject);
				temporaryOverlayInstance.duration = 0.6f;
				temporaryOverlayInstance.animateShaderAlpha = true;
				temporaryOverlayInstance.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
				temporaryOverlayInstance.destroyComponentOnEnd = true;
				temporaryOverlayInstance.originalMaterial = LegacyResourcesAPI.Load<Material>("Materials/matMercEvisTarget");
				temporaryOverlayInstance.AddToCharacterModel(modelTransform.GetComponent<CharacterModel>());
				TemporaryOverlayInstance temporaryOverlayInstance2 = TemporaryOverlayManager.AddOverlay(modelTransform.gameObject);
				temporaryOverlayInstance2.duration = 0.7f;
				temporaryOverlayInstance2.animateShaderAlpha = true;
				temporaryOverlayInstance2.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
				temporaryOverlayInstance2.destroyComponentOnEnd = true;
				temporaryOverlayInstance2.originalMaterial = LegacyResourcesAPI.Load<Material>("Materials/matHuntressFlashExpanded");
				temporaryOverlayInstance2.AddToCharacterModel(modelTransform.GetComponent<CharacterModel>());
			}
			if ((bool)characterModel)
			{
				characterModel.invisibilityCount--;
			}
			aimRequest?.Dispose();
			if (NetworkServer.active)
			{
				base.characterBody.RemoveBuff(RoR2Content.Buffs.HiddenInvincibility);
				base.characterBody.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, lingeringInvincibilityDuration);
			}
			Util.PlaySound(endSoundString, base.gameObject);
			SmallHop(base.characterMotor, smallHopVelocity);
			base.OnExit();
		}
		
		private HurtBox SearchForTarget()
		{
			BullseyeSearch bullseyeSearch = new BullseyeSearch();
			bullseyeSearch.searchOrigin = base.transform.position;
			bullseyeSearch.searchDirection = Random.onUnitSphere;
			bullseyeSearch.maxDistanceFilter = maxRadius;
			bullseyeSearch.teamMaskFilter = TeamMask.GetUnprotectedTeams(GetTeam());
			bullseyeSearch.sortMode = BullseyeSearch.SortMode.Distance;
			bullseyeSearch.RefreshCandidates();
			bullseyeSearch.FilterOutGameObject(base.gameObject);
			HurtBox hurtBoxLowest = null;
			foreach (var hurtBox in bullseyeSearch.GetResults())
			{
				if (hurtBoxLowest == null || hurtBox.healthComponent && hurtBox.healthComponent.health < hurtBoxLowest.healthComponent.health)
				{
					hurtBoxLowest = hurtBox;
				}
			}
			return hurtBoxLowest;
		}
	}*/

	public override void Init(Harmony harmony)
    {     
        if(!enabled.Value) { return; }
        //harmony.CreateClassProcessor(typeof(SkillsmasChanges)).Patch();
        //SkillsmasContent.Resources.entityStateTypes.Add(typeof(NewZandatsuHit));
        GlobalEventManager.onCharacterDeathGlobal += GlobalEventManagerOnonCharacterDeathGlobal;
        //On.EntityStates.Merc.EvisDash.FixedUpdate += EvisDashOnFixedUpdate; 
    }

	private void EvisDashOnFixedUpdate(EvisDash.orig_FixedUpdate orig, EntityStates.Merc.EvisDash self)
	{
		Log.Debug($" bewww34 {self.dashVector * (self.moveSpeedStat * EntityStates.Merc.EvisDash.speedCoefficient * self.GetDeltaTime())}");
		orig(self);
	}

	private void GlobalEventManagerOnonCharacterDeathGlobal(DamageReport damageReport)
    {
        if (damageReport.damageInfo != null && damageReport.damageInfo.HasModdedDamageType(Skillsmas.Skills.Merc.Zandatsu.zandatsuDamageType))
        {
            if (damageReport.attackerBody)
            {
                damageReport.attackerBody.skillLocator.special.ApplyAmmoPack();
            }
        }
    }

    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("BNR - skillsmas",
            "enable patches for skillsmas",
            true,
            "");
        BNRUtils.CheckboxConfig(enabled);
        
        zandatsuRechargeSkill = config.Bind("BNR - skillsmas",
            "have zandatsu recharge skill on kill !!",
            true,
            "like !! if it kills something refresh cooldown !!");
        BNRUtils.CheckboxConfig(zandatsuRechargeSkill);
    }

    private static ConfigEntry<bool> zandatsuRechargeSkill;
    private ConfigEntry<bool> enabled;
}