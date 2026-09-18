using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Audio;
using static LongshipUpgrades.LongshipUpgrades;

namespace LongshipUpgrades
{
    // Only explicitly supplied, mod-owned objects are inspected. Shared vanilla prefabs are never edited.
    internal static class PrefabAudio
    {
        private static readonly HashSet<AudioSource> sources = new HashSet<AudioSource>();
        private static GameObject templates;
        private static AudioMixerGroup sfxGroup;
        private static AudioMixer warnedMixer;

        private static bool IsHeadless => SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;

        internal static GameObject Clone(GameObject prefab)
        {
            if (prefab == null || IsHeadless)
                return prefab;

            if (templates == null)
            {
                templates = new GameObject(pluginID + ".AudioPrefabs");
                templates.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(templates);
            }

            // An inactive parent prevents playback and Awake callbacks while preparing the template.
            GameObject clone = UnityEngine.Object.Instantiate(prefab, templates.transform, false);
            clone.name = prefab.name; // Preserve ZSFX concurrency hashes and any existing prefab identity.
            Register(clone);
            return clone;
        }

        internal static void Register(GameObject root)
        {
            if (root == null || IsHeadless)
                return;

            foreach (AudioSource source in root.GetComponentsInChildren<AudioSource>(true))
                if (source != null)
                    sources.Add(source);

            Apply(AudioMan.instance);
        }

        internal static void Apply(AudioMan audioMan)
        {
            if (audioMan == null || audioMan != AudioMan.instance || audioMan.m_masterMixer == null || IsHeadless)
                return;

            sources.RemoveWhere(source => source == null);
            if (sources.Count == 0)
                return;

            AudioMixer mixer = audioMan.m_masterMixer;
            if (sfxGroup == null || sfxGroup.audioMixer != mixer)
            {
                sfxGroup = null;
                foreach (AudioMixerGroup group in mixer.FindMatchingGroups(string.Empty))
                    if (group != null && string.Equals(group.name, "Sfx", StringComparison.OrdinalIgnoreCase))
                    {
                        sfxGroup = group;
                        break;
                    }
            }

            if (sfxGroup == null)
            {
                if (warnedMixer != mixer)
                {
                    warnedMixer = mixer;
                    Debug.LogWarning(pluginID + ": the game's Sfx mixer group was not found; audio routing was not changed.");
                }
                return;
            }

            warnedMixer = null;
            int changed = 0;
            foreach (AudioSource source in sources)
            {
                // These explicitly selected lantern and interaction sounds are all world sound effects.
                if (source.outputAudioMixerGroup == sfxGroup)
                    continue;

                source.outputAudioMixerGroup = sfxGroup;
                changed++;
            }

            if (changed > 0)
                LogInfo($"Routed {changed} audio source(s) to the game's Sfx mixer.");
        }

        internal static void Clear()
        {
            sources.Clear();
            sfxGroup = null;
            warnedMixer = null;
            if (templates != null)
                UnityEngine.Object.Destroy(templates);
            templates = null;
        }

        [HarmonyPatch(typeof(AudioMan), nameof(AudioMan.Awake))]
        private static class AudioMan_Awake_RouteAudio
        {
            private static void Postfix(AudioMan __instance) => Apply(__instance);
        }
    }
}
