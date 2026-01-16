using System;
using static BNR.butterscotchnroses;
using BNR.patches;
using BepInEx.Configuration;
using HarmonyLib;
using On.EntityStates.Drifter;
using On.EntityStates.JunkCube;
using RoR2;
using RoR2.Projectile;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Networking;
using CharacterMaster = On.RoR2.CharacterMaster;
using DeathState = On.EntityStates.JunkCube.DeathState;
using Object = UnityEngine.Object;
using ProjectileStickOnImpact = On.RoR2.Projectile.ProjectileStickOnImpact;
using RigidbodyStickOnImpact = On.RoR2.RigidbodyStickOnImpact;

namespace BNR;

public class drifter : PatchBase<drifter>
{
    public override void Init(Harmony harmony)
    {
        if (enabled.Value)
        {
            applyHooks();
        }
    }

    public void applyHooks()
    {
        if (enabled.Value)
        {
            EmptyBag.ModifyProjectile += EmptyBagOnModifyProjectile;
            ProjectileStickOnImpact.TrySticking += ProjectileStickOnImpactOnTrySticking;
            ProjectileStickOnImpact.UpdateSticking += ProjectileStickOnImpactOnUpdateSticking;
            DeathState.Explode += DeathStateOnExplode;
            CharacterMaster.GetDeployableSameSlotLimit += CharacterMasterOnGetDeployableSameSlotLimit;
            Idle.OnEnter += IdleOnOnEnter;
        }
        else
        {
            EmptyBag.ModifyProjectile -= EmptyBagOnModifyProjectile;
            ProjectileStickOnImpact.TrySticking -= ProjectileStickOnImpactOnTrySticking;
            ProjectileStickOnImpact.UpdateSticking -= ProjectileStickOnImpactOnUpdateSticking;
            DeathState.Explode -= DeathStateOnExplode;
            CharacterMaster.GetDeployableSameSlotLimit -= CharacterMasterOnGetDeployableSameSlotLimit;
            Idle.OnEnter -= IdleOnOnEnter;
        }
    }
    
    private void IdleOnOnEnter(Idle.orig_OnEnter orig, EntityStates.JunkCube.Idle self)
    {
        orig(self);
        self.fixedAge = Mathf.NegativeInfinity; // you will live forever
    }
    private int CharacterMasterOnGetDeployableSameSlotLimit(CharacterMaster.orig_GetDeployableSameSlotLimit orig, RoR2.CharacterMaster self, DeployableSlot slot)
    {
        return slot == DeployableSlot.DrifterJunkCube ? cubeCount.Value : orig(self, slot);
    }

    private void DeathStateOnExplode(DeathState.orig_Explode orig, EntityStates.JunkCube.DeathState self)
    {
        Log.Debug($"drifter - {self.transform.childCount}");
        for (int i = 0; i < self.transform.childCount; i++)
        {
            if (self.transform.GetChild(i).name != "ModelBase" &&
                self.transform.GetChild(i).name != "JunkCubeDamagedEffect(Clone)" &&
                self.transform.GetChild(i).name != "JunkCubePreDeath(Clone)" &&
                self.transform.GetChild(i).name != "JunkCubeLaunchEffect(Clone)")
                
            {
                Log.Debug($"drifter explode 2 - {self.transform.GetChild(i)}");
                var child = self.transform.GetChild(i);
                child.parent = null;
                var layerMask = LayerMask.GetMask("World");
                Ray someRay = new Ray(self.transform.position, Vector3.down);
                Physics.Raycast(someRay, out RaycastHit hit, Mathf.Infinity, layerMask);
                child.transform.position = hit.point;
                Log.Debug($"drifter hit - {hit.point}");
                Log.Debug($"drifter hit - {hit.point}");
                child.transform.rotation = new Quaternion(0f, 0f, 0f, 0f);

            }
        }
        orig(self);
    }

    private void ProjectileStickOnImpactOnUpdateSticking(ProjectileStickOnImpact.orig_UpdateSticking orig, RoR2.Projectile.ProjectileStickOnImpact self)
    {
        if (self.GetComponent<driftercubebwaaa>() && self.GetComponent<driftercubebwaaa>().referencetrans != null)
        {
            Log.Debug($"drifter - bwaaaa4");
            

            self.stickEvent.Invoke();
            self.alreadyRanStickEvent = true;

            self.GetComponent<ThrownObjectProjectileController>().EjectPassengerToFinalPosition();
            
            //self.GetComponent<ThrownObjectProjectileController>().Networkpassenger.transform.rotation = self.GetComponent<driftercubebwaaa>().referencerot;
            self.GetComponent<ThrownObjectProjectileController>().Networkpassenger.transform.SetParent(self.GetComponent<driftercubebwaaa>().referencetrans);
            Vector3 target;
            target.x = 0;
            target.y = self.GetComponent<ThrownObjectProjectileController>().Networkpassenger.transform.eulerAngles.y;
            target.z = 0;
            self.GetComponent<ThrownObjectProjectileController>().Networkpassenger.transform.rotation = Quaternion.Euler(target);
            self.GetComponent<driftercubebwaaa>().referencetrans = null;
            
            Object.Destroy(self.gameObject);
            
        }
        else
        {
            orig(self);
        }
    }

    private bool ProjectileStickOnImpactOnTrySticking(ProjectileStickOnImpact.orig_TrySticking orig, RoR2.Projectile.ProjectileStickOnImpact self, Collider hitCollider, Vector3 impactNormal)
    {
        string cubename = hitCollider.transform?.parent?.transform?.parent?.transform?.parent?.transform?.parent?.name;

        if (cubename != null)
        {
            Log.Debug($"drifter - cubenae {cubename}");
            Log.Debug($"drifter - {hitCollider.transform.parent.transform.parent.transform.parent.transform.parent.name}");

            if (hitCollider.transform.parent.transform.parent.transform.parent.transform.parent.name.Contains(
                    "JunkCube"))
            {
                /*
                */
                self.NetworkrunStickEvent = true;
                //self.NetworklocalPosition = transform.InverseTransformPoint(self.transform.position);
                self.victim = self.gameObject;
                self.NetworkhitHurtboxIndex = -1;
                Transform transform = hitCollider.transform;
                ParticleSystem[] array = self.stickParticleSystem;
                for (int i = 0; i < array.Length; i++)
                {
                    array[i].Play();
                }

                if (self.stickSoundString.Length > 0)
                {
                    Util.PlaySound(self.stickSoundString, self.gameObject);
                }

                if (self.alignNormals && impactNormal != Vector3.zero)
                {
                    self.transform.rotation =
                        Util.QuaternionSafeLookRotation(self.invertNormal ? (-impactNormal) : impactNormal,
                            self.transform.up);
                }

                ;
                self.NetworklocalRotation = Quaternion.Inverse(transform.rotation) * self.transform.rotation;

                self.transform.SetParent(hitCollider.gameObject.transform);

                GameObject gameObject = (NetworkServer.active ? self.victim : self.syncVictim);
                if ((bool)gameObject)
                {
                    self.stuckTransform = gameObject.transform;
                    if (self.hitHurtboxIndex >= 0)
                    {
                        self.stuckBody = self.stuckTransform.GetComponent<CharacterBody>();
                        if ((bool)self.stuckBody && (bool)self.stuckBody.hurtBoxGroup &&
                            self.hitHurtboxIndex < self.stuckBody.hurtBoxGroup.hurtBoxes.Length &&
                            self.stuckBody.hurtBoxGroup.hurtBoxes[self.hitHurtboxIndex] != null)
                        {
                            self.stuckTransform = self.stuckBody.hurtBoxGroup.hurtBoxes[self.hitHurtboxIndex].transform;
                        }

                        ModelLocator component = self.syncVictim.GetComponent<ModelLocator>();
                        if ((bool)component)
                        {
                            Transform modelTransform = component.modelTransform;
                            if ((bool)modelTransform)
                            {
                                HurtBoxGroup component2 = modelTransform.GetComponent<HurtBoxGroup>();
                                if ((bool)component2)
                                {
                                    HurtBox hurtBox = component2.hurtBoxes[self.hitHurtboxIndex];
                                    if ((bool)hurtBox)
                                    {
                                        self.stuckTransform = hurtBox.transform;
                                    }
                                }
                            }
                        }
                    }
                }

                driftercubebwaaa driftercubebwaaa = self.gameObject.AddComponent<driftercubebwaaa>();
                driftercubebwaaa.referencetrans = hitCollider.transform.parent.transform.parent.transform.parent.transform.parent;
                driftercubebwaaa.referencerot =  Util.QuaternionSafeLookRotation(self.invertNormal ? (-impactNormal) : impactNormal,
                    self.transform.up);
                if (!self.rigidbody.isKinematic)
                {
                    self.rigidbody.detectCollisions = false;
                    self.rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
                    self.rigidbody.isKinematic = true;

                }
                return true;
            }
        }
        
        return orig(self, hitCollider, impactNormal);
    }

    private void EmptyBagOnModifyProjectile(EmptyBag.orig_ModifyProjectile orig, EntityStates.Drifter.EmptyBag self, ref FireProjectileInfo fireProjectileInfo)
    {
        orig(self, ref fireProjectileInfo);
        Log.Debug($"drifter - {self.characterBody.inputBank.sprint.down}");
        Log.Debug($"drifter - {self.characterBody.inputBank.sprint.wasDown}");
        if (self.characterBody.inputBank.sprint.down)
        {
            Log.Debug($"bwaa2 {fireProjectileInfo.force} {fireProjectileInfo.speedOverride}");
            
            fireProjectileInfo.force = 0;
            fireProjectileInfo.speedOverride = 0;
        }
        
        
    }

    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("BNR - drifter",
            "enable patches for drifter",
            true,
            "");
        BNRUtils.CheckboxConfig(enabled);
        enabled.SettingChanged += (_, _) => { applyHooks(); };
        
        cubeCount = config.Bind("BNR - drifter",
            "junk cube count",
            20,
            "limit of junk cubes !!");
        BNRUtils.SliderConfig(4, 300, cubeCount);
    }

    private ConfigEntry<bool> enabled;
    private ConfigEntry<int> cubeCount;
}

public class driftercubebwaaa : MonoBehaviour
{
    public Transform referencetrans;
    public Quaternion referencerot;

    public void FixedUpdate()
    {
        if (referencetrans)
        {
            
        }
        else
        {
            
            Object.Destroy(this);
        }
    }
}