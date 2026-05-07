// Warning: Some assembly references could not be resolved automatically. This might lead to incorrect decompilation of some parts,
// for ex. property getter/setter access. To get optimal decompilation results, please manually add the missing references to the list of loaded assemblies.

// /run/media/icebrah/newdrive/downloads/DTEE-Icons-1.4.1/dteeicons.dll
// dteeicons, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// Global type: <Module>
// Architecture: AnyCPU (64-bit preferred)
// Runtime: v4.0.30319
// Hash algorithm: SHA1

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using On.RoR2.UI;
using RiskOfOptions;
using RiskOfOptions.Options;
using RoR2;
using UnityEngine;
using UnityEngine.UI;
using BodyCatalog = RoR2.BodyCatalog;
using CharacterBody = RoR2.CharacterBody;
using CharacterSelectController = On.RoR2.UI.CharacterSelectController;
using DLC1Content = RoR2.DLC1Content;
using DroneCatalog = RoR2.DroneCatalog;
using DroneDef = RoR2.DroneDef;
using HealthComponent = On.RoR2.HealthComponent;
using Language = RoR2.Language;
using LocalUser = RoR2.LocalUser;
using LocalUserManager = RoR2.LocalUserManager;
using RoR2Application = RoR2.RoR2Application;
using RoR2Content = RoR2.RoR2Content;
using SkinCatalog = RoR2.SkinCatalog;
using SkinDef = RoR2.SkinDef;
using SurvivorIconController = RoR2.UI.SurvivorIconController;

namespace icons
{
	[BepInPlugin("DTEE.Icons", "Icons", "1.4.1")]
	public class dteeicons : BaseUnityPlugin
	{
		private static AssetBundle assetBundle;
		private static Dictionary<string, string> iconOverrides;
		private Dictionary<string, Texture2D> loadedTextures = new();
		private Dictionary<SurvivorIconController, int> lastSeenSkin = new();
		public static ConfigEntry<bool> RunExtras { get; set; }
		public static ConfigEntry<bool> SkinSpecificIcons { get; set; }
		public static ConfigEntry<bool> AggressiveLogging { get; set; }

		public void Start()
		{
			Log.Init(Logger);
			
			RunExtras = Config.Bind("Runtime Asset Changes", "Enable Runtime Assets",
				true,
				"Enables asset changes that would occur after loading the game. This enables compatibility for items like Halcyon Seed, Happiest Mask, and Goobo Jr! This may have minor performance impacts, as I'm still working out the code.");
			SkinSpecificIcons = Config.Bind("Skin-Specific Icons (Experimental)",
				"Enable Skin Icons", false,
				"Enables icons to be loaded in specific scenarios so that skins have their own icons. Haven't made a lot of assets for this, so this is mostly for my testing...");
			AggressiveLogging = Config.Bind("Aggressive Logging",
				"Enable Aggressive Logging", false,
				"Enables logging in a lot more situations. This is mostly for debugging purposes, but maybe you like watching text scroll!");
			
			ModSettingsManager.AddOption(new CheckBoxOption(RunExtras));
			ModSettingsManager.AddOption(new CheckBoxOption(SkinSpecificIcons));
			ModSettingsManager.AddOption(new CheckBoxOption(AggressiveLogging));
			ModSettingsManager.SetModDescription("This mod replaces in-game renders with hand drawn art assets.");
			
			assetBundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location)!, "dteeicons.bundle"));
			Sprite modIcon = assetBundle.LoadAsset<Sprite>("Assets/DevotedLemurian.png");
			
			ModSettingsManager.SetModIcon(modIcon);
			SetupHooks();
			SetUpImageOverrides();

			RoR2Application.onLoad += ApplyPortraitIcons;
			
			if (RunExtras.Value)
			{
				Texture2D GhostTexture = assetBundle.LoadAsset<Texture2D>("Assets/HappiestMask.png");
				Texture2D GooboTexture = assetBundle.LoadAsset<Texture2D>("Assets/Goobo.png");
				
				CharacterBody.onBodyStartGlobal += delegate(CharacterBody body)
				{
					if (body.master && body.master.inventory)
					{
						if (body.master.inventory.GetItemCountPermanent(DLC1Content.Items.GummyCloneIdentifier) > 0 &&
						    body.teamComponent.teamIndex == TeamIndex.Player)
						{
							body.portraitIcon = GooboTexture;
							if (AggressiveLogging.Value)
							{
								Log.Message("We got goo!");
							}
						}

						if (body.master.inventory.GetItemCountPermanent(RoR2Content.Items.Ghost) > 0 &&
						    body.teamComponent.teamIndex == TeamIndex.Player)
						{
							body.portraitIcon = GhostTexture;
							if (AggressiveLogging.Value)
							{
								Log.Message("We got a ghost!");
							}
						}
					}

					if (body.bodyIndex == BodyCatalog.FindBodyIndex("TitanGoldBody"))
					{
						string text = "AurelioniteBoss";
						if (body.teamComponent.teamIndex == TeamIndex.Player)
						{
							text = "AurelioniteAlly";
						}

						body.portraitIcon = assetBundle.LoadAsset<Texture2D>("Assets/" + text + ".png");
					}
				};
			}

			if (Chainloader.PluginInfos.ContainsKey("com.rob.Dante"))
			{
				BodyCatalog.availability.CallWhenAvailable(LoadDanteAssets);
			}

			if (Chainloader.PluginInfos.ContainsKey("com.vcr.GhoulSurvMod"))
			{
				BodyCatalog.availability.CallWhenAvailable(LoadGhoulAssets);
			}
		}

		private void SetUpImageOverrides()
		{
			iconOverrides = new Dictionary<string, string>
			{
				["Drone1BodyRemoteOp"] = "Drone1",
				["Drone2BodyRemoteOp"] = "Drone2",
				["Turret1BodyRemoteOp"] = "Turret1",
				["EmergencyDroneBodyRemoteOp"] = "EmergencyDrone",
				["JunkDroneBodyRemoteOp"] = "JunkDrone",
				["HaulerDroneBodyRemoteOp"] = "HaulerDrone",
				["RechargeDroneBodyRemoteOp"] = "RechargeDrone",
				["JailerDroneBodyRemoteOp"] = "JailerDrone",
				["MegaDroneBodyRemoteOp"] = "MegaDrone",
				["FlameDroneBodyRemoteOp"] = "FlameDrone",
				["MissileDroneBodyRemoteOp"] = "MissileDrone",
				["BombardmentDroneBodyRemoteOp"] = "BombardmentDrone",
				["CleanupDroneBodyRemoteOp"] = "CleanupDrone",
				["CopycatDroneBodyRemoteOp"] = "CopycatDrone",
				["ShockDroneBodyRemoteOp"] = "ShockDrone",
				["VoltaicDroneBodyRemoteOp"] = "VoltaicDrone",
				["InfernoDroneBodyRemoteOp"] = "InfernoDrone",
				["BackupDroneBodyRemoteOp"] = "BackupDrone",
				["VoidRaidCrabBody"] = "VoidRaidCrab",
				["MiniVoidRaidCrabBodyBase"] = "VoidRaidCrab",
				["MiniVoidRaidCrabBodyPhase1"] = "VoidRaidCrab",
				["MiniVoidRaidCrabBodyPhase2"] = "VoidRaidCrab",
				["MiniVoidRaidCrabBodyPhase3"] = "VoidRaidCrab",
				["FalseSonBossBody"] = "FalseSonBoss",
				["FalseSonBossBodyLunarShard"] = "FalseSonBoss",
				["FalseSonBossBodyBrokenLunarShard"] = "FalseSonBoss"
			};
			if (Chainloader.PluginInfos.ContainsKey("com.BigBadPigeon.DressUpMithrix"))
			{
				iconOverrides["BrotherBody"] = "BrotherClothed";
				iconOverrides["BrotherGlassBody"] = "BrotherClothed";
				iconOverrides["BrotherHauntBody"] = "BrotherClothed";
				iconOverrides["BrotherHurtBody"] = "BrotherClothed";
				iconOverrides["ITBrotherBody"] = "BrotherClothed";
			}
			else
			{
				iconOverrides["BrotherBody"] = "Brother";
				iconOverrides["BrotherGlassBody"] = "Brother";
				iconOverrides["BrotherHauntBody"] = "Brother";
				iconOverrides["BrotherHurtBody"] = "Brother";
				iconOverrides["ITBrotherBody"] = "Brother";
			}
		}

		private void ApplyPortraitIcons()
		{
			foreach (GameObject allBodyPrefab in BodyCatalog.allBodyPrefabs)
			{
				if (allBodyPrefab == null)
				{
					continue;
				}

				string name = allBodyPrefab.name;
				if (!iconOverrides.TryGetValue(name, out var value))
				{
					string text;
					if (!name.EndsWith("Body"))
					{
						text = name;
					}
					else
					{
						string text2 = name;
						text = text2.Substring(0, text2.Length - 4);
					}

					value = text;
				}

				string text3 = "Assets/" + value + ".png";
				CharacterBody component = allBodyPrefab.GetComponent<CharacterBody>();
				if (component == null)
				{
					if (AggressiveLogging.Value)
					{
						Log.Error("CharacterBody for " + allBodyPrefab.name + " returned null!");
					}

					continue;
				}

				Texture2D cachedTexture = GetCachedTexture(text3, null);
				if (cachedTexture == null)
				{
					if (AggressiveLogging.Value)
					{
						Log.Error("Failed to load texture for body " + name + " under path " + text3 + "!");
					}

					continue;
				}

				Log.Message("Successfully found texture for " + name + " under path " + text3 + "!");
				if (component.portraitIcon == null)
				{
					if (AggressiveLogging.Value)
					{
						Log.Debug("Pre-existing Portrait icon for " + name + " is null, and may be getting overriden at a later point.");
					}

					continue;
				}

				component.portraitIcon = cachedTexture;
				if (name == "RobHunkBody")
				{
					string text4 = ((Language.GetString("ROB_HUNK_BODY_NAME") == "HUNK")
						? "Assets/RobHunk.png"
						: "Assets/RobSpecialist.png");
					Texture2D cachedTexture2 = GetCachedTexture(text4, null);
					if ((bool)cachedTexture2)
					{
						component.portraitIcon = cachedTexture2;
						if (AggressiveLogging.Value)
						{
							Log.Info("[Startup] Set HUNK base portrait to: " + text4);
						}
					}
				}

				DroneDef droneDef = DroneCatalog.FindDroneDefFromBody(allBodyPrefab);
				if (droneDef != null)
				{
					Log.Debug("DroneDef found for " + name + ".");
					droneDef.iconSprite = Sprite.Create(cachedTexture,
						new Rect(0f, 0f, cachedTexture.width, cachedTexture.height), new Vector2(0.5f, 0.5f));
				}
			}
		}

		private void SetupHooks()
		{
			On.RoR2.UI.SurvivorIconController.Update += SurvivorIconControllerOnUpdate;
			CharacterSelectController.OnDisable += CharacterSelectControllerOnOnDisable;
			On.RoR2.CharacterBody.Start += CharacterBodyOnStart;
			//On.RoR2.UI.AllyCardController.Awake += AllyCardControllerOnAwake;
			On.RoR2.UI.AllyCardController.LateUpdate += AllyCardControllerOnLateUpdate;
			On.RoR2.UI.ScoreboardStrip.Update += ScoreboardStripOnUpdate;
		}

		private void ScoreboardStripOnUpdate(ScoreboardStrip.orig_Update orig, RoR2.UI.ScoreboardStrip self)
		{
			orig(self);
			Log.Debug("asdasdasdasdasddfgsd");
			float health = self.userBody.healthComponent.healthFraction;
			self.classIcon.GetComponent<RawImage>().color = new Color(1 + (1 - health) * intensity, 1 - (1 - health), 1 - (1 - health), 1f);
		}

		private void AllyCardControllerOnLateUpdate(AllyCardController.orig_LateUpdate orig, RoR2.UI.AllyCardController self)
		{
			orig(self);
			Log.Debug("asdasdasdsa");
			float health = self.sourceMaster.GetBody().healthComponent.healthFraction;
			self.portraitIconImage.GetComponent<RawImage>().color = new Color(1 + (1 - health) * intensity, 1 - (1 - health), 1 - (1 - health), 1f);
		}

		private void AllyCardControllerOnUpdateInfo(AllyCardController.orig_UpdateInfo orig, RoR2.UI.AllyCardController self)
		{
			orig(self);
			Log.Debug("asdbasdasd");
			float health = self.sourceMaster.GetBody().healthComponent.healthFraction;
			self.portraitIconImage.GetComponent<RawImage>().color = new Color(1 + (1 - health) * intensity, 1 - (1 - health), 1 - (1 - health), 1f);
		}

		private void AllyCardControllerOnAwake(AllyCardController.orig_Awake orig, RoR2.UI.AllyCardController self)
		{
			orig(self);
			self.portraitIconImage.GetComponent<RawImage>().color = new Color(1, 1, 1, 0.5f);
		}

		public class healthRedChecker : MonoBehaviour
		{
			public CharacterBody body;
			private float lastHealth; 

			private void OnEnable()
			{
				lastHealth = body.healthComponent.health;
			}

			private void FixedUpdate()
			{
				if (Math.Abs(lastHealth - body.healthComponent.health) < 0.01) return;
				
				body.portraitIcon = Evilify(body, body.healthComponent.healthFraction);
				lastHealth = body.healthComponent.health;
			}
		}

		public static float intensity = 3f;
		public static Dictionary<CharacterBody, Texture> woaw = [];
		private static Texture Evilify(CharacterBody body, float health)
		{
			if (woaw.TryGetValue(body, out Texture texture))
			{
				Texture2D texture2D = makeReadable(texture);
				Color[] pixels = texture2D.GetPixels(0, 0, texture.width, texture.height);
				
				for (int i = 0; i < pixels.Length; i++)
				{
					pixels[i].r *= 1 + (1 - health) * intensity; 
					pixels[i].g *= 1 - (1 - health); 
					pixels[i].b *= 1 - (1 - health); 
				}

				Debug.Log(1 + (1 - health) * intensity);
				Debug.Log(1 - (1 - health));
				texture2D.SetPixels(pixels);
				texture2D.Apply();
				
				return texture2D;
			}

			Log.Debug("gorp ,.., ");
			return body.portraitIcon;
		}
		
		static Texture2D makeReadable(Texture texture)
		{
			var tmp = RenderTexture.GetTemporary(texture.width, texture.height, 32);
			tmp.name = "Whatever";
			tmp.enableRandomWrite = true;
			tmp.Create();
			// Blit the pixels on texture to the RenderTexture
			UnityEngine.Graphics.Blit(texture, tmp);
			// Backup the currently set RenderTexture
			RenderTexture previous = RenderTexture.active;
			// Set the current RenderTexture to the temporary one we created
			RenderTexture.active = tmp;
			// Create a new readable Texture2D to copy the pixels to it
			Texture2D myTexture2D = new Texture2D(texture.width, texture.height);
			// Copy the pixels from the RenderTexture to the new Texture
			myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
			myTexture2D.Apply();
			// Reset the active RenderTexture
			RenderTexture.active = previous;
			// Release the temporary RenderTexture
			RenderTexture.ReleaseTemporary(tmp);

			return myTexture2D;
		}
		
		[ConCommand(commandName = "intensity", flags = ConVarFlags.None, helpText = "asdasdasd")]
		public static void akstopsound(ConCommandArgs args)
		{
			if (float.TryParse(args[0], out float intensityNew))
			{
				intensity = intensityNew;
			}
		}
		
		private void Update()
		{
#if DEBUG
			if (Input.GetKeyUp(KeyCode.F5))
			{
				UnityHotReloadNS.UnityHotReload.LoadNewAssemblyVersion(typeof(icons.dteeicons).Assembly, System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location)!, "icons.dll"));
			}
#endif
		}

		private void CharacterBodyOnStart(On.RoR2.CharacterBody.orig_Start orig, CharacterBody self)
		{
			orig.Invoke(self);
			//self.gameObject.AddComponent<healthRedChecker>().body = self;
			//woaw.Add(self, self.portraitIcon);
			
			if (self.bodyIndex != BodyIndex.None)
			{
				if (self.bodyIndex == BodyCatalog.FindBodyIndex("TitanGoldBody"))
				{
					if (AggressiveLogging.Value)
					{
						Log.Info("Aurelionite found! Skippin skin-specific icon. Hi Aurelionite :)");
					}
				}
				else
				{
					if (self.master && self.master.inventory)
					{
						if (self.master.inventory.GetItemCountPermanent(DLC1Content.Items.GummyCloneIdentifier) > 0 &&
						    self.teamComponent.teamIndex == TeamIndex.Player)
						{
							if (AggressiveLogging.Value)
							{
								Log.Info("Goo found! Skipping skin-specific icon.");
							}

							return;
						}

						if (self.master.inventory.GetItemCountPermanent(RoR2Content.Items.Ghost) > 0 &&
						    self.teamComponent.teamIndex == TeamIndex.Player)
						{
							if (AggressiveLogging.Value)
							{
								Log.Info("Ghost found! Skipping skin-specific icon.");
							}

							return;
						}
					}

					ApplyDynamicIcon(self.bodyIndex, (int)self.skinIndex,
						delegate(Texture2D tex) { self.portraitIcon = tex; }, "Body Loaded!");
				}
			}
		}

		private void CharacterSelectControllerOnOnDisable(CharacterSelectController.orig_OnDisable orig,
			RoR2.UI.CharacterSelectController self)
		{
			orig.Invoke(self);
			lastSeenSkin.Clear();
		}

		private void SurvivorIconControllerOnUpdate(On.RoR2.UI.SurvivorIconController.orig_Update orig,
			SurvivorIconController self)
		{
			orig.Invoke(self);
			if (SkinSpecificIcons.Value && (bool)self.survivorDef)
			{
				BodyIndex bodyIndex = BodyCatalog.FindBodyIndex(self.survivorDef.bodyPrefab);
				LocalUser firstLocalUser = LocalUserManager.GetFirstLocalUser();
				if (firstLocalUser != null)
				{
					int skinIndex = (int)firstLocalUser.userProfile.loadout.bodyLoadoutManager.GetSkinIndex(bodyIndex);
					if (!lastSeenSkin.TryGetValue(self, out var value) || value != skinIndex)
					{
						ApplyDynamicIcon(bodyIndex, skinIndex,
							delegate(Texture2D tex) { self.survivorIcon.texture = tex; }, "Skin Swapped!");
						lastSeenSkin[self] = skinIndex;
					}
				}
			}
		}

		private void ApplyDynamicIcon(BodyIndex bIndex, int sIndex, Action<Texture2D> applyAction, string logPrefix)
		{
			SkinDef bodySkinDef = SkinCatalog.GetBodySkinDef(bIndex, sIndex);
			if (!bodySkinDef)
			{
				return;
			}

			string bodyName = BodyCatalog.GetBodyName(bIndex);
			if (bodyName == "RobDanteBody" || bodyName == "VcrGhoulBody")
			{
				return;
			}

			if (!iconOverrides.TryGetValue(bodyName, out var value))
			{
				string text;
				if (!bodyName.EndsWith("Body"))
				{
					text = bodyName;
				}
				else
				{
					string text2 = bodyName;
					text = text2.Substring(0, text2.Length - 4);
				}

				value = text;
			}

			string text3 = "Assets/" + value + bodySkinDef.name + ".png";
			string fallbackPath = "Assets/" + value + ".png";
			if (AggressiveLogging.Value)
			{
				Log.Info(logPrefix + " Body: " + bodyName + " | Skin: " + bodySkinDef.name + " | Path: " + text3);
			}

			if (bodyName == "RobHunkBody")
			{
				string path = ((Language.GetString("ROB_HUNK_BODY_NAME") == "HUNK")
					? "Assets/RobHunk.png"
					: "Assets/RobSpecialist.png");
				Texture2D cachedTexture = GetCachedTexture(path, null);
				if ((bool)cachedTexture)
				{
					applyAction(cachedTexture);
				}
			}
			else
			{
				Texture2D cachedTexture2 = GetCachedTexture(text3, fallbackPath);
				if ((bool)cachedTexture2)
				{
					applyAction(cachedTexture2);
				}
			}
		}

		private Texture2D GetCachedTexture(string path, string fallbackPath)
		{
			if (loadedTextures.TryGetValue(path, out var value))
			{
				return value;
			}

			if (assetBundle.Contains(path))
			{
				value = assetBundle.LoadAsset<Texture2D>(path);
				loadedTextures.Add(path, value);
			}
			else if (!string.IsNullOrEmpty(fallbackPath) && assetBundle.Contains(fallbackPath))
			{
				if (loadedTextures.TryGetValue(fallbackPath, out value))
				{
					return value;
				}

				value = assetBundle.LoadAsset<Texture2D>(fallbackPath);
				loadedTextures.Add(fallbackPath, value);
			}

			return value;
		}

		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		private void LoadDanteAssets()
		{
			//DanteAssets.danteIcon = assetBundle.LoadAsset<Sprite>("Assets/RobDante.png");
			//DanteAssets.danteDevilIcon = assetBundle.LoadAsset<Sprite>("Assets/RobDante.png");
			//DanteAssets.leonIcon = assetBundle.LoadAsset<Sprite>("Assets/RobSpecialist.png");
		}

		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		private void LoadGhoulAssets()
		{
			//GhoulAssets.baseIcon = assetBundle.LoadAsset<Sprite>("Assets/VcrGhoul.png").texture;
		}
	}
}
