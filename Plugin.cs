// Copyright (C) 2023 somedevfox <somedevfox@gmail.com>
// 
// This library is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with [vine boom]. If not, see <http://www.gnu.org/licenses/>.


using System;
using System.Collections;
using System.Reflection;
using System.Timers;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Networking;

namespace UltrakillVineBoomMod
{
	public class DecayTimer : MonoBehaviour
	{
		public bool Enabled = false;
		public double Interval = ModData.DECAY_TIMER_INTERVAL;
		private double Elapsed = 0f;

		private void FixedUpdate()
		{
			if (!Enabled)
				return;
			if (Elapsed >= Interval)
			{
				ModData.SoundVolume = 0f;
				Enabled = false;
				Elapsed = 0f;
				return;
			}

			Elapsed += Time.fixedDeltaTime * 1000;
		}

		public void Reset()
		{
			Enabled = false;
			Elapsed = 0;
		}
	}
	public class LogSource
	{
		public static ManualLogSource Death;
		public static ManualLogSource ChangeLevel;
		public static ManualLogSource RestartCheckpoint;
		public static ManualLogSource RestartMission;
		public static ManualLogSource QuitMission;
	}
	public class ModData
	{
		public const string MOD_GUID = "vul.somedevfox.vineboom";
		public const int DECAY_TIMER_INTERVAL = 300000; // 5min

		public static float SoundVolume = 0f;
		public static AudioSource audioSource;
		public static AudioClip audioClip;

		public static IEnumerator LoadAudioClipFromSA(string filename)
		{
			string soundPath = "file:///" + filename;

			using UnityWebRequest request = new(soundPath, "GET", new DownloadHandlerAudioClip(soundPath, AudioType.MPEG), null);
			yield return request.Send();
			audioClip = DownloadHandlerAudioClip.GetContent(request);
			yield break;
		}
		public static void Reset()
		{
			SoundVolume = 0f;
			Plugin.Decay.Reset();
		}
	}
	public class Patches
	{
		/* EnemyIdentifier */
		public static void Death_Patch(EnemyIdentifier __instance)
		{
			if (__instance.dead)
				return;
			LogSource.Death.LogInfo("An enemy has died... Y'know what that means :)");
			if (Plugin.config.soundProgressivelyGetsLouder)
			{
				if (ModData.SoundVolume >= Plugin.config.soundMaximumVolume)
					LogSource.Death.LogWarning("Sound volume has reached maximum setting, not increasing the volume.");
				else
				{
					LogSource.Death.LogInfo("Sound volume is less than the maximum setting, increasing...");
					ModData.SoundVolume += Plugin.config.enemyGlobal;
				}
			}
			LogSource.Death.LogInfo("Playing the Vine Boom:tm: sound");
			ModData.audioSource.PlayOneShot(ModData.audioClip, ModData.SoundVolume);

			if (!Plugin.Decay.Enabled && Plugin.config.timerEnabled)
			{
				LogSource.Death.LogInfo("Decay timer is now active");
				Plugin.Decay.Enabled = true;
			}
		}

		/* OptionsManager */
		public static void ChangeLevel_Patch(string levelname)
		{
			LogSource.ChangeLevel.LogInfo("Change level request received! Resetting sound volume and decay timer...");
			ModData.Reset();
		}
		public static void QuitMission_Patch()
		{
			LogSource.QuitMission.LogInfo("Resetting sound volume and decay timer before exiting the mission...");
			ModData.Reset();
		}
		public static void RestartCheckpoint_Patch()
		{
			LogSource.RestartCheckpoint.LogInfo("Restart request received! Resetting sound volume and decay timer...");
			ModData.Reset();
		}
		public static void RestartMission_Patch()
		{
			LogSource.RestartMission.LogInfo("Restart request received! Resetting sound volume and decay timer...");
			ModData.Reset();
		}
	}

	public class PluginConfig
	{

		/* Sound section */
		public bool soundProgressivelyGetsLouder;
		public float soundMaximumVolume;
		public float soundVolume;
		public string soundFilePath;

		/* Timer section */
		public bool timerEnabled;
		public float timerDecayIn;
		/* Enemy-specific section */
		public float enemyGlobal;

		public PluginConfig(ConfigFile Config)
		{
			soundProgressivelyGetsLouder = Config.Bind("Sound",
													   "ProgressivelyGetsLouder",
													   true,
													   "Whether or not should the sound effect get progressively louder with each enemy kill")
													   .Value;
			soundMaximumVolume = Config.Bind("Sound",
											 "MaximumVolume",
											 1.0f,
											 "What the maximum volume should be (1.0 is 100%)")
											 .Value;
			soundVolume = Config.Bind("Sound",
									  "Volume",
									  1.0f,
									  "What the sound volume should be (this setting is not used if `ProgressivelyGetsLouder` setting is true)")
									  .Value;
			soundFilePath = Config.Bind("Sound",
										"FilePath",
										"{{ModFolder}}/funny.mp3",
										"What sound effect should be used")
										.Value;

			timerEnabled = Config.Bind("Timer",
									   "Enabled",
									   true,
									   "Whether the decay timer is enabled")
									   .Value;
			timerDecayIn = Config.Bind("Timer",
								  "DecayIn",
								  ModData.DECAY_TIMER_INTERVAL,
								  "When should the sound volume be reset in milliseconds")
								  .Value;

			enemyGlobal = Config.Bind("Enemy",
									  "Global",
									  0.1f,
									  "How much should the sound increase in volume for all enemy types")
									  .Value;
		}
	}

	[BepInPlugin(ModData.MOD_GUID, "*vine boom*", PluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin
	{
		public static DecayTimer Decay;
		public static PluginConfig config;
		private Harmony harmony;

		/* Helper methods */
		private void PatchPrefixMethod(MethodInfo method)
		{
			MethodInfo original = AccessTools.Method(method.DeclaringType, method.Name);
			MethodInfo patch = AccessTools.Method(typeof(Patches), method.Name + "_Patch");
			harmony.Patch(original, new HarmonyMethod(patch));
		}
		private void PatchPostfixMethod(MethodInfo method)
		{
			MethodInfo original = AccessTools.Method(method.DeclaringType, method.Name);
			MethodInfo patch = AccessTools.Method(typeof(Patches), method.Name + "_Patch");
			harmony.Patch(original, null, new HarmonyMethod(patch));
		}

		/* Mono events */
		private void Awake()
		{
			Logger.LogInfo($"Plugin {ModData.MOD_GUID} is loaded!");

			/* Load config */
			config = new(Config);
			if (config.soundProgressivelyGetsLouder)
				ModData.SoundVolume = config.soundVolume;
			config.soundFilePath = config
										.soundFilePath
										.Replace(
											"{{ModFolder}}",
											Application.streamingAssetsPath + "/mods/" + ModData.MOD_GUID + "/"
										);

			/* Load the sound */
			StartCoroutine(ModData.LoadAudioClipFromSA(config.soundFilePath));
			ModData.audioSource = gameObject.AddComponent<AudioSource>();

			/* Create log sources */
			LogSource.Death = BepInEx.Logging.Logger.CreateLogSource("EnemyIdentifier.Death");
			LogSource.ChangeLevel = BepInEx.Logging.Logger.CreateLogSource("OptionsManager.ChangeLevel");
			LogSource.QuitMission = BepInEx.Logging.Logger.CreateLogSource("OptionsManager.QuitMission");
			LogSource.RestartCheckpoint = BepInEx.Logging.Logger.CreateLogSource("OptionsManager.RestartCheckpoint");
			LogSource.RestartMission = BepInEx.Logging.Logger.CreateLogSource("OptionsManager.RestartMission");

			/* Attach the decay timer to the current scene */
			Decay = gameObject.AddComponent<DecayTimer>();

			/* Patch in event hooks */
			harmony = new(ModData.MOD_GUID);
			/* ? EnemyIdentifier ? */
			PatchPrefixMethod(typeof(EnemyIdentifier).GetMethod("Death"));
			/* ? OptionsManager ? */
			PatchPrefixMethod(typeof(OptionsManager).GetMethod("ChangeLevel"));
			PatchPrefixMethod(typeof(OptionsManager).GetMethod("QuitMission"));
			PatchPrefixMethod(typeof(OptionsManager).GetMethod("RestartCheckpoint"));
			PatchPrefixMethod(typeof(OptionsManager).GetMethod("RestartMission"));
		}
	}
}
