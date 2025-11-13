using System;
using System.Collections.Generic;
using System.IO;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Projectile;
using UnityEngine;
using Object = UnityEngine.Object;
using Path = System.IO.Path;

namespace AssetExtractor;

public static partial class WikiFormat
{
    public static void FormatSkill(ReadOnlyContentPack readOnlyContentPack)
    {
        var path = Path.Combine(WikiOutputPath, WIKI_OUTPUT_SKILLS);

        var f = "skills[\"{0}\"] = {{\n";
        f += "\tName = \"{1}\",\n";
        f += "\tDesc = \"{2}\",\n";
        f += "\tSurvivor = \"{3}\",\n";
        f += "\tType = \"{4}\",\n";
        f += "\tCooldown = \"{5}\",\n";
        f += "\tUnlock = \"{6}\",\n";


        if (!Directory.Exists(WikiOutputPath)) Directory.CreateDirectory(WikiOutputPath);
        TextWriter tw = new StreamWriter(path, WikiAppend);

        foreach (var surv in readOnlyContentPack.survivorDefs)
            try
            {
                if (surv.bodyPrefab.TryGetComponent(out CharacterBody body))
                {
                    var scripts = body.GetComponents<GenericSkill>();
                    var skilllocator = body.GetComponent<SkillLocator>();
                    foreach (var skill in scripts)
                    {
                        var type = "Passive";
                        var name = "";
                        var desc = "";
                        var unlock = "";
                        float cooldown = 0;
                        List<float> proc = new();
                        //Log.Debug(skill.skillFamily.ToString());
                        var survivor = Language.GetString(surv.displayNameToken);

                        foreach (var variant in skill.skillFamily.variants)
                        {
                            if (variant.skillDef.skillNameToken != null)
                                name = Language.GetString(variant.skillDef.skillNameToken);
                            //Log.Debug(variant.skillDef.skillNameToken);
                            if (skill == skilllocator.primary)
                                type = "Primary";
                            else if (skill == skilllocator.secondary)
                                type = "Secondary";
                            else if (skill == skilllocator.utility)
                                type = "Utility";
                            else if (skill == skilllocator.special) type = "Special";

                            if (variant.skillDef.baseRechargeInterval != 0)
                                cooldown = variant.skillDef.baseRechargeInterval;
                            if (variant.skillDef.skillDescriptionToken != null)
                                desc = Language.GetString(variant.skillDef.skillDescriptionToken);
                            if (variant.unlockableDef != null)
                            {
                                var unlockable =
                                    AchievementManager.GetAchievementDefFromUnlockable(variant.unlockableDef
                                        .cachedName);
                                if (unlockable != null) unlock = Language.GetString(unlockable.nameToken);
                                //Log.Debug(unlockable.nameToken);
                            }

                            if (variant.skillDef.activationState.stateType != null &&
                                WikiTryGetProcs) // must be passive then if not
                                try
                                {
                                    var entitystate = EntityStateCatalog.InstantiateState(
                                        EntityStateCatalog.stateTypeToIndex[
                                            variant.skillDef.activationState.stateType]);
                                    if (entitystate != null)
                                        foreach (var readOnlyContentPack2 in ContentManager.allLoadedContentPacks)
                                        {
                                            Log.Warning("lookin through ,., " + readOnlyContentPack2.identifier);

                                            foreach (var config in readOnlyContentPack2.entityStateConfigurations)
                                            {
                                                if (!config.name.Contains(variant.skillDef.activationState.stateType
                                                        .ToString())) continue;
                                                Log.Warning("found config ! " + config.name);
                                                foreach (var field in config.serializedFieldsCollection
                                                             .serializedFields)
                                                    if (field.fieldName.ToLower()
                                                        .Contains("proc")) // wowie ! proc was just sitting there !
                                                    {
                                                        proc.Add(float.Parse(field.fieldValue.stringValue));
                                                        //Log.Debug("proc coefficient is " + proc);
                                                        //break;
                                                    }
                                                    else if (field.fieldName.ToLower()
                                                             .Contains(
                                                                 "projectile")) // check inside projectile for its proc
                                                    {
                                                        if (field.fieldValue.objectValue != null)
                                                        {
                                                            var projectile =
                                                                Object.Instantiate(field.fieldValue.objectValue) as
                                                                    GameObject;

                                                            if (projectile.TryGetComponent<ProjectileController>(
                                                                    out var controller))
                                                                proc.Add(controller.procCoefficient);
                                                            //Log.Debug("proc coefficient is " + proc);
                                                            //break;
                                                            else
                                                                Log.Error(
                                                                    "could not get projectile controller out of prefab ! ");
                                                        }
                                                    }
                                                    else if (field.fieldName.ToLower().Contains("damage") &&
                                                             proc.Count == 0) // default proc, but still has
                                                    {
                                                        proc.Add(1);
                                                        Log.Debug("no proc found ! defaulting to 1 ,., ");
                                                    }
                                            }
                                        }
                                }
                                catch (Exception e)
                                {
                                    Log.Error("Error while getting proc coefficient: " + e);
                                }

                            if (!variant.skillDef.icon.texture) continue;

                            var temp = WikiOutputPath + @"\skills\";
                            Directory.CreateDirectory(temp);
                            try
                            {
                                if (name == "")
                                {
                                    Log.Debug("skill name is blank ! using toke n");
                                    exportTexture(variant.skillDef.icon,
                                        Path.Combine(temp, variant.skillDef.skillNameToken + WikiModname + ".png"));
                                }
                                else
                                {
                                    exportTexture(variant.skillDef.icon,
                                        Path.Combine(temp, name.Replace(" ", "_") + WikiModname + ".png"));
                                }
                            }
                            catch
                            {
                                Log.Debug(
                                    "erm ,,.,. failed to export skill icon with proper name ,,. trying with tokenm !! " +
                                    name);
                                exportTexture(variant.skillDef.icon,
                                    Path.Combine(temp, variant.skillDef.skillNameToken + WikiModname + ".png"));
                            }

                            string format;
                            var tempformat = f;
                            if (proc.Count == 0) // no proc found, dont add 
                            {
                                tempformat += "\t}},";
                                format = Language.GetStringFormatted(tempformat, name, name, desc, survivor, type,
                                    cooldown, unlock);
                            }
                            else
                            {
                                tempformat += "\tProc = \"{7}\",\n";
                                tempformat += "\t}},";
                                format = Language.GetStringFormatted(tempformat, name, name, desc, survivor, type,
                                    cooldown, unlock, proc);
                            }

                            foreach (var kvp in FormatR2ToWiki) format = format.Replace(kvp.Key, kvp.Value);
                            tw.WriteLine(format);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error while exporting skill: " + e);
            }

        tw.Close();

        var length = new FileInfo(path).Length;
        if (length <= 0) File.Delete(path);
    }
}