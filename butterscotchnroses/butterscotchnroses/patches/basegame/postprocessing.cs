using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters;
using System.Text.RegularExpressions;
using BNR.patches;
using BepInEx.Configuration;
using R2API.Utils;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering.PostProcessing;

namespace BNR;

public class postprocessing : PatchBase<postprocessing>
{
    private static List<PostProcessProfile> postProcessProfiles = [];
    public override void Init()
    {
        applyHooks();
    }

    private void applyHooks()
    {
        if (enabled.Value)
        {
            // search https://xiaoxiao921.github.io/GithubActionCacheTest/assetPathsDump.html "UnityEngine.Rendering.PostProcessing.PostProcessProfile"

            List<Assembly> assemblies =
            [
                typeof(SobelRain).Assembly,
                typeof(AmbientOcclusion).Assembly
            ];
            
            foreach (Assembly assembly in assemblies)
            {
                Type[] test = assembly.ManifestModule.FindTypes(Module.FilterTypeName, "*");
                Log.Debug($"test count: {test.Length}");
                foreach (Type typetest in test)
                {
                    if (typetest.ToString().Contains("+") || typetest.ToString().Contains("[") || typetest.ToString().Contains("<")) continue;
                    
                    if (!stagePP.typeStringMatcher.ContainsKey(typetest.ToString().Split(".")[^1]))
                    {
                        stagePP.typeStringMatcher.Add(typetest.ToString().Split(".")[^1], typetest);
                    }
                }
            }
            

            /*foreach (KeyValuePair<string, Type> pair in stagePP.typeStringMatcher)
            {
                Log.Debug($"type string matcher contains: {pair.Key}, {pair.Value}");
            }*/

            string[] PPVEdits = PPVEdit.Value.Split(";");
            foreach (string edit in PPVEdits)
            {
                Log.Debug($"PPV edit string: {edit}");
                Log.Debug($"GUID/asset psth : {edit.Split(",")[0]}");
                Log.Debug($"forward facing name : {edit.Split(",")[1]}");
                Log.Debug($"regex []: {Regex.Match(edit, @"\[(.*?)\]").Groups[1].Value}");
                new stagePP(edit.Split(",")[0], edit.Split(",")[1], Regex.Match(edit, @"\[(.*?)\]").Groups[1].Value);
            }
        }
        else
        {

        }
    }

    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("BNR - postprocessing",
            "enable patches for postprocessing",
            true,
            "");
        Utils.CheckboxConfig(enabled);
        enabled.SettingChanged += (_, _) => { applyHooks(); };
        
        PPVEdit = config.Bind("BNR - postprocessing",
            "stage edits",
            @"RoR2/Base/title/PostProcessing/ppSceneGolemplainsFoggy.asset,titanic plains,[AmbientOcclusion];RoR2/DLC3/nest/ppSceneNest.asset,pretenders,[AmbientOcclusion]",
            "");
        Utils.StringConfig(PPVEdit);
    }

    private ConfigEntry<bool> enabled;
    private ConfigEntry<string> PPVEdit;

    private class stagePP
    {
        private PostProcessProfile profile;
        private string forwardFacingName;
        private string[] stageEditsList = [];
        public stagePP(string GUID, string forwardFacingName, string edits)
        {
            Addressables.LoadAssetAsync<PostProcessProfile>(GUID).Completed += handle =>
            {
                postProcessProfiles.Add(handle.Result); //(Add/Remove/Edit),(AmbientOcclusion),[(varname),(newvar)]
                profile = handle.Result;
                this.forwardFacingName = forwardFacingName;
                stageEditsList = edits.Split(",");
                ApplyEdits();
                //create config for stage
                //string config, what custom things should be added and by how much eg. "AmbientOcclusion,0.4"
                //maybe a custom class as a datatype ? 
            };
        }
        

        public static Dictionary<string, Type> typeStringMatcher = new Dictionary<string, Type>()
        {
            //{ "AmbientOcclusion", typeof(AmbientOcclusion) },
        };
        
        private void ApplyEdits()
        {
            foreach (string PPVEdit in stageEditsList)
            {
                Log.Debug($"add edit for {forwardFacingName} {PPVEdit}");

                if (!typeStringMatcher.TryGetValue(PPVEdit, out Type editedType))
                {
                    Log.Warning($"could not find type {PPVEdit} in supported types !!! supported types below ,. ,");
                    foreach (string key in typeStringMatcher.Keys)
                    {
                        Log.Warning(key);
                    }

                    return;
                }
                
                if (!profile.HasSettings(editedType))
                {
                    profile.AddSettings(editedType);
                    Log.Debug($"added type {editedType} to {forwardFacingName}");
                }

                //object profileSetting = profile.GetSetting<AmbientOcclusion>();
                MethodInfo method = typeof(PostProcessProfile).GetMethods().First(m => m.Name == "GetSetting" && m.IsGenericMethod);
                object profileSetting = method.MakeGenericMethod(editedType).Invoke(profile, []);
                
                // i love you stack overflow https://stackoverflow.com/questions/6536163/how-to-list-all-variables-of-class .,. 
                BindingFlags bindingFlags = BindingFlags.Instance |
                                            BindingFlags.NonPublic |
                                            BindingFlags.Public;
                List<FieldInfo> fieldValues = profileSetting.GetType()
                    .GetFields(bindingFlags)
                    .ToList();

                foreach (FieldInfo variable in fieldValues)
                {
                    try
                    {
                        FieldInfo valueVariable = variable.GetValue(profileSetting)
                            .GetType()
                            .GetFields(bindingFlags).First(fieldInfo => fieldInfo.Name == "value");
                    
                        MethodInfo methodGetFieldValue = typeof(stagePP).GetMethods().First(m => m.Name == "ApplyEdit" && m.IsGenericMethod);
                        methodGetFieldValue.MakeGenericMethod(valueVariable.FieldType).Invoke(this, [variable, PPVEdit, profileSetting]);
                    }
                    catch (Exception e)
                    {
                        switch (e)
                        {
                            case InvalidOperationException invalidOperationException:
                                Log.Warning($"variable {variable.Name} of typwe {variable.GetValue(profileSetting)} had no value variable !!");
                                break;
                            case TargetInvocationException targetInvocationException:
                                Log.Warning($"was unable to create config for variable {variable.Name} of type {variable.GetValue(profileSetting)} !!!");
                                break;
                            default:
                                Log.Error(e);
                                break;
                        }
                    }
                }
            }
        }

        public void ApplyEdit<T>(FieldInfo variable, string editType, object profileSetting)
        {
            string oldValue = variable.GetValue(profileSetting).GetFieldValue<T>("value").ToString();
                                    
            if (!butterscotchnroses.instance.Config.TryGetEntry(new ConfigDefinition($"BNR - postprocessing - {forwardFacingName}", $"{editType} - {variable.Name}"), out ConfigEntry<T> entryColor))
            {
                entryColor = butterscotchnroses.instance.Config.Bind($"BNR - postprocessing - {forwardFacingName}",
                    $"{editType} - {variable.Name}",
                    variable.GetValue(profileSetting).GetFieldValue<T>("value"),
                    "");
            }

            if (!Equals(variable.GetValue(profileSetting).GetFieldValue<T>("value"), entryColor.Value))
            {
                variable.GetValue(profileSetting).SetFieldValue("value", entryColor.Value);
                variable.GetValue(profileSetting).SetFieldValue("overrideState", true);
                Log.Warning($"{variable.Name} of type {variable.GetValue(profileSetting)} .,., \nold value {oldValue} ,..,\nnew value {variable.GetValue(profileSetting).GetFieldValue<T>("value")} !!!");
            }
            else
            {
                Log.Debug($"{variable.Name} of type {variable.GetValue(profileSetting)} .,., value {variable.GetValue(profileSetting).GetFieldValue<T>("value")} !!!");
            }
        }
    }
}