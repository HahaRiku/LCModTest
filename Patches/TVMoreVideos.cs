using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using UnityEngine;
using UnityEngine.Video;

namespace LCModTest.Patches {
    [HarmonyPatch(typeof(TVScript))]
    public class TVMoreVideos {
        private static bool isVideosLoaded = false;
        private static VideoClip[] finalTvClips = [];
        private static AudioClip[] finalAudioClips = [];
        private static AudioSource? audioSourceForVideo = null;

        [HarmonyPatch("OnEnable")]
        [HarmonyPrefix]
        private static bool TVScriptOnEnablePrefix(TVScript __instance) {
            audioSourceForVideo = Object.Instantiate(__instance.tvSFX, __instance.tvSFX.transform.position, __instance.transform.rotation, __instance.tvSFX.transform.parent);
            __instance.video.audioOutputMode = VideoAudioOutputMode.AudioSource;
            __instance.video.SetTargetAudioSource(0, audioSourceForVideo);

            __instance.video.playOnAwake = false;

            if (!isVideosLoaded) {

                finalTvClips = __instance.tvClips;
                finalAudioClips = __instance.tvAudioClips;

                List<string> directoriesInMoreTvVideos = Directory.GetDirectories(Paths.PluginPath, "moreTvVideos", SearchOption.AllDirectories).ToList();
                List<string> videoBundlePathList = new List<string>();

                for (int i = 0; i < directoriesInMoreTvVideos.Count(); i++) {
                    if (directoriesInMoreTvVideos[i] != "") {
                        string[] videoBundleFiles = Directory.GetFiles(directoriesInMoreTvVideos[i], "*.videobundle");
                        videoBundlePathList.AddRange(videoBundleFiles);
                    }
                }

                for (int i = 0; i < videoBundlePathList.Count; i++) {
                    string videoBundlePath = videoBundlePathList[i];
                    Object[] loadedAssets = AssetBundle.LoadFromFile(videoBundlePath).LoadAllAssets();
                    for (int j = 0; j < loadedAssets.Length; j++) {
                        if (loadedAssets[j] is VideoClip) {
                            VideoClip videoClipAsset = (VideoClip)loadedAssets[j];

                            finalTvClips = finalTvClips.AddToArray(videoClipAsset);
                            AudioClip tempAudioClip = AudioClip.Create("temp", 88200, 1, 44100, true);
                            finalAudioClips = finalAudioClips.AddToArray(tempAudioClip);

                            continue;
                        }
                    }
                }

                isVideosLoaded = true;
            }

            __instance.tvClips = finalTvClips;
            __instance.tvAudioClips = finalAudioClips;

            // continue to original function flow
            return true;
        }

        [HarmonyPatch("OnDisable")]
        [HarmonyPrefix]
        private static bool TVScriptOnDisablePrefix(TVScript __instance) {
            if (audioSourceForVideo != null) {
                Object.Destroy(audioSourceForVideo.gameObject);
                audioSourceForVideo = null;
            }
            return true;
        }

        [HarmonyPatch("TVFinishedClip")]
        [HarmonyPrefix]
        private static bool TVScriptTVFinishedClipPrefix(TVScript __instance) {
            __instance.video.Stop();
            __instance.tvSFX.Stop();

            __instance.video.SetTargetAudioSource(0, audioSourceForVideo);

            return true;
        }

        [HarmonyPatch("TurnTVOnOff")]
        [HarmonyPrefix]
        private static bool TVScriptTurnTVOnOff(TVScript __instance, bool on) {
            __instance.tvOn = on;
            if (on) {
                Traverse tvScriptTraverseInstance = Traverse.Create(__instance);
                float _currentClipTime = (float)(tvScriptTraverseInstance.Field("currentClipTime").GetValue());
                int _currentClip = (int)(tvScriptTraverseInstance.Field("currentClip").GetValue());
                float _audioCurrentClipTime = _currentClipTime;
                if (_currentClipTime > __instance.tvAudioClips[_currentClip].length) {
                    _audioCurrentClipTime = 0;
                }

                __instance.tvSFX.clip = __instance.tvAudioClips[_currentClip];
                __instance.tvSFX.time = _audioCurrentClipTime;
                __instance.tvSFX.Play();
                __instance.tvSFX.PlayOneShot(__instance.switchTVOn);
                WalkieTalkie.TransmitOneShotAudio(__instance.tvSFX, __instance.switchTVOn);
            }
            else {
                __instance.tvSFX.Stop();
                __instance.tvSFX.PlayOneShot(__instance.switchTVOff);
                WalkieTalkie.TransmitOneShotAudio(__instance.tvSFX, __instance.switchTVOff);
            }

            return false;
        }
    }
}

