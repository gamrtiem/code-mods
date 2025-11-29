using BepInEx.Configuration;
using BNR.patches;
using On.RoR2.Navigation;
using R2API;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using SS2;
using UnityEngine.Networking;
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
public class starstorm : PatchBase<starstorm>
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
    
    public override void Init(Harmony harmony)
    {
        if (!applySS2.Value) return;
        harmony.CreateClassProcessor(typeof(Starstorm2ExeChanges)).Patch();
        LanguageAPI.Add("SS2_EXECUTIONER2_EXECUTION_DESC", $"Leap into the air, then slam an ion axe for <style=cIsDamage>{baseDamage.Value * 100f}-{boostedDamage.Value * 100f}% damage</style>. Hitting an isolated target deals <style=cIsDamage>double damage</style> and restores 3 <color=#29e5f2>Ion Charges</color>.");
        LanguageAPI.Add("SS2_ITEM_ICETOOL_DESC", $"While <style=cIsUtility>touching a wall</style>, gain <style=cIsUtility>+1</style> <style=cStack>(+1 per stack)</style> extra jump and a <style=\"cIsUtility\">{iceToolFreezeChance.Value}%</style> <style=\"cStack\">(+{iceToolFreezeChanceStack.Value}% per stack)</style> chance to <style=\"cIsUtility\">freeze enemies</style> for <style=\"cIsUtility\">{iceToolFreezeTime.Value} seconds</style> <style=\"cStack\">(+{iceToolFreezeTimeStack.Value} per stack)</style>. ");
        
        Hooks();
    }

    public override void Hooks()
    {
        On.RoR2.HealthComponent.TakeDamageProcess += (orig, self, info) =>
        {
            orig(self, info);
            CharacterBody attackerbody = info.attacker.GetComponent<CharacterBody>();
            
            if (!attackerbody || !attackerbody.inventory) return;
            
            int stacks = attackerbody.inventory.GetItemCountEffective(SS2Content.Items.IceTool._itemIndex);
            if (stacks <= 0) return;
            
            if (!Util.CheckRoll(iceToolFreezeChance.Value + iceToolFreezeChanceStack.Value * (stacks - 1), attackerbody.master)) return;
            if (!NetworkServer.active) return;
            
            SetStateOnHurt frozenState = self.body.GetComponent<SetStateOnHurt>();
            if (frozenState)
            {
                frozenState.SetFrozen(iceToolFreezeTime.Value + iceToolFreezeTimeStack.Value * (stacks - 1));
            }
        };
    }

    public override void Config(ConfigFile config)
    {
        applySS2 = config.Bind("Mods - SS2",
            "apply ss2 patches !!",
            true,
            "");
        BNRUtils.CheckboxConfig(applySS2);
        
        #region iceTool

        iceToolFreezeChance = config.Bind("Mods - SS2",
            "ice tool freeze chance",
            5f,
            "percent chance for icetool to freeze enemies !!");
        BNRUtils.SliderConfig(0, 100, iceToolFreezeChance);
        
        iceToolFreezeTime = config.Bind("Mods - SS2",
            "ice tool freeze time",
            0.5f,
            "how long icetool should freeze enemies !!");
        BNRUtils.SliderConfig(0, 30, iceToolFreezeTime);
        
        iceToolFreezeChanceStack = config.Bind("Mods - SS2",
            "ice tool freeze chance stack",
            2.5f,
            "percent chance for icetool to freeze enemies stack !!");
        BNRUtils.SliderConfig(0, 100, iceToolFreezeChanceStack);
        
        iceToolFreezeTimeStack = config.Bind("Mods - SS2",
            "ice tool freeze time stack",
            0.25f,
            "how long icetool should freeze enemies !!");
        BNRUtils.SliderConfig(0, 30, iceToolFreezeTimeStack);

        iceToolFreezeChance.SettingChanged += IceToolFreezeChanceOnSettingChanged;
        iceToolFreezeTime.SettingChanged += IceToolFreezeChanceOnSettingChanged;
        iceToolFreezeChanceStack.SettingChanged += IceToolFreezeChanceOnSettingChanged;
        iceToolFreezeTimeStack.SettingChanged += IceToolFreezeChanceOnSettingChanged;
        
        void IceToolFreezeChanceOnSettingChanged(object sender, EventArgs e)
        {
            if (applySS2.Value)
            {
                LanguageAPI.Add("SS2_ITEM_ICETOOL_DESC", $"While <style=cIsUtility>touching a wall</style>, gain <style=cIsUtility>+1</style> <style=cStack>(+1 per stack)</style> extra jump and a <style=\"cIsUtility\">{iceToolFreezeChance.Value}%</style> <style=\"cStack\">(+{iceToolFreezeChanceStack.Value}% per stack)</style> chance to <style=\"cIsUtility\">freeze enemies</style> for <style=\"cIsUtility\">{iceToolFreezeTime.Value} seconds</style> <style=\"cStack\">(+{iceToolFreezeTimeStack.Value} per stack)</style>. ");
            }
        }
        #endregion
        
        #region execution
        
        speedmult = config.Bind("Mods - SS2",
                "execution speed damage multiplier",
                10f,
                "like uhh how much extrad amage should be added of how fast you go past starting velocity compared to terminal ,.,. idk just move it around be yourself !!!! you can just set to 0 if you dont like !!!!");
        BNRUtils.SliderConfig(0, 60, speedmult);

        baseSpeed = config.Bind("Mods - SS2",
            "execution base speed",
            10f,
            "base starting speed for special !!!! multiplied by movespeed unless config for that is off ,.,. (then its multiplied by 10,. .,.,");
        BNRUtils.SliderConfig(0, 40, baseSpeed);

        boostedSpeed = config.Bind("Mods - SS2",
            "execution boosted speed",
            20f,
            "boosted speed when you use special from dash !!!! also multiplied by movespeed unless config for that is off ,.,. (then its multiplied by 10,. .,.,");
        BNRUtils.SliderConfig(0, 40, boostedSpeed);

        terminalSpeed = config.Bind("Mods - SS2",
            "execution terminal speed",
            30f,
            "how fast max speed should be !! be careful equation uses lerp so you go reallys fast if you put a high number ,..,");
        BNRUtils.SliderConfig(0, 60, terminalSpeed);

        baseDamage = config.Bind("Mods - SS2",
            "execution base damage coeff",
            13f,
            "base damage coeff when not boosted !!!!! speedmult is added on top too ,.,. ");
        BNRUtils.SliderConfig(1, 25, baseDamage);

        boostedDamage = config.Bind("Mods - SS2",
            "boosted damage coeff",
            15.5f,
            "boosted damage coeff  !!!!! speedmult is added on top too ,.,. ");
        BNRUtils.SliderConfig(1, 25, boostedDamage);


        useMovespeed = config.Bind("Mods - SS2",
            "execution use movespeed",
            true,
            "should movespeed affect how fast down you go !!! regular ss2 its just a set number ,.,.");
        BNRUtils.CheckboxConfig(useMovespeed);
        
        baseDamage.SettingChanged += BaseDamageOnSettingChanged;
        boostedDamage.SettingChanged += BaseDamageOnSettingChanged;

        void BaseDamageOnSettingChanged(object sender, EventArgs e)
        {
            if (applySS2.Value)
            {
                LanguageAPI.Add("SS2_EXECUTIONER2_EXECUTION_DESC",
                    $"Leap into the air, then slam an ion axe for <style=cIsDamage>{baseDamage.Value * 100f}-{boostedDamage.Value * 100f}% damage</style>. Hitting an isolated target deals <style=cIsDamage>double damage</style> and restores 3 <color=#29e5f2>Ion Charges</color>.");
            }
        }

        #endregion
    }

    
    public static ConfigEntry<bool> applySS2;

    public static ConfigEntry<float> baseSpeed;
    public static ConfigEntry<float> boostedSpeed;
    public static ConfigEntry<float> terminalSpeed;
    public static ConfigEntry<float> speedmult;
    public static ConfigEntry<float> baseDamage;
    public static ConfigEntry<float> boostedDamage;
    public static ConfigEntry<bool> useMovespeed;
    
    public static ConfigEntry<float> iceToolFreezeChance;
    public static ConfigEntry<float> iceToolFreezeTime;
    public static ConfigEntry<float> iceToolFreezeTimeStack;
    public static ConfigEntry<float> iceToolFreezeChanceStack;
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