using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TimeKey.Presentation.Audio;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace TimeKey.Tests.EditMode.Audio
{
    public sealed class AudioCueCatalogTests
    {
        private readonly List<UnityEngine.Object> _created = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var item in _created)
            {
                if (item != null)
                {
                    UnityEngine.Object.DestroyImmediate(item);
                }
            }

            _created.Clear();
        }

        [Test]
        public void ContractCatalog_MapsAllStableIdsAndDeclaredRoutes()
        {
            var entries = new List<AudioCueDefinition>();
            foreach (var stableId in AudioCueIds.All)
            {
                var route = stableId.StartsWith("bgm.", StringComparison.Ordinal)
                    ? AudioBusRoute.Music
                    : AudioBusRoute.Sfx;
                entries.Add(new AudioCueDefinition(stableId, CreateClip(stableId), route,
                    route == AudioBusRoute.Music));
            }

            var catalog = CreateCatalog(entries);

            Assert.That(catalog.Validate().Any(item => item.IsError), Is.False);
            Assert.That(catalog.Cues, Has.Count.EqualTo(AudioCueIds.All.Count));
            foreach (var stableId in AudioCueIds.All)
            {
                Assert.That(catalog.TryGet(stableId, out var cue), Is.True, stableId);
                Assert.That(cue.StableId, Is.EqualTo(stableId));
                Assert.That(cue.Route, Is.EqualTo(
                    stableId.StartsWith("bgm.", StringComparison.Ordinal)
                        ? AudioBusRoute.Music
                        : AudioBusRoute.Sfx));
            }
        }

        [Test]
        public void CatalogValidation_RejectsDuplicateAndEmptyStableIds()
        {
            var entries = new[]
            {
                new AudioCueDefinition("sfx.draw_card", CreateClip("one"), AudioBusRoute.Sfx),
                new AudioCueDefinition("sfx.draw_card", CreateClip("two"), AudioBusRoute.Sfx),
                new AudioCueDefinition(string.Empty, CreateClip("empty"), AudioBusRoute.Sfx)
            };
            var catalog = CreateCatalog(entries);

            var diagnostics = catalog.Validate();

            Assert.That(diagnostics.Count(item => item.IsError), Is.EqualTo(2));
            StringAssert.Contains("duplicate", string.Join("\n", diagnostics).ToLowerInvariant());
            StringAssert.Contains("empty", string.Join("\n", diagnostics).ToLowerInvariant());
        }

        [Test]
        public void CandidateAsset_ContainsAllImportedContractClips()
        {
            const string path =
                "Assets/_Project/Audio/Wave04/Wave04AudioCueCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<AudioCueCatalog>(path);

            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Cues, Has.Count.EqualTo(AudioCueIds.All.Count));
            foreach (var stableId in AudioCueIds.All)
            {
                Assert.That(catalog.TryGet(stableId, out var cue), Is.True, stableId);
                Assert.That(cue.Clip, Is.Not.Null, stableId);
                Assert.That(cue.Clip.length, Is.GreaterThan(0f), stableId);
                Assert.That(cue.Clip.channels, Is.EqualTo(2), stableId);
                Assert.That(cue.Clip.frequency, Is.EqualTo(48000), stableId);
                var clipPath = AssetDatabase.GetAssetPath(cue.Clip);
                Assert.That(clipPath, Does.StartWith("Assets/_Project/Audio/Wave04/"));
                var importer = AssetImporter.GetAtPath(clipPath) as AudioImporter;
                Assert.That(importer, Is.Not.Null, stableId);
                var expectedLoadType = stableId.StartsWith("bgm.", StringComparison.Ordinal)
                    ? AudioClipLoadType.Streaming
                    : stableId == AudioCueIds.VictoryLong ||
                        stableId == AudioCueIds.GameOver
                        ? AudioClipLoadType.CompressedInMemory
                        : AudioClipLoadType.DecompressOnLoad;
                Assert.That(importer.defaultSampleSettings.loadType,
                    Is.EqualTo(expectedLoadType), stableId);
            }
        }

        [Test]
        public void CandidatePrefab_SavesTwoBgmOneLoopingAndSixOneShotSources()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                AudioCandidateAuthoring.PrefabPath);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<AudioRoot>(), Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<AudioSource>(true), Has.Length.EqualTo(9));
            Assert.That(prefab.transform.Find("BgmSourceA"), Is.Not.Null);
            Assert.That(prefab.transform.Find("BgmSourceB"), Is.Not.Null);
            Assert.That(prefab.transform.Find("LoopingSfxSource"), Is.Not.Null);
            for (var index = 1; index <= 6; index++)
            {
                Assert.That(prefab.transform.Find("OneShotSource" + index), Is.Not.Null);
            }

            Assert.That(prefab.GetComponentsInChildren<AudioSource>(true)
                .All(source => !source.playOnAwake), Is.True);

            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(
                AudioCandidateAuthoring.MixerPath);
            Assert.That(mixer, Is.Not.Null);
            Assert.That(mixer.FindMatchingGroups("Music"), Has.Length.EqualTo(1));
            Assert.That(mixer.FindMatchingGroups("SFX"), Has.Length.EqualTo(1));
            Assert.That(mixer.GetFloat("MasterVolume", out _), Is.True);
            Assert.That(mixer.GetFloat("MusicVolume", out _), Is.True);
            Assert.That(mixer.GetFloat("SfxVolume", out _), Is.True);

            var root = prefab.GetComponent<AudioRoot>();
            var serializedRoot = new SerializedObject(root);
            Assert.That(serializedRoot.FindProperty("mixer").objectReferenceValue,
                Is.SameAs(mixer));
            Assert.That(serializedRoot.FindProperty("masterMixerGroup").objectReferenceValue,
                Is.Not.Null);
            Assert.That(serializedRoot.FindProperty("musicMixerGroup").objectReferenceValue,
                Is.Not.Null);
            Assert.That(serializedRoot.FindProperty("sfxMixerGroup").objectReferenceValue,
                Is.Not.Null);
        }

        private AudioCueCatalog CreateCatalog(IEnumerable<AudioCueDefinition> entries)
        {
            var catalog = AudioCueCatalog.CreateTransient(entries);
            _created.Add(catalog);
            return catalog;
        }

        private AudioClip CreateClip(string name)
        {
            var clip = AudioClip.Create(name, 4410, 1, 44100, false);
            _created.Add(clip);
            return clip;
        }
    }
}
