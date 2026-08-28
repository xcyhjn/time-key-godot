using System.Collections;
using System.Linq;
using NUnit.Framework;
using TimeKey.Presentation.Audio;
using UnityEngine;
using UnityEngine.TestTools;

namespace TimeKey.Tests.PlayMode.Audio
{
    public sealed class AudioRootPlayModeTests
    {
        private GameObject _rootObject;
        private AudioRoot _root;
        private AudioCueCatalog _catalog;
        private AudioClip _clip;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _rootObject = new GameObject("AudioRootPlayModeTest");
            _root = _rootObject.AddComponent<AudioRoot>();
            _clip = AudioClip.Create("test", 44100, 1, 44100, false);
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
            SetField(_root, "crossfadeDuration", 0.05f);
            Assert.That(_root.Bind(), Is.True);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            AudioRoot.Unbind(_root);
            if (_rootObject != null)
            {
                Object.Destroy(_rootObject);
            }

            if (_catalog != null)
            {
                Object.Destroy(_catalog);
            }

            if (_clip != null)
            {
                Object.Destroy(_clip);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator BgmSwitch_CrossfadesAndSuppressesRepeatedCue()
        {
            Assert.That(_root.PlayBgm("bgm.main_menu"), Is.True);
            yield return null;
            Assert.That(_root.PlayBgm("bgm.main_menu"), Is.True);
            Assert.That(_root.Diagnostics.Any(item => item.Code == "DuplicateCueSuppressed"), Is.True);

            Assert.That(_root.PlayBgm("bgm.battle"), Is.True);
            yield return new WaitForSecondsRealtime(0.15f);

            Assert.That(_root.ActiveBgmCueId, Is.EqualTo("bgm.battle"));
            Assert.That(_root.ActiveBgmSourceCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator OneShotPool_ReportsExhaustionAndReusesOldestSource()
        {
            Assert.That(_root.PlaySfx("sfx.draw_card"), Is.True);
            Assert.That(_root.PlaySfx("sfx.draw_card"), Is.True);
            yield return null;

            Assert.That(_root.Diagnostics.Any(item => item.Code == "OneShotPoolExhausted"), Is.True);
        }

        [UnityTest]
        public IEnumerator LoopingSfx_CanStopImmediatelyAndDisableClearsBinding()
        {
            Assert.That(_root.PlayLoopingSfx("sfx.victory_short"), Is.True);
            Assert.That(_root.IsLoopingSfxActive, Is.True);
            _root.StopLoopingSfx(0f);
            Assert.That(_root.IsLoopingSfxActive, Is.False);

            _rootObject.SetActive(false);
            yield return null;
            Assert.That(AudioRoot.Active, Is.Null);
            Assert.That(_root.IsBound, Is.False);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
