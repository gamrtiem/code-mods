using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RoR2;
using RoR2.ContentManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using Path = System.IO.Path;
using SceneExitController = On.RoR2.SceneExitController;

namespace AssetExtractor;

public static partial class WikiFormat
{
    public static void FormatStages(ReadOnlyContentPack readOnlyContentPack)
    {
        var path = Path.Combine(WikiOutputPath, WIKI_OUTPUT_STAGES);

        if (!Directory.Exists(WikiOutputPath)) Directory.CreateDirectory(WikiOutputPath);
        TextWriter tw = new StreamWriter(path, WikiAppend);

        foreach (var sceneDef in readOnlyContentPack.sceneDefs)
            try
            {
                var f = "Environments[\"{0}\"] = {{\n"; // cutoff "Hidden Realm: "
                f += "\tName = \"{1}\",\n";
                f += "\tInternalName = \"{2}\",\n";
                f += "\tImage = \"{3}\",\n";
                f += "\tSubName = \"{4}\",\n";
                f += "\tStage = \"{5}\",\n"; //sceneDef.stageOrder
                f += "\tSoundtrack = \"{6}\",\n";
                f += "\tInteractableCredits = \"{7}\",\n";
                f += "\tMonsterCredits = \"{8}\",\n";
                f += "\tDescription = \"{9}\",\n";
                
                Log.Debug(sceneDef.nameToken);
                Log.Debug(sceneDef.sceneAddress.AssetGUID);
                if (sceneDef.nameToken == "") continue;
                
                var loadedscene = Addressables.LoadSceneAsync(sceneDef.sceneAddress.AssetGUID).WaitForCompletion();
                var sceneInfo = GameObject.Find("SceneInfo");
                if (sceneInfo != null)
                {
                    Log.Debug("found scene info !! hooray !!!");
                    var classicStageInfo = sceneInfo.GetComponent<ClassicStageInfo>();

                    #region interactables
                    f += "\tInteractables = {{\n";
                    List<List<string>> awesome = [];
                    foreach (var category in classicStageInfo.GetInteractableDccsPool.poolCategories)
                    {
                        foreach (var poolEntry in category.alwaysIncluded)
                        {
                            foreach (var interactableCategory in poolEntry.dccs.categories)
                            {
                                // I LOVE NESTING !!!!!!!!!!!!!!!!!!!!!!!!!!
                                foreach (var interactableCard in interactableCategory.cards)
                                {
                                    string nameToken = processNameToken(interactableCard.spawnCard.prefab);
                                    int selectionWeight = interactableCard.selectionWeight;
                                    
                                    string awesomestring = nameToken + "," + selectionWeight;
                                    List<string> awesomeadd =
                                    [
                                        awesomestring
                                    ];
                                    awesome.Add(awesomeadd);
                                }
                            }
                        }
                        
                        foreach (var poolEntry in category.includedIfConditionsMet)
                        {
                            foreach (var interactableCategory in poolEntry.dccs.categories)
                            {
                                foreach (var interactableCard in interactableCategory.cards)
                                {
                                    string nameToken = processNameToken(interactableCard.spawnCard.prefab);
                                    int selectionWeight = interactableCard.selectionWeight;

                                    string awesomeaddstring = nameToken + "," + selectionWeight + "," + acronymHelper(Language.GetString(poolEntry.requiredExpansions[0].nameToken), true);
                                    
                                    bool contains = false;
                                    foreach (var awesomestring in awesome)
                                    {
                                        foreach (var awesomerstring in awesomestring.ToList())
                                        {
                                            if (awesomerstring.Split(",")[0].Equals(nameToken))
                                            {
                                                contains = true;
                                                awesomestring.Add(awesomeaddstring);
                                            }
                                        }
                                    }
                                    if (!contains)
                                    {
                                        List<string> awesomeadd =
                                        [
                                            awesomeaddstring
                                        ];
                                        awesome.Add(awesomeadd);
                                    }
                                }
                            }
                        }

                        foreach (var stringList in awesome)
                        {
                            //stringList[0].Split(",")[0] = name 
                            //stringList[0].Split(",")[1] = weight
                            //stringList[0].Split(",")[2] = dlc (if applicable
                            f += "\t\t[\"" + stringList[0].Split(",")[0] + "\"] = {{ ";//[ALL] = {{ Weight = " + selectionWeight + " }} }},\n";
                            for (int i = 0; i < stringList.Count; i++)
                            {
                                if(i == 0)
                                {
                                    //starting string is an always case
                                    Log.Debug(stringList[i]);
                                    if (stringList[0].Split(",").Length == 2)
                                    {
                                        f += "[ALL] = {{ Weight = " + stringList[i].Split(",")[1] + " }}";
                                    }
                                    else
                                    {
                                        f += "[" + stringList[i].Split(",")[2] + "] = {{ Weight = " + stringList[i].Split(",")[1] + " }}";
                                    }
                                }
                                else
                                {
                                    f += ", [" + stringList[i].Split(",")[2] + "] = {{ Weight = " + stringList[i].Split(",")[1] + " }}";
                                }
                            }

                            f += " }},\n";
                        }
                    }
                    f += "\t}},\n";
                    #endregion
                    
                    #region monsters
                    f += "\tMonsters = {{\n";
                    List<List<string>> awesome2 = [];
                    foreach (var category in classicStageInfo.GetMonsterDccsPool.poolCategories)
                    {
                        if (category.name == "Standard")
                        {
                            foreach (var poolEntry in category.alwaysIncluded)
                            {
                                foreach (var monsterCategory in poolEntry.dccs.categories)
                                {
                                    // I LOVE NESTING !!!!!!!!!!!!!!!!!!!!!!!!!!
                                    foreach (var monsterCard in monsterCategory.cards)
                                    {
                                        string nameToken = Language.GetString(monsterCard.spawnCard.prefab.GetComponent<CharacterMaster>().bodyPrefab.GetComponent<CharacterBody>().baseNameToken);
                                        int selectionWeight = monsterCard.selectionWeight;
                                        int minimumStage = monsterCard.minimumStageCompletions;
                                        string categoryName = monsterCategory.name;
                                        string awesomestring = nameToken + "," + selectionWeight + "," + minimumStage + "," + categoryName;
                                        List<string> awesomeadd =
                                        [
                                            awesomestring
                                        ];
                                        awesome2.Add(awesomeadd);
                                    }
                                }
                            }
                            
                            foreach (var poolEntry in category.includedIfConditionsMet)
                            {
                                foreach (var monsterCategory in poolEntry.dccs.categories)
                                {
                                    foreach (var monsterCard in monsterCategory.cards)
                                    {
                                        string nameToken = Language.GetString(monsterCard.spawnCard.prefab.GetComponent<CharacterMaster>().bodyPrefab.GetComponent<CharacterBody>().baseNameToken);
                                        int selectionWeight = monsterCard.selectionWeight;
                                        int minimumStage = monsterCard.minimumStageCompletions;
                                        string categoryName = monsterCategory.name;
                                        string awesomeaddstring = nameToken + "," + selectionWeight + "," + minimumStage + "," + categoryName + "," + acronymHelper(Language.GetString(poolEntry.requiredExpansions[0].nameToken), true);
                                        
                                        bool contains = false;
                                        foreach (var awesomestring in awesome2)
                                        {
                                            foreach (var awesomerstring in awesomestring.ToList())
                                            {
                                                if (awesomerstring.Split(",")[0].Equals(nameToken))
                                                {
                                                    contains = true;
                                                    awesomestring.Add(awesomeaddstring);
                                                }
                                            }
                                        }
                                        if (!contains)
                                        {
                                            List<string> awesomeadd =
                                            [
                                                awesomeaddstring
                                            ];
                                            awesome2.Add(awesomeadd);
                                        }
                                    }
                                }
                            }

                            foreach (var stringList in awesome2)
                            {
                                //stringList[0].Split(",")[0] = name 
                                //stringList[0].Split(",")[1] = weight
                                //stringList[0].Split(",")[2] = dlc (if applicable
                                f += "\t\t[\"" + stringList[0].Split(",")[0] + "\"] = {{ ";//[ALL] = {{ Weight = " + selectionWeight + " }} }},\n";
                                for (int i = 0; i < stringList.Count; i++)
                                {
                                    if(i == 0)
                                    {
                                        //starting string is an always case
                                        Log.Debug(stringList[i]);
                                        if (stringList[0].Split(",").Length == 4)
                                        {
                                            if (Int32.Parse(stringList[0].Split(",")[2]) > 0)
                                            {
                                                f += "[ALL] = {{ Weight = " + stringList[i].Split(",")[1] + ", Stage = " + stringList[i].Split(",")[2] + ", Category = \"" + stringList[i].Split(",")[3] + "\" }}";
                                            }
                                            else
                                            {
                                                f += "[ALL] = {{ Weight = " + stringList[i].Split(",")[1] + ", Category = \"" + stringList[i].Split(",")[3] + "\" }}";
                                            }
                                        }
                                        else if (Int32.Parse(stringList[0].Split(",")[2]) > 0)
                                        {
                                            f += "[" + stringList[i].Split(",")[4] + "] = {{ Weight = " + stringList[i].Split(",")[1] + ", Stage = " + stringList[i].Split(",")[2] + ", Category = \"" + stringList[i].Split(",")[3] + "\" }}";
                                        }
                                        else
                                        {
                                            f += "[" + stringList[i].Split(",")[4] + "] = {{ Weight = " + stringList[i].Split(",")[1] + ", Category = \"" + stringList[i].Split(",")[3] + "\" }}";
                                        }
                                    }
                                    else
                                    {
                                        if (Int32.Parse(stringList[0].Split(",")[2]) > 0)
                                        {
                                            f += "[" + stringList[i].Split(",")[4] + "] = {{ Weight = " + stringList[i].Split(",")[1] + ", Stage = " + stringList[i].Split(",")[2] + ", Category = \"" + stringList[i].Split(",")[3] + "\" }}";
                                        }
                                        else
                                        {
                                            f += "[" + stringList[i].Split(",")[4] + "] = {{ Weight = " + stringList[i].Split(",")[1] + ", Category = \"" + stringList[i].Split(",")[3] + "\" }}";
                                        }
                                    }
                                }

                                f += " }},\n";
                            }
                        }
                    }
                    f += "\t}},\n";
                    #endregion
                    
                    #region family
                    string family = "\tFamily = {{ ";
                    foreach (var category in classicStageInfo.GetMonsterDccsPool.poolCategories)
                    {
                        if (category.name != "Standard")
                        { // family or void 
                            {
                                foreach (var poolEntry in category.includedIfConditionsMet)
                                {
                                    family += "\"" + poolEntry.dccs.name.Replace("dccs", "").Replace("FamilySandy", "").Replace("FamilyNature", "").Replace("FamilySnowy", "").Replace("Family", "") + "\", ";
                                }
                            }
                        }
                    }
                    family = family[..^2];
                    family += " }}\n";
                    if (family != "\tFamily = {{ }}\n")
                    {
                        f += family;
                    }
                    #endregion
                    
                    string bazaar = "BAZAAR_SEER_" + loadedscene.Scene.name.ToUpper();
                    if (Language.english.TokenIsRegistered(bazaar))
                    {
                        f += "\tLunarSeer = \"" + Language.GetString(bazaar).Replace("<style=cWorldEvent>", "").Replace("</style>", "") + "\",\n";
                    }

                    string name = Language.GetString(sceneDef.nameToken);
                    if (name.StartsWith("Hidden Realm"))
                    {
                        f += "\tHidden Realm = true,\n";
                        name = name.Replace("Hidden Realm: ", "");
                    }
                    
                    loredefs.Add("Environments " + sceneDef.nameToken + " " + sceneDef.loreToken);

                    f += "}}";
                    string format = Language.GetStringFormatted(f, 
                        name, 
                        name, 
                        loadedscene.Scene.name,
                        name + " Logbook Thumbnail.png", 
                        Language.GetString(sceneDef.subtitleToken),
                        sceneDef.stageOrder,
                        sceneDef.mainTrack.cachedName,
                        classicStageInfo.sceneDirectorInteractibleCredits, 
                        classicStageInfo.sceneDirectorMonsterCredits, 
                        "DESCRIPTION TEMP REPLACE ME PRETTY PLEASE !!");
                
                    foreach (var kvp in FormatR2ToWiki)
                        format = format.Replace(kvp.Key, kvp.Value);
                
                    tw.WriteLine(format);

                    Texture preview = Addressables.LoadAssetAsync<Texture>(sceneDef.previewTextureReference).WaitForCompletion();
                    string temp = WikiOutputPath + @"\stages\";
                    Directory.CreateDirectory(temp);
                    try
                    {
                        exportTexture(preview, Path.Combine(temp, name + WikiModname + ".png"));
                    }
                    catch
                    {
                        Log.Debug("erm ,,.,. failed to export stage icon ,,. " + name);
                    }
                }
                else
                {
                    Log.Warning("uable to f9ind scene info ,., ");
                }
                Addressables.UnloadSceneAsync(loadedscene).WaitForCompletion();
                Log.Debug("unlaoded scene !! ");
            }
            catch (Exception e)
            {
                Log.Error("Error while exporting scene: " + e);
            }

        tw.Close();
    }

    public static string processNameToken(GameObject prefab)
    {
        string nameToken = prefab.name;
        if (prefab != null && prefab.GetComponent<GenericDisplayNameProvider>() != null)
        {
            //check for if drone
            if (prefab.GetComponent<SummonMasterBehavior>())
            {
                nameToken = Language.GetString(prefab
                    .GetComponent<SummonMasterBehavior>().masterPrefab
                    .GetComponent<CharacterMaster>().bodyPrefab
                    .GetComponent<CharacterBody>().baseNameToken);
            }
            else
            {
                nameToken = Language.GetString(prefab.GetComponent<GenericDisplayNameProvider>().displayToken);
            }
        }
        // for some reason all category chests are healing in the display name provider ?? weird .,. 
        if (nameToken.Contains("Chest - Healing"))
        {
            Log.Debug($"found healing chest ~!!! prefab name is {prefab.name}");
            if (prefab.name.Contains("Utility"))
            {
                nameToken = "Utility Chest";
            } 
            else if (prefab.name.Contains("Damage"))
            {
                nameToken = "Damage Chest";
            }
            else
            {
                nameToken = "Healing Chest";
            }
        }
        foreach (var kvp in FormatInteractableNames) nameToken = nameToken.Replace(kvp.Key, kvp.Value);
        
        return nameToken;
    } 
    
    private static readonly Dictionary<string, string> FormatInteractableNames = new()
    {
        { "DuplicatorWild", "3D Printer (Overgrown)"},
        { "DuplicatorMilitary", "Mili-Tech Printer" },
        { "DuplicatorLarge", "3D Printer (Green)" },
        { "Duplicator", "3D Printer (White)" },
        { "Open Large Chest - Utility", "Large Utility Chest" },
        { "Large Chest - Healing", "Large Healing Chest" },
        { "Large Chest - Damage", "Large Damage Chest" },
        { "TripleShopLarge", "Multishop Terminal (Green)" },
        { "TripleShopEquipment", "Multishop Terminal (Equipment)" },
        { "TripleShop", "Multishop Terminal (White)"},
        { "GoldChest", "Legendary Chest" }, 
        { "ShrineHalcyonite", "Halcyonite Shrine" }, 
        { "ShrineColossusAccess", "Shrine of Shaping" }, 
    };
}