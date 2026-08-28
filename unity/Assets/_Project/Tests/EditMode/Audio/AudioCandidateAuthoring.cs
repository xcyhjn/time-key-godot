using System;
using System.Reflection;
using TimeKey.Presentation.Audio;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace TimeKey.Tests.EditMode.Audio
{
    public static class AudioCandidateAuthoring
    {
        public const string CatalogPath =
            "Assets/_Project/Audio/Wave04/Wave04AudioCueCatalog.asset";
        public const string PrefabPath =
            "Assets/_Project/Prefabs/Audio/Wave04AudioRootCandidate.prefab";
        public const string MixerPath =
            "Assets/_Project/Audio/Wave04/Wave04AudioMixer.mixer";

        [MenuItem("TimeKey/Audio/Build Wave 04 Audio Candidates")]
        public static void BuildCandidateAssets()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<AudioCueCatalog>(CatalogPath);
            if (catalog == null)
            {
                throw new InvalidOperationException(
                    "Wave 04 audio catalog is missing at " + CatalogPath);
            }

            var mixer = LoadOrCreateMixer();
            var masterGroup = FindMasterGroup(mixer);
            var musicGroup = FindSingleGroup(mixer, "Music");
            var sfxGroup = FindSingleGroup(mixer, "SFX");

            var rootObject = new GameObject("Wave04AudioRootCandidate");
            try
            {
                var root = rootObject.AddComponent<AudioRoot>();
                var bgmA = AddSource(rootObject.transform, "BgmSourceA");
                var bgmB = AddSource(rootObject.transform, "BgmSourceB");
                var looping = AddSource(rootObject.transform, "LoopingSfxSource");
                var oneShots = new AudioSource[6];
                for (var index = 0; index < oneShots.Length; index++)
                {
                    oneShots[index] = AddSource(
                        rootObject.transform,
                        "OneShotSource" + (index + 1));
                }

                var serialized = new SerializedObject(root);
                serialized.FindProperty("cueCatalog").objectReferenceValue = catalog;
                serialized.FindProperty("mixer").objectReferenceValue = mixer;
                serialized.FindProperty("masterMixerGroup").objectReferenceValue = masterGroup;
                serialized.FindProperty("musicMixerGroup").objectReferenceValue = musicGroup;
                serialized.FindProperty("sfxMixerGroup").objectReferenceValue = sfxGroup;
                serialized.FindProperty("bgmSourceA").objectReferenceValue = bgmA;
                serialized.FindProperty("bgmSourceB").objectReferenceValue = bgmB;
                serialized.FindProperty("loopingSfxSource").objectReferenceValue = looping;
                var pool = serialized.FindProperty("oneShotPool");
                pool.arraySize = oneShots.Length;
                for (var index = 0; index < oneShots.Length; index++)
                {
                    pool.GetArrayElementAtIndex(index).objectReferenceValue = oneShots[index];
                }

                serialized.FindProperty("crossfadeDuration").floatValue = 1f;
                serialized.FindProperty("deterministicPitchSeed").intValue = 731;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var directory = System.IO.Path.GetDirectoryName(PrefabPath)
                    ?.Replace('\\', '/');
                if (!AssetDatabase.IsValidFolder(directory))
                {
                    AssetDatabase.CreateFolder(
                        "Assets/_Project/Prefabs",
                        "Audio");
                }

                PrefabUtility.SaveAsPrefabAsset(rootObject, PrefabPath);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }

        private static AudioMixer LoadOrCreateMixer()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (existing != null &&
                existing.FindMatchingGroups("Master").Length == 1 &&
                existing.FindMatchingGroups("Music").Length == 1 &&
                existing.FindMatchingGroups("SFX").Length == 1)
            {
                return existing;
            }

            if (existing != null && !AssetDatabase.DeleteAsset(MixerPath))
            {
                throw new InvalidOperationException(
                    "Could not replace the incomplete Wave 04 mixer candidate.");
            }

            var controllerType = Type.GetType(
                "UnityEditor.Audio.AudioMixerController, UnityEditor");
            if (controllerType == null)
            {
                throw new InvalidOperationException(
                    "Unity 6000.4 AudioMixerController editor type is unavailable.");
            }

            var create = RequiredMethod(
                controllerType,
                "CreateMixerControllerAtPath",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var controller = create.Invoke(null, new object[] { MixerPath });
            if (!(controller is AudioMixer mixer))
            {
                throw new InvalidOperationException("AudioMixer creation did not return a mixer.");
            }

            var master = controllerType.GetProperty(
                    "masterGroup",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(controller);
            if (master == null)
            {
                throw new InvalidOperationException("Created AudioMixer has no master group.");
            }

            var music = AddGroup(controllerType, controller, master, "Music");
            var sfx = AddGroup(controllerType, controller, master, "SFX");
            ExposeVolume(controllerType, controller, master, "MasterVolume");
            ExposeVolume(controllerType, controller, music, "MusicVolume");
            ExposeVolume(controllerType, controller, sfx, "SfxVolume");
            RequiredMethod(
                    controllerType,
                    "SanitizeGroupViews",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Invoke(controller, null);
            EditorUtility.SetDirty(mixer);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(MixerPath, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        }

        private static object AddGroup(
            Type controllerType,
            object controller,
            object master,
            string name)
        {
            var group = RequiredMethod(
                    controllerType,
                    "CreateNewGroup",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Invoke(controller, new object[] { name, false });
            RequiredMethod(
                    controllerType,
                    "AddChildToParent",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Invoke(controller, new[] { group, master });
            return group;
        }

        private static void ExposeVolume(
            Type controllerType,
            object controller,
            object group,
            string exposedName)
        {
            var groupType = group.GetType();
            var volumeGuid = RequiredMethod(
                    groupType,
                    "GetGUIDForVolume",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Invoke(group, null);
            var pathType = controllerType.Assembly.GetType(
                "UnityEditor.Audio.AudioGroupParameterPath");
            var path = Activator.CreateInstance(
                pathType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { group, volumeGuid },
                null);
            RequiredMethod(
                    controllerType,
                    "AddExposedParameter",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Invoke(controller, new[] { path });

            var exposedProperty = controllerType.GetProperty(
                "exposedParameters",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var exposed = exposedProperty?.GetValue(controller) as Array;
            if (exposed == null || exposed.Length == 0)
            {
                throw new InvalidOperationException(
                    "AudioMixer did not expose " + exposedName + ".");
            }

            var index = exposed.Length - 1;
            var parameter = exposed.GetValue(index);
            parameter.GetType().GetField(
                    "name",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.SetValue(parameter, exposedName);
            exposed.SetValue(parameter, index);
            exposedProperty.SetValue(controller, exposed);
        }

        private static MethodInfo RequiredMethod(
            Type type,
            string name,
            BindingFlags flags)
        {
            return type.GetMethod(name, flags) ??
                throw new MissingMethodException(type.FullName, name);
        }

        private static AudioMixerGroup FindSingleGroup(AudioMixer mixer, string name)
        {
            var groups = mixer.FindMatchingGroups(name);
            if (groups.Length != 1)
            {
                throw new InvalidOperationException(
                    "Expected exactly one AudioMixer group named " + name + ".");
            }

            return groups[0];
        }

        private static AudioMixerGroup FindMasterGroup(AudioMixer mixer)
        {
            var controllerType = Type.GetType(
                "UnityEditor.Audio.AudioMixerController, UnityEditor");
            var master = controllerType?.GetProperty(
                    "masterGroup",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(mixer) as AudioMixerGroup;
            if (master == null)
            {
                throw new InvalidOperationException(
                    "Expected the Wave 04 mixer to expose its Master group.");
            }

            return master;
        }

        private static AudioSource AddSource(Transform parent, string name)
        {
            var sourceObject = new GameObject(name);
            sourceObject.transform.SetParent(parent, false);
            var source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            return source;
        }
    }
}
