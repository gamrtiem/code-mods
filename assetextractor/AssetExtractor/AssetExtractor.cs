using System;
using System.Diagnostics;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using R2API;
using RoR2.ContentManagement;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using Path = System.IO.Path;
using UnityHotReloadNS;

namespace AssetExtractor
{
    [BepInDependency(ItemAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    
    public class AssetExtractor : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "icebro";
        public const string PluginName = "assetextractor";
        public const string PluginVersion = "0.4.1";
        public ConfigEntry<bool> useModnameInFile;
        public ConfigEntry<bool> useModnameInDirectory;
        public ConfigEntry<bool> trygettingprocs;
        public ConfigEntry<bool> logskillprocs;
        
        private Stopwatch timer = new();
        internal static AssetExtractor Instance { get; private set; }

        public void Awake()
        {
            Instance = this;
            Log.Init(Logger);
            useModnameInFile = Config.Bind("export options", "use mod name", false, "always modname in file name (useful for conflicts when using single folder; eg ssu chirr and ss2 chirr");
            useModnameInDirectory = Config.Bind("export options", "use mod dir", true, "export everything in a content pack to its own folder (off for monolithic wiki folder with every skill/item/etc in one file/folder");
            trygettingprocs = Config.Bind("export options", "try getting skill procs automatically", false, "attempt to automatically get skill procs from entity state configurations (slow, bad and doesnt work half the time../ just get them from log brah !!");
            logskillprocs = Config.Bind("export options", "log skill procs", true, "log any on hit procs from skills (like 100 leeching seeds but awesome");
           
            if (!logskillprocs.Value) return;
            On.RoR2.BlastAttack.Fire += (orig, self) =>
            {
                Log.Debug("Firing blast proc " + self.procCoefficient);
                return orig(self);
            };
            On.RoR2.Projectile.ProjectileManager.FireProjectileClient += (orig, self, info, client, time) =>
            {
                Log.Debug("Firing projectile" + info.projectilePrefab);
                if (info.projectilePrefab.TryGetComponent<ProjectileController>(out var controller))
                {
                    Log.Debug("Firing projectile proc " + controller.procCoefficient);
                }
                else
                {
                    Log.Error("could not get projectile controller out of prefab ! ");
                }

                orig(self, info, client, time);
            };
            On.EntityStates.GenericBulletBaseState.GenerateBulletAttack += (orig, self, ray) =>
            {
                Log.Debug("Firing bullet proc " + self.procCoefficient);

                return orig(self, ray);
            };
            On.RoR2.BulletAttack.Fire += (orig, self) =>
            {
                Log.Debug("Firing bullet proc " + self.procCoefficient);
                orig(self);
            };
            On.RoR2.OverlapAttack.Fire += (orig, self, hurtboxes) =>
            {
                Log.Debug("Firing overlap proc " + self.procCoefficient);
                return orig(self, hurtboxes);
            };
            On.RoR2.GlobalEventManager.OnHitAll += (orig, self, damageInfo, hit) =>
            {
                Log.Debug("hit by proc " + damageInfo.procCoefficient);
                orig(self, damageInfo, hit);
            };
        }
        

        private void Update()
        {
            if (Input.GetKeyUp(KeyCode.F2))
            {
                // Process.Start(new ProcessStartInfo() { 
                //     FileName = Path.Combine(Path.Combine(Path.GetDirectoryName(Instance.Info.Location) ?? throw new InvalidOperationException(), "wiki")),
                //     UseShellExecute = true,
                //     Verb = "open"
                // });
                
                UnityHotReload.LoadNewAssemblyVersion(
                    typeof(AssetExtractor).Assembly, // The currently loaded assembly to replace.
                    "Z:\\run\\media\\icebrah\\buh\\gale\\riskofrain2\\profiles\\debug\\BepInEx\\plugins\\icebro-asset_extractor/AssetExtractor.dll"  // The path to the newly compiled DLL.
                );
            }

            if (Input.GetKeyUp(KeyCode.F1))
            {
                Log.Debug("wow !2323!");
            }
            
            if (!Input.GetKeyDown(KeyCode.F5)) return;
            Log.Info("F5 pressed ,,. extracting !!!!");
            
            long itemTime = 0;
            long equipTime = 0;
            long survTime = 0;
            long skillTime = 0;
            long challengeTime = 0;
            long bodyTime = 0;
            long buffTime = 0;
            long stageTime = 0;
            
            WikiFormat.WikiTryGetProcs = trygettingprocs.Value; 
            foreach (var readOnlyContentPack in ContentManager.allLoadedContentPacks)
            {
                if (useModnameInDirectory.Value)
                {
                    WikiFormat.WikiOutputPath = Path.Combine(Path.Combine(Path.GetDirectoryName(Instance.Info.Location) ?? throw new InvalidOperationException(), "wiki"), readOnlyContentPack.identifier);
                }
                else
                {
                    WikiFormat.WikiOutputPath = Path.Combine(Path.Combine(Path.GetDirectoryName(Instance.Info.Location) ?? throw new InvalidOperationException(), "wiki"));
                    WikiFormat.WikiAppend = true;
                }
                if(useModnameInFile.Value)
                    WikiFormat.WikiModname = "_" + readOnlyContentPack.identifier;
                else
                {
                    WikiFormat.WikiModname = "";
                }
                timer.Start();
                WikiFormat.loredefs = [];

                foreach (var scene in readOnlyContentPack.sceneDefs)
                {
                    var stage = Addressables.LoadAssetAsync<GameObject>(scene.sceneAddress.AssetGUID).WaitForCompletion();
                }
                
                WikiFormat.FormatItem(readOnlyContentPack);
                itemTime += timer.ElapsedMilliseconds;
                timer.Restart();
                
                WikiFormat.FormatEquipment(readOnlyContentPack);
                equipTime += timer.ElapsedMilliseconds;
                timer.Restart();
                
                WikiFormat.FormatSurvivor(readOnlyContentPack);
                survTime += timer.ElapsedMilliseconds;
                timer.Restart();
                
                WikiFormat.FormatSkill(readOnlyContentPack);
                skillTime += timer.ElapsedMilliseconds;
                timer.Restart();
                
                WikiFormat.FormatChallenges(readOnlyContentPack);
                challengeTime += timer.ElapsedMilliseconds;
                timer.Restart();
                
                WikiFormat.FormatBodies(readOnlyContentPack);
                bodyTime += timer.ElapsedMilliseconds;
                timer.Restart();
                
                WikiFormat.FormatBuffs(readOnlyContentPack);
                buffTime += timer.ElapsedMilliseconds;
                timer.Restart();
                
                WikiFormat.FormatStages(readOnlyContentPack);
                stageTime += timer.ElapsedMilliseconds;
                timer.Restart();

                WikiFormat.FormatLore(readOnlyContentPack);
                
                timer.Stop();

                try
                {
                    int fCount = Directory.GetFiles(WikiFormat.WikiOutputPath, "*", SearchOption.TopDirectoryOnly).Length;
                    if (fCount == 0) Directory.Delete(WikiFormat.WikiOutputPath);
                } catch (Exception e)
                {
                    Log.Debug(e);
                }
            }
            
            Log.Info("Exported items in " + (itemTime) + "ms");
            Log.Info("Exported equips in " + (equipTime) + "ms");
            Log.Info("Exported survivors in " + (survTime) + "ms");
            Log.Info("Exported skills in " + (skillTime) + "ms");
            Log.Info("Exported challenges in " + (challengeTime) + "ms");
            Log.Info("Exported bodies in " + (bodyTime) + "ms");
            Log.Info("Exported buffs in " + (buffTime) + "ms");
            Log.Info("Exported stages in " + (buffTime) + "ms");
            
            Log.Info("complete !!!! located at: " + Path.Combine(Path.Combine(Path.GetDirectoryName(Instance.Info.Location))));
            
            Addressables.LoadSceneAsync(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_title.title_unity, LoadSceneMode.Single, activateOnLoad: true).WaitForCompletion();

        }
    }


    
}
