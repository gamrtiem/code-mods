using System;
using BNR.patches;
using BepInEx.Configuration;
using HarmonyLib;
using PhotoMode;
using Rewired;
using RiskOfOptions;
using RiskOfOptions.Options;
using RoR2;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BNR;

public class photomode : PatchBase<photomode>
{
	public override string chainLoaderKey => "com.cwmlolzlz.photomode";
    
	[HarmonyPatch]
	public class PhotomodeChanges
	{
		private static bool timeStop;
		public static float prevTimeScale = -1;
		[HarmonyPatch(typeof(PhotoModeController), "Update")]
		[HarmonyPrefix]
		public static bool UpdatePrefix(PhotoModeController __instance)
        {
            UserProfile userProfile = __instance.cameraRigController.localUserViewer.userProfile;
			Player inputPlayer = __instance.cameraRigController.localUserViewer.inputPlayer;
			if (inputPlayer.GetButton(25))
			{
				__instance.ExitPhotoMode();
				return false;
			}

			if (timeStop)
			{
				prevTimeScale = Time.timeScale;
				Time.timeScale = 0;
				timeStop = false;
			}
			
			if (RoR2.UI.ConsoleWindow.instance?.gameObject)
			{
				return false;
			}
			float mouseLookSensitivity = userProfile.mouseLookSensitivity;
			float mouseLookScaleX = userProfile.mouseLookScaleX;
			float mouseLookScaleY = userProfile.mouseLookScaleY;
			float axis = inputPlayer.GetAxis(23);
			float axis2 = inputPlayer.GetAxis(24);
			float num = 1f;
			if (__instance.gamepad)
			{
				num = 10f;
			}
			if ((__instance.gamepad && inputPlayer.GetButton(9)) || Input.GetMouseButton(1))
			{
				__instance.cameraState.fov = Mathf.Clamp(__instance.Camera.fieldOfView + mouseLookSensitivity * Time.unscaledDeltaTime * axis2 * num, 4f, 120f);
			}
			if ((__instance.gamepad && inputPlayer.GetButton(10)) || Input.GetMouseButton(2))
			{
				__instance.cameraState.roll += (0f - mouseLookScaleX) * mouseLookSensitivity * Time.unscaledDeltaTime * axis * num;
			}
			else
			{
				float value = mouseLookScaleX * mouseLookSensitivity * Time.unscaledDeltaTime * axis * num;
				float value2 = mouseLookScaleY * mouseLookSensitivity * Time.unscaledDeltaTime * axis2 * num;
				__instance.ConditionalNegate(ref value, userProfile.mouseLookInvertX);
				__instance.ConditionalNegate(ref value2, userProfile.mouseLookInvertY);
				float f = __instance.cameraState.roll * (MathF.PI / 180f);
				__instance.cameraState.yaw += __instance.cameraState.fov * (value * Mathf.Cos(f) - value2 * Mathf.Sin(f));
				__instance.cameraState.pitch += __instance.cameraState.fov * ((0f - value2) * Mathf.Cos(f) - value * Mathf.Sin(f));
				__instance.cameraState.pitch = Mathf.Clamp(__instance.cameraState.pitch, -89f, 89f);
			}
			Vector3 vector = new Vector3(inputPlayer.GetAxis(0) * num, 0f, inputPlayer.GetAxis(1) * num);
			if ((__instance.gamepad && inputPlayer.GetButton(7)) || Input.GetKey(KeyCode.Q))
			{
				vector.y -= 1f;
			}
			if ((__instance.gamepad && inputPlayer.GetButton(8)) || Input.GetKey(KeyCode.E))
			{
				vector.y += 1f;
			}
			vector.Normalize();
			if (inputPlayer.GetButton("Sprint"))
			{
				vector *= PhotoModeController.cameraSprintMultiplier;
			}
			if (Input.GetKey(KeyCode.LeftControl))
			{
				vector *= PhotoModeController.cameraSlowMultiplier;
			}
			__instance.cameraState.position += __instance.cameraState.Rotation * vector * Time.unscaledDeltaTime * PhotoModeController.cameraSpeed;
			__instance.Camera.transform.position = __instance.cameraState.position;
			Quaternion quaternion = __instance.cameraState.Rotation;
			if ((double)Mathf.Abs(quaternion.eulerAngles.z) < 2.0)
			{
				quaternion = quaternion.WithEulerAngles(null, null, 0f);
			}
			__instance.Camera.transform.rotation = quaternion;
			__instance.Camera.fieldOfView = __instance.cameraState.fov;
			return false;
        }
		
		[HarmonyPatch(typeof(PhotoModeController), "SetupPhotoModeHUD")]
		[HarmonyPostfix]
		public static void PhotomodePrefix(PhotoModeController __instance)
		{
			
			timeStop = true;
			Log.Debug($"enter photo mode !2 {prevTimeScale}");
		}
		
		[HarmonyPatch(typeof(PhotoModeController), "ExitPhotoMode")]
		[HarmonyPostfix]
		public static void PhotomodePost(PhotoModeController __instance)
		{
			Time.timeScale = prevTimeScale;
			Log.Debug($"leave mode !2 {Time.timeScale}");
		}
	}

    public override void Init()
    {
        if (!enabled.Value) return;
        
        butterscotchnroses.harmony.CreateClassProcessor(typeof(PhotomodeChanges)).Patch();
    }
    
    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("BNR - photomode",
            "enable patches for photomode",
            true,
            "");
        Utils.CheckboxConfig(enabled);
    }
    
    private ConfigEntry<bool> enabled;
}