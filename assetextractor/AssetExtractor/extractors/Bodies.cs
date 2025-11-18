using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DebugToolkit;
using RoR2;
using RoR2.ContentManagement;
using RoR2.ExpansionManagement;
using UnityEngine;
using Path = System.IO.Path;

namespace AssetExtractor;

public partial class WikiFormat
{
    public static void FormatBodies(ReadOnlyContentPack readOnlyContentPack)
    {
        var path = Path.Combine(WikiOutputPath, WIKI_OUTPUT_BODIES);

        if (!Directory.Exists(WikiOutputPath)) Directory.CreateDirectory(WikiOutputPath);
        TextWriter tw = new StreamWriter(path, WikiAppend);

        var arg = "";
        var cards = new HashSet<DirectorCard>(StringFinder.Instance.GetDirectorCardsFromPartial(arg));

        foreach (var bodyprefab in readOnlyContentPack.bodyPrefabs)
            try
            {
                var f = "monsters[\"{0}\"] = {{\n";
                f += "\tInternalName = \"{1}\",\n"; // todo look into how to get internalname !!
                f += "\tImage = \"{2}\",\n";
                f += "\tBaseHealth = {3},\n";
                f += "\tScalingHealth = {4},\n";
                f += "\tBaseDamage = {5},\n";
                f += "\tScalingDamage = {6},\n";
                f += "\tBaseHealthRegen = {7},\n";
                f += "\tScalingHealthRegen = {8},\n";
                f += "\tBaseSpeed = {9},\n";
                f += "\tBaseArmor = {10},\n";
                f += "\tMass = {11},\n";
                f += "\tLocalizationInternalName = \"{12}\",\n";
                f += "\tColor = \"{13}\",\n";
                
                var bodyName = "";
                var token = "";
                var color = "";
                float mass = 99999999999999;
                
                var breakout = SurvivorCatalog.allSurvivorDefs.Any(survdef => survdef.bodyPrefab == bodyprefab);
                if (breakout) continue;
                if (!bodyprefab.TryGetComponent(out CharacterBody charbody)) continue;

                if (charbody.baseNameToken != null)
                {
                    bodyName = charbody.GetDisplayName();
                    f += $"\tName = \"{Language.GetString(charbody.baseNameToken)}\",\n";
                    f += $"\tLink = \"{Language.GetString(charbody.baseNameToken)}\",\n";
                }
                
                var basehealth = charbody.baseMaxHealth;
                var scalinghealth = charbody.levelMaxHealth;
                var damage = charbody.baseDamage;
                var scalingdamage = charbody.levelDamage;
                var regen = charbody.baseRegen;
                var scalingregen = charbody.levelRegen;
                var speed = charbody.baseMoveSpeed;
                var armor = charbody.baseArmor;

                if (!Language.english.TokenIsRegistered(charbody.baseNameToken.Replace("_NAME", "_LORE")))
                    f += "\tNoLogbook = true,\n";
                else
                    loredefs.Add("Monsters " + charbody.baseNameToken + " " + charbody.baseNameToken.Replace("_NAME", "_LORE"));

                var deathRewards = charbody.gameObject.GetComponent<DeathRewards>();
                if (deathRewards != null && deathRewards.bossDropTable)
                    f += "\tType = \"Boss\",\n";
                else
                    f += "\tType = \"Normal\",\n";

                f += $"\tExpansion = \"{readOnlyContentPack.identifier}\",\n";

                foreach (var card in StringFinder.Instance.DirectorCards)
                {
                    if (!cards.Contains(card)) continue;
                    if (!card.spawnCard.prefab.GetComponent<CharacterMaster>().bodyPrefab.name.Equals(bodyprefab.name)) continue;

                    if(card.cost != 0)
                        f += $"\tCreditsCost = {card.cost},\n";
                    if (card.minimumStageCompletions != 0)
                    {
                        f += $"\tStartingStage = {card.minimumStageCompletions},\n";
                    }
                    break;
                }

                //yeah ,.,..,
                string flags = "\tCategory = {{ ";
                if ((charbody.bodyFlags & CharacterBody.BodyFlags.Devotion) != 0) flags += "\"Devotion\", ";
                if ((charbody.bodyFlags & CharacterBody.BodyFlags.HasBackstabImmunity) != 0)
                    flags += "\"HasBackstabImmunity\", ";
                if ((charbody.bodyFlags & CharacterBody.BodyFlags.HasBackstabPassive) != 0)
                    flags += "\"HasBackstabPassive\", ";
                if ((charbody.bodyFlags & CharacterBody.BodyFlags.IgnoreFallDamage) != 0)
                    flags += "\"IgnoreFallDamage\", ";
                if ((charbody.bodyFlags & CharacterBody.BodyFlags.IgnoreItemUpdates) != 0)
                    flags += "\"IgnoreItemUpdates\", ";
                if ((charbody.bodyFlags & CharacterBody.BodyFlags.IgnoreKnockback) != 0)
                    flags += "\"IgnoreKnockback\", ";
                if ((charbody.bodyFlags & CharacterBody.BodyFlags.ImmuneToExecutes) != 0)
                    flags += "\"ImmuneToExecutes\", ";
                if ((charbody.bodyFlags & CharacterBody.BodyFlags.ImmuneToGoo) != 0)
                    flags += "\"ImmuneToGoo\", ";
                if ((charbody.bodyFlags & CharacterBody.BodyFlags.ImmuneToLava) != 0)
                    flags += "\"ImmuneToLava\", ";
                if ((charbody.bodyFlags & CharacterBody.BodyFlags.ImmuneToVoidDeath) != 0)
                    flags += "\"ImmuneToVoidDeath\", ";
                if ((charbody.bodyFlags & CharacterBody.BodyFlags.Masterless) != 0) flags += "\"Masterless\", ";
                if ((charbody.bodyFlags & CharacterBody.BodyFlags.Mechanical) != 0) flags += "\"Mechanical\", ";
                if ((charbody.bodyFlags & CharacterBody.BodyFlags.OverheatImmune) != 0)
                    flags += "\"OverheatImmune\", ";
                if ((charbody.bodyFlags & CharacterBody.BodyFlags.ResistantToAOE) != 0)
                    flags += "\"ResistantToAOE\", ";
                if ((charbody.bodyFlags & CharacterBody.BodyFlags.SprintAnyDirection) != 0)
                    flags += "\"SprintAnyDirection\", ";
                if ((charbody.bodyFlags & CharacterBody.BodyFlags.Void) != 0) flags += "\"Void\", ";
                if (flags != "\tCategory = {{ ") f += flags.Substring(0, flags.Length - 2) + " }},\n";

                if (charbody.subtitleNameToken != null)
                {
                    string desc = Language.GetString(charbody.subtitleNameToken);
                    if (!desc.EndsWith("SUBTITLE") && desc != "") f += $"\tBossName = \"{desc}\",\n";
                }

                if (charbody.bodyColor != null) color = ColorUtility.ToHtmlStringRGB(charbody.bodyColor);

                if (charbody.TryGetComponent(out ExpansionRequirementComponent expansion))
                    if (expansion != null)
                        acronymHelper(Language.GetString(expansion.requiredExpansion.nameToken), false);


                if (charbody.baseNameToken != null && charbody.baseNameToken.EndsWith("_NAME"))
                    token = charbody.baseNameToken.Remove(charbody.baseNameToken.Length - 5); // remove _NAME
                else if (charbody.baseNameToken != null) token = charbody.baseNameToken;


                

                if (charbody.TryGetComponent(out CharacterMotor motor))
                {
                    if ((int)motor.mass < 99999999)
                        mass = motor.mass;
                    else
                        mass = 0;
                }

                f += "\t}}";
                var format = Language.GetStringFormatted(f, bodyName, bodyprefab.name,
                    bodyName.Replace(" ", "_") + WikiModname + ".png", basehealth, scalinghealth, damage, scalingdamage,
                    regen, scalingregen, speed, armor, mass, token, "#" + color);

                foreach (var kvp in FormatR2ToWiki) format = format.Replace(kvp.Key, kvp.Value);

                tw.WriteLine(format);

                if (!charbody.portraitIcon) continue;

                string temp = WikiOutputPath + @"\bodies\";
                Directory.CreateDirectory(temp);
                try
                {
                    if (bodyName == "")
                    {
                        Log.Debug("body name is blank ! using toke n");
                        exportTexture(charbody.portraitIcon,
                            Path.Combine(temp, token + WikiModname + ".png"));
                    }
                    else
                    {
                        exportTexture(charbody.portraitIcon,
                            Path.Combine(temp, bodyName.Replace(" ", "_") + WikiModname + ".png"));
                    }
                }
                catch
                {
                    Log.Debug(
                        "erm ,,.,. failed to export body icon with proper name ,,. trying with tokenm !! " + bodyName);
                    exportTexture(charbody.portraitIcon,
                        Path.Combine(temp, token + WikiModname + ".png"));
                }
            }
            catch (Exception e)
            {
                Log.Error("Error while exporting body: " + e);
            }

        tw.Close();

        long length = new FileInfo(path).Length;
        if (length <= 0) File.Delete(path);
    }
}