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
using System.Net;
using System.Reflection;
using System.Threading;
using System.Timers;
using BepInEx;
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
            if(!Enabled)
                return;
            if(Elapsed >= Interval) {
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
		public static float SoundMaximumVolume = 1f;
        public static AudioSource audioSource;
		public static AudioClip audioClip;

		public static IEnumerator LoadAudioClipFromSA(string filename)
		{
			string modPath = "file:///" + Application.streamingAssetsPath + "/mods/" + MOD_GUID + "/";
			string audioPath = string.Format(modPath + "{0}", filename);

			using UnityWebRequest request = new(audioPath, "GET", new DownloadHandlerAudioClip(audioPath, AudioType.MPEG), null);
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
            if(__instance.dead)
                return;
			LogSource.Death.LogInfo("An enemy has died... Y'know what that means :)");
			if (ModData.SoundVolume >= ModData.SoundMaximumVolume)
				LogSource.Death.LogWarning("Sound volume has reached maximum setting, not increasing the volume.");
			else
			{
				LogSource.Death.LogInfo("Sound volume is less than the maximum setting, increasing...");
				ModData.SoundVolume += 0.1f;
			}
			LogSource.Death.LogInfo("Playing Vine Boom:tm: sound");
            ModData.audioSource.PlayOneShot(ModData.audioClip, ModData.SoundVolume);

            if(!Plugin.Decay.Enabled) {
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

	[BepInPlugin(ModData.MOD_GUID, "*vine boom*", PluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin
	{
        public static DecayTimer Decay;
        private Harmony harmony;

        /* Events */
        private void OnTimedEvent(System.Object source, ElapsedEventArgs e)
        {
            Console.WriteLine(e.SignalTime);
        }

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
			/* Plugin startup logic */
			Logger.LogInfo($"Plugin {ModData.MOD_GUID} is loaded!");

			/* Load the sound */
			StartCoroutine(ModData.LoadAudioClipFromSA("funny.mp3"));
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
