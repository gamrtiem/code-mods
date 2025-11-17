using static BNR.butterscotchnroses;

namespace BNR;
using System;
using EntityStates.Executioner2;
using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using UnityEngine;
using FieldAttributes = Mono.Cecil.FieldAttributes;
public class starstorm
{
    [HarmonyPatch]
    public class Starstorm2ExeChanges
    {
        [HarmonyPatch(typeof(EntityStates.Executioner2.Dash), "OnEnter")]
        [HarmonyPostfix]
        public static void DashOnEnterPostFix(Dash __instance)
        {
            __instance.characterBody.gameObject.AddComponent<Boosted>();
        }

        [HarmonyPatch(typeof(Dash), "OnExit")]
        [HarmonyPostfix]
        public static void DashOnExitPostFix(Dash __instance)
        {
            bool foundaxe = false;

            foreach (var esm in __instance.characterBody.gameObject.GetComponents<EntityStateMachine>())
            {
                //Log.Debug(esm.state.ToString().ToLower());
                if (esm.state.ToString().ToLower().Contains("slam"))
                {
                    foundaxe = true;
                }
            }

            if (!foundaxe)
            {
                //Log.Debug("not keeping boosted!!");
                GameObject.Destroy(__instance.characterBody.gameObject.GetComponent<Boosted>());
            }
        }

        [HarmonyPatch(typeof(ExecuteSlam), "HandleMovement")]
        [HarmonyILManipulator]
        public static void OnEnterPostFix(ILContext il)
        {
            Log.Debug("loading il gook ");
            var c = new ILCursor(il);

            if (c.TryGotoNext(x => x.MatchLdsfld(typeof(ExecuteSlam), "verticlalSpeed")))
            {
                //c.Index += 1;
                c.RemoveRange(2);

                //Debug.Log(c);

                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<ExecuteSlam, float>>((es) =>
                {
                    if (es.characterBody.gameObject.GetComponent<Boosted>() != null)
                    {
                        //Log.Debug("found boosted!!");
                        if (useMovespeed.Value)
                            return es.characterBody.moveSpeed * boostedSpeed.Value;
                        return 10f * boostedSpeed.Value;
                    }

                    if (useMovespeed.Value)
                        return es.characterBody.moveSpeed * baseSpeed.Value;
                    return 10f * baseSpeed.Value;
                });

                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<ExecuteSlam, float>>((es) =>
                {
                    if (useMovespeed.Value)
                        return es.characterBody.moveSpeed * terminalSpeed.Value;
                    return 10f * terminalSpeed.Value;
                });
                //Debug.Log(computeValueDelegate);

                //c.Emit(OpCodes.Ldc_R4, 12);

                //c.Emit(OpCodes.Ret);

                //Log.Error(il.ToString());
            }
            else
            {
                Log.Debug("failedilbok");
            }
        }

        [HarmonyPatch(typeof(ExecuteSlam), "OnEnter")]
        [HarmonyPostfix]
        public static void OnEnterPostFix(ExecuteSlam __instance)
        {
            __instance.characterBody.gameObject.AddComponent<SpeedTester>();
            //__instance
        }

        [HarmonyPatch(typeof(ExecuteSlam), "OnExit")]
        [HarmonyPostfix]
        public static void OnExitPostFix(ExecuteSlam __instance)
        {
            if (__instance.characterBody.gameObject.GetComponent<Boosted>() != null)
                GameObject.Destroy(__instance.characterBody.gameObject.GetComponent<Boosted>());
            if (__instance.characterBody.gameObject.GetComponent<SpeedTester>() != null)
                GameObject.Destroy(__instance.characterBody.gameObject.GetComponent<SpeedTester>());
        }

        [HarmonyPatch(typeof(ExecuteSlam), "DoImpactAuthority")]
        [HarmonyILManipulator]
        private static void slam(ILContext il)
        {
            Log.Debug("loading il gook ");
            var c = new ILCursor(il);

            if (c.TryGotoNext(x => x.MatchLdsfld(typeof(ExecuteSlam), "exesillymod.baseDamageCoefficient")
                ))
            {
                c.Index += 2;

                //Debug.Log(c);

                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc_3);

                c.EmitDelegate<Func<ExecuteSlam, float, float>>((es, damage) =>
                {
                    //Log.Debug(damage);
                    //Log.Debug(es.characterMotor.velocity.magnitude);
                    float highest = 0;
                    if (es.characterBody.gameObject.GetComponent<SpeedTester>() != null)
                    {
                        string total = "";
                        highest = es.characterBody.gameObject.GetComponent<SpeedTester>().speedlist[0];
                        for (int i = 0;
                             i < es.characterBody.gameObject.GetComponent<SpeedTester>().speedlist.Length;
                             i++)
                        {
                            total += es.characterBody.gameObject.GetComponent<SpeedTester>().speedlist[i] + " ";

                            if (es.characterBody.gameObject.GetComponent<SpeedTester>().speedlist[i] > highest)
                            {
                                highest = es.characterBody.gameObject.GetComponent<SpeedTester>().speedlist[i];
                            }
                        }

                        //Log.Debug(total);
                    }

                    if (es.characterBody.gameObject.GetComponent<Boosted>() != null)
                    {
                        if (useMovespeed.Value)
                            return boostedDamage.Value +
                                   (highest / (es.characterBody.moveSpeed * boostedSpeed.Value) - 1) *
                                   speedmult.Value;
                        return boostedDamage.Value + (highest / (10f * boostedSpeed.Value) - 1) * speedmult.Value;
                    }

                    if (useMovespeed.Value)
                        return baseDamage.Value + (highest / (es.characterBody.moveSpeed * baseSpeed.Value) - 1) *
                            speedmult.Value;
                    return baseDamage.Value + (highest / (10f * baseSpeed.Value) - 1) * speedmult.Value;
                });
                //Debug.Log(computeValueDelegate);

                //c.Emit(OpCodes.Ldc_R4, 12);
                c.Emit(OpCodes.Stloc_3);
                //c.Emit(OpCodes.Ret);

                //Log.Error(il.ToString());
            }
            else
            {
                Log.Debug("failedilbok");
            }
        }

        public static void Patch(AssemblyDefinition assembly)
        {
            TypeDefinition genericSkill = assembly.MainModule.GetType("EntityStates.Executioner2", "ExecuteSlam");
            if (genericSkill != null)
            {
                genericSkill.Fields.Add(new FieldDefinition("flyingspeed", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(Vector3))));
            }
            else
            {
                Log.Debug("failed");
            }
        }
    }
}

public class SpeedTester : MonoBehaviour
{
    public Vector3 lastPosition = Vector3.zero;
    public float speed;
    public float[] speedlist = new  float[6];

    private void Start()
    {
        lastPosition = transform.position;
    }

    private void FixedUpdate()
    {
        speed = Vector3.Distance(lastPosition, transform.position) / Time.deltaTime;
        //Log.Debug(speed);
        
        lastPosition = transform.position;

        for (int i = 1; i < speedlist.Length; i++)
        {
            speedlist[i] = speedlist[i - 1];
        }

        speedlist[0] = speed;
    }
}
public class Boosted : MonoBehaviour
{
   
}