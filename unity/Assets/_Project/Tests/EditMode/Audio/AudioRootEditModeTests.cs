using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TimeKey.Presentation.Audio;
using UnityEngine;

namespace TimeKey.Tests.EditMode.Audio
{
    public sealed class AudioRootEditModeTests
    {
        private GameObject _rootObject;
        private AudioRoot _root;
        private AudioCueCatalog _catalog;
        private AudioClip _clip;

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(AudioRoot.MasterVolumePreferenceKey);
            PlayerPrefs.DeleteKey(AudioRoot.MusicVolumePreferenceKey);
            PlayerPrefs.DeleteKey(AudioRoot.SfxVolumePreferenceKey);
            _rootObject = new GameObject("AudioRootTest");
            _root = _rootObject.AddComponent<AudioRoot>();
            _clip = AudioClip.Create("test", 4410, 1, 44100, false);
            _catalog = AudioCueCatalog.CreateTransient(new[]
            {
                new AudioCueDefinition("bgm.main_menu", _clip, AudioBusRoute.Music, true),
                new AudioCueDefinition("bgm.battle", _clip, AudioBusRoute.Music, true),
                new AudioCueDefinition("sfx.draw_card", _clip, AudioBusRoute.Sfx),
                new AudioCueDefinition("sfx.victory_short", _clip, AudioBusRoute.Sfx, true)
            });

            SetField(_root, "cueCatalog", _catalog);
            SetField(_root, "bgmSourceA", _rootObject.AddComponent<AudioSource>());
            SetField(_root, "bgmSourceB", _rootObject.AddComponent<AudioSource>());
            SetField(_root, "loopingSfxSource", _rootObject.AddComponent<AudioSource>());
            SetField(_root, "oneShotPool", new[] { _rootObject.AddComponent<AudioSource>() });
        }

        [TearDown]
        public void TearDown()
        {
            AudioRoot.Unbind(_root);
            if (_rootObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_rootObject);
            }

            if (_catalog != null)
            {
                UnityEngine.Object.DestroyImmediate(_catalog);
            }

            if (_clip != null)
            {
                UnityEngine.Object.DestroyImmediate(_clip);
            }

            PlayerPrefs.DeleteKey(AudioRoot.MasterVolumePreferenceKey);
            PlayerPrefs.DeleteKey(AudioRoot.MusicVolumePreferenceKey);
            PlayerPrefs.DeleteKey(AudioRoot.SfxVolumePreferenceKey);
        }

        [Test]
        public void BindAndRebind_AreIdempotentAndDoNotCreateSources()
        {
            var sourceCount = _rootObject.GetComponentsInChildren<AudioSource>(true).Length;

            Assert.That(_root.Bind(), Is.True);
            Assert.That(_root.Bind(), Is.True);
            Assert.That(_root.Rebind(), Is.True);

            Assert.That(AudioRoot.Active, Is.SameAs(_root));
            Assert.That(_root.IsBound, Is.True);
            Assert.That(_rootObject.GetComponentsInChildren<AudioSource>(true), Has.Length.EqualTo(sourceCount));
            Assert.That(_root.Diagnostics.Any(item => item.Code == "DuplicateBind"), Is.False);
        }

        [Test]
        public void MissingClip_ProducesStructuredDiagnosticWithoutThrowing()
        {
            var missingCatalog = AudioCueCatalog.CreateTransient(new[]
            {
                new AudioCueDefinition("sfx.draw_card", null, AudioBusRoute.Sfx)
            });
            SetField(_root, "cueCatalog", missingCatalog);
            Assert.That(_root.Bind(), Is.True);

            Assert.That(_root.PlaySfx("sfx.draw_card"), Is.False);

            Assert.That(_root.Diagnostics.Last().Code, Is.EqualTo("MissingClip"));
            UnityEngine.Object.DestroyImmediate(missingCatalog);
        }

        [Test]
        public void VolumeInterface_ClampsAndPersistsMasterMusicAndSfx()
        {
            Assert.That(_root.Bind(), Is.True);

            _root.ApplyVolumeSettings(1.5f, -0.5f, 0.25f, true);
            var settings = _root.GetVolumeSettings();

            Assert.That(settings.Master, Is.EqualTo(1f));
            Assert.That(settings.Music, Is.EqualTo(0f));
            Assert.That(settings.Sfx, Is.EqualTo(0.25f));
            Assert.That(PlayerPrefs.GetFloat(AudioRoot.MasterVolumePreferenceKey), Is.EqualTo(1f));
            Assert.That(PlayerPrefs.GetFloat(AudioRoot.MusicVolumePreferenceKey), Is.EqualTo(0f));
            Assert.That(PlayerPrefs.GetFloat(AudioRoot.SfxVolumePreferenceKey), Is.EqualTo(0.25f));
        }

        [Test]
        public void PauseAndUnpause_TrackTheRuntimePauseState()
        {
            Assert.That(_root.Bind(), Is.True);

            _root.SetPaused(true);
            Assert.That(_root.IsPaused, Is.True);
            _root.SetPaused(false);
            Assert.That(_root.IsPaused, Is.False);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            target.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
