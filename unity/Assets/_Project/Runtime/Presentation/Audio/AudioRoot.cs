using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace TimeKey.Presentation.Audio
{
    public readonly struct AudioVolumeSettings
    {
        public AudioVolumeSettings(float master, float music, float sfx)
        {
            Master = Mathf.Clamp01(master);
            Music = Mathf.Clamp01(music);
            Sfx = Mathf.Clamp01(sfx);
        }

        public float Master { get; }

        public float Music { get; }

        public float Sfx { get; }

        public static AudioVolumeSettings Default => new AudioVolumeSettings(1f, 1f, 1f);
    }

    [DisallowMultipleComponent]
    public sealed class AudioRoot : MonoBehaviour
    {
        public const string MasterVolumePreferenceKey = "timekey.settings.master-volume";
        public const string MusicVolumePreferenceKey = "timekey.settings.music-volume";
        public const string SfxVolumePreferenceKey = "timekey.settings.sfx-volume";

        private const float SilentLinearVolume = 0.0001f;
        private const int DiagnosticLimit = 128;

        [SerializeField] private AudioCueCatalog cueCatalog = null;
        [SerializeField] private AudioMixer mixer = null;
        [SerializeField] private AudioMixerGroup masterMixerGroup = null;
        [SerializeField] private AudioMixerGroup musicMixerGroup = null;
        [SerializeField] private AudioMixerGroup sfxMixerGroup = null;
        [SerializeField] private AudioSource bgmSourceA = null;
        [SerializeField] private AudioSource bgmSourceB = null;
        [SerializeField] private AudioSource loopingSfxSource = null;
        [SerializeField] private AudioSource[] oneShotPool = Array.Empty<AudioSource>();
        [SerializeField, Min(0f)] private float crossfadeDuration = 1f;
        [SerializeField, Min(0)] private int deterministicPitchSeed = 731;
        [SerializeField] private string masterVolumeParameter = "MasterVolume";
        [SerializeField] private string musicVolumeParameter = "MusicVolume";
        [SerializeField] private string sfxVolumeParameter = "SfxVolume";
        [SerializeField] private bool bindOnEnable = true;

        private readonly List<AudioDiagnostic> _diagnostics =
            new List<AudioDiagnostic>();
        private AudioVolumeSettings _volumeSettings = AudioVolumeSettings.Default;
        private System.Random _pitchRandom;
        private long[] _oneShotSequences = Array.Empty<long>();
        private float[] _oneShotBusyUntil = Array.Empty<float>();
        private long _oneShotSequence;
        private int _activeBgmIndex = -1;
        private string _activeBgmCueId = string.Empty;
        private AudioSource _fadeOutSource;
        private AudioSource _fadeInSource;
        private float _fadeOutStartVolume;
        private float _fadeInTargetVolume;
        private float _fadeElapsed;
        private float _fadeDuration;
        private bool _clearBgmAfterFade;
        private bool _loopingSfxActive;
        private bool _loopStopPending;
        private float _loopFadeElapsed;
        private float _loopFadeDuration;
        private float _loopFadeStartVolume;
        private bool _paused;
        private bool _bound;

        public static AudioRoot Active { get; private set; }

        public bool IsBound => _bound;

        public bool IsPaused => _paused;

        public bool IsLoopingSfxActive => _loopingSfxActive;

        public string ActiveBgmCueId => _activeBgmCueId;

        public IReadOnlyList<AudioDiagnostic> Diagnostics => _diagnostics;

        public int BgmSwitchCount { get; private set; }

        public int DuplicateCueSuppressionCount { get; private set; }

        public int PoolExhaustionCount { get; private set; }

        public int PeakOneShotSourceCount { get; private set; }

        public int ActiveBgmSourceCount
        {
            get
            {
                var count = 0;
                if (bgmSourceA != null && bgmSourceA.isPlaying)
                {
                    count++;
                }

                if (bgmSourceB != null && bgmSourceB.isPlaying)
                {
                    count++;
                }

                return count;
            }
        }

        public int ActiveOneShotSourceCount
        {
            get
            {
                var count = 0;
                var now = Time.realtimeSinceStartup;
                for (var index = 0; index < _oneShotBusyUntil.Length; index++)
                {
                    if (_oneShotBusyUntil[index] > now)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        private void OnEnable()
        {
            if (bindOnEnable)
            {
                Bind();
            }
        }

        private void OnDisable()
        {
            Unbind(this);
        }

        private void OnDestroy()
        {
            Unbind(this);
        }

        private void Update()
        {
            if (!_bound || _paused)
            {
                return;
            }

            UpdateBgmFade(Time.unscaledDeltaTime);
            UpdateLoopingSfxFade(Time.unscaledDeltaTime);
            var activeOneShots = ActiveOneShotSourceCount;
            if (activeOneShots > PeakOneShotSourceCount)
            {
                PeakOneShotSourceCount = activeOneShots;
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            SetPaused(pauseStatus);
        }

        public bool Bind()
        {
            if (_bound && Active == this)
            {
                return true;
            }

            if (Active != null && Active != this)
            {
                AddDiagnostic(
                    "DuplicateAudioRoot",
                    AudioDiagnosticSeverity.Error,
                    "Another AudioRoot is already bound.");
                return false;
            }

            if (!DependenciesAssigned())
            {
                return false;
            }

            var catalogDiagnostics = cueCatalog.Validate();
            var hasCatalogError = false;
            for (var index = 0; index < catalogDiagnostics.Count; index++)
            {
                var diagnostic = catalogDiagnostics[index];
                AddDiagnostic(
                    diagnostic.Code,
                    diagnostic.Severity,
                    diagnostic.Message,
                    diagnostic.CueId);
                hasCatalogError |= diagnostic.IsError;
            }

            if (hasCatalogError)
            {
                return false;
            }

            Active = this;
            _bound = true;
            _paused = false;
            _pitchRandom = new System.Random(deterministicPitchSeed);
            _oneShotSequences = new long[oneShotPool.Length];
            _oneShotBusyUntil = new float[oneShotPool.Length];
            _volumeSettings = LoadVolumeSettings();
            ConfigureSources();
            ApplyMixerVolumes();
            return true;
        }

        public bool Rebind()
        {
            ReleaseBinding();
            return Bind();
        }

        public static void Unbind(AudioRoot root)
        {
            if (root == null)
            {
                return;
            }

            root.ReleaseBinding();
        }

        public bool PlayBgm(string stableId)
        {
            if (!TryGetPlayableCue(stableId, AudioBusRoute.Music, out var cue))
            {
                return false;
            }

            var activeSource = GetBgmSource(_activeBgmIndex);
            if (_activeBgmCueId == cue.StableId &&
                activeSource != null && activeSource.clip == cue.Clip)
            {
                DuplicateCueSuppressionCount++;
                AddDiagnostic(
                    "DuplicateCueSuppressed",
                    AudioDiagnosticSeverity.Info,
                    "Repeated BGM cue was suppressed.",
                    cue.StableId);
                return true;
            }

            var nextIndex = _activeBgmIndex == 0 ? 1 : 0;
            var nextSource = GetBgmSource(nextIndex);
            if (nextSource == null)
            {
                AddDiagnostic(
                    "MissingSource",
                    AudioDiagnosticSeverity.Error,
                    "Configured BGM source is missing.",
                    cue.StableId);
                return false;
            }

            nextSource.Stop();
            nextSource.clip = cue.Clip;
            nextSource.outputAudioMixerGroup = ResolveMixerGroup(cue.Route);
            nextSource.loop = cue.Loop;
            nextSource.priority = Mathf.Clamp(cue.Priority, 0, 256);
            nextSource.pitch = NextPitch(cue);
            var targetVolume = CueLinearVolume(cue);
            nextSource.volume = 0f;
            nextSource.Play();

            StartBgmFade(activeSource, nextSource, targetVolume, crossfadeDuration, false);
            _activeBgmIndex = nextIndex;
            _activeBgmCueId = cue.StableId;
            BgmSwitchCount++;
            return true;
        }

        public void StopBgm(float fadeDuration = -1f)
        {
            var activeSource = GetBgmSource(_activeBgmIndex);
            if (activeSource == null || activeSource.clip == null)
            {
                ClearBgmState();
                return;
            }

            var duration = fadeDuration < 0f ? crossfadeDuration : fadeDuration;
            StartBgmFade(activeSource, null, 0f, duration, true);
        }

        public bool PlaySfx(string stableId)
        {
            if (!TryGetPlayableCue(stableId, AudioBusRoute.Sfx, out var cue))
            {
                return false;
            }

            var index = FindOneShotSourceIndex();
            if (index < 0)
            {
                return false;
            }

            var source = oneShotPool[index];
            var now = Time.realtimeSinceStartup;
            if (_oneShotBusyUntil[index] > now)
            {
                PoolExhaustionCount++;
                AddDiagnostic(
                    "OneShotPoolExhausted",
                    AudioDiagnosticSeverity.Warning,
                    "One-shot pool was exhausted; the oldest source was reused.",
                    cue.StableId);
                source.Stop();
            }

            var pitch = NextPitch(cue);
            source.clip = cue.Clip;
            source.outputAudioMixerGroup = ResolveMixerGroup(cue.Route);
            source.loop = false;
            source.priority = Mathf.Clamp(cue.Priority, 0, 256);
            source.pitch = pitch;
            source.volume = CueLinearVolume(cue);
            source.Play();
            _oneShotSequence++;
            _oneShotSequences[index] = _oneShotSequence;
            _oneShotBusyUntil[index] = now + (cue.Clip.length / Mathf.Max(0.01f, pitch));
            var activeCount = ActiveOneShotSourceCount;
            if (activeCount > PeakOneShotSourceCount)
            {
                PeakOneShotSourceCount = activeCount;
            }

            return true;
        }

        public bool PlayLoopingSfx(string stableId)
        {
            if (!TryGetPlayableCue(stableId, AudioBusRoute.Sfx, out var cue))
            {
                return false;
            }

            _loopStopPending = false;
            loopingSfxSource.Stop();
            loopingSfxSource.clip = cue.Clip;
            loopingSfxSource.outputAudioMixerGroup = ResolveMixerGroup(cue.Route);
            loopingSfxSource.loop = cue.Loop;
            loopingSfxSource.priority = Mathf.Clamp(cue.Priority, 0, 256);
            loopingSfxSource.pitch = NextPitch(cue);
            loopingSfxSource.volume = CueLinearVolume(cue);
            loopingSfxSource.Play();
            _loopingSfxActive = true;
            return true;
        }

        public void StopLoopingSfx(float fadeDuration = 0.5f)
        {
            if (loopingSfxSource == null)
            {
                _loopingSfxActive = false;
                _loopStopPending = false;
                return;
            }

            _loopingSfxActive = false;
            if (fadeDuration <= 0f || loopingSfxSource.clip == null)
            {
                StopAndClear(loopingSfxSource);
                _loopStopPending = false;
                return;
            }

            _loopStopPending = true;
            _loopFadeElapsed = 0f;
            _loopFadeDuration = fadeDuration;
            _loopFadeStartVolume = loopingSfxSource.volume;
        }

        public void SetPaused(bool paused)
        {
            if (_paused == paused)
            {
                return;
            }

            _paused = paused;
            SetSourcePaused(bgmSourceA, paused);
            SetSourcePaused(bgmSourceB, paused);
            SetSourcePaused(loopingSfxSource, paused);
            if (oneShotPool == null)
            {
                return;
            }

            for (var index = 0; index < oneShotPool.Length; index++)
            {
                SetSourcePaused(oneShotPool[index], paused);
            }
        }

        public void ApplyVolumeSettings(
            float master,
            float music,
            float sfx,
            bool persist)
        {
            _volumeSettings = new AudioVolumeSettings(master, music, sfx);
            ApplyMixerVolumes();
            RefreshSourceVolumes();
            if (!persist)
            {
                return;
            }

            PlayerPrefs.SetFloat(MasterVolumePreferenceKey, _volumeSettings.Master);
            PlayerPrefs.SetFloat(MusicVolumePreferenceKey, _volumeSettings.Music);
            PlayerPrefs.SetFloat(SfxVolumePreferenceKey, _volumeSettings.Sfx);
            PlayerPrefs.Save();
        }

        public AudioVolumeSettings GetVolumeSettings()
        {
            return _volumeSettings;
        }

        public AudioVolumeSettings LoadVolumeSettings()
        {
            return new AudioVolumeSettings(
                PlayerPrefs.GetFloat(MasterVolumePreferenceKey, 1f),
                PlayerPrefs.GetFloat(MusicVolumePreferenceKey, 1f),
                PlayerPrefs.GetFloat(SfxVolumePreferenceKey, 1f));
        }

        public AudioMixerGroup ResolveMixerGroup(AudioBusRoute route)
        {
            switch (route)
            {
                case AudioBusRoute.Master:
                    return masterMixerGroup;
                case AudioBusRoute.Music:
                    return musicMixerGroup;
                case AudioBusRoute.Sfx:
                    return sfxMixerGroup;
                default:
                    throw new ArgumentOutOfRangeException(nameof(route), route, null);
            }
        }

        public void StopAll()
        {
            StopAndClear(bgmSourceA);
            StopAndClear(bgmSourceB);
            StopAndClear(loopingSfxSource);
            if (oneShotPool != null)
            {
                for (var index = 0; index < oneShotPool.Length; index++)
                {
                    StopAndClear(oneShotPool[index]);
                }
            }

            Array.Clear(_oneShotSequences, 0, _oneShotSequences.Length);
            Array.Clear(_oneShotBusyUntil, 0, _oneShotBusyUntil.Length);
            ClearBgmState();
            _loopingSfxActive = false;
            _loopStopPending = false;
            _paused = false;
        }

        private bool DependenciesAssigned()
        {
            var valid = true;
            if (cueCatalog == null)
            {
                AddDiagnostic(
                    "MissingCatalog",
                    AudioDiagnosticSeverity.Error,
                    "AudioRoot requires an AudioCueCatalog.");
                valid = false;
            }

            if (bgmSourceA == null || bgmSourceB == null)
            {
                AddDiagnostic(
                    "MissingBgmSources",
                    AudioDiagnosticSeverity.Error,
                    "AudioRoot requires two saved BGM sources.");
                valid = false;
            }

            if (loopingSfxSource == null)
            {
                AddDiagnostic(
                    "MissingLoopingSource",
                    AudioDiagnosticSeverity.Error,
                    "AudioRoot requires one saved looping SFX source.");
                valid = false;
            }

            if (oneShotPool == null || oneShotPool.Length == 0)
            {
                AddDiagnostic(
                    "MissingOneShotPool",
                    AudioDiagnosticSeverity.Error,
                    "AudioRoot requires a finite saved one-shot pool.");
                valid = false;
            }
            else
            {
                for (var index = 0; index < oneShotPool.Length; index++)
                {
                    if (oneShotPool[index] != null)
                    {
                        continue;
                    }

                    AddDiagnostic(
                        "MissingOneShotSource",
                        AudioDiagnosticSeverity.Error,
                        "One-shot pool contains a missing source.");
                    valid = false;
                }
            }

            return valid;
        }

        private bool TryGetPlayableCue(
            string stableId,
            AudioBusRoute requiredRoute,
            out AudioCueDefinition cue)
        {
            if (!_bound)
            {
                AddDiagnostic(
                    "NotBound",
                    AudioDiagnosticSeverity.Error,
                    "AudioRoot is not bound.",
                    stableId);
                cue = null;
                return false;
            }

            if (!cueCatalog.TryGet(stableId, out cue))
            {
                AddDiagnostic(
                    "UnknownCue",
                    AudioDiagnosticSeverity.Warning,
                    "Audio cue is not present in the catalog.",
                    stableId);
                return false;
            }

            if (cue.Clip == null)
            {
                AddDiagnostic(
                    "MissingClip",
                    AudioDiagnosticSeverity.Warning,
                    "Audio cue has no clip assigned.",
                    stableId);
                return false;
            }

            if (cue.Route != requiredRoute)
            {
                AddDiagnostic(
                    "RouteMismatch",
                    AudioDiagnosticSeverity.Error,
                    "Audio cue route does not match the requested playback channel.",
                    stableId);
                return false;
            }

            return true;
        }

        private void ConfigureSources()
        {
            ConfigureSource(bgmSourceA, musicMixerGroup, true);
            ConfigureSource(bgmSourceB, musicMixerGroup, true);
            ConfigureSource(loopingSfxSource, sfxMixerGroup, true);
            for (var index = 0; index < oneShotPool.Length; index++)
            {
                ConfigureSource(oneShotPool[index], sfxMixerGroup, false);
            }
        }

        private static void ConfigureSource(
            AudioSource source,
            AudioMixerGroup group,
            bool loop)
        {
            source.playOnAwake = false;
            source.outputAudioMixerGroup = group;
            source.loop = loop;
            source.spatialBlend = 0f;
        }

        private void ReleaseBinding()
        {
            StopAll();
            _bound = false;
            if (Active == this)
            {
                Active = null;
            }
        }

        private AudioSource GetBgmSource(int index)
        {
            switch (index)
            {
                case 0:
                    return bgmSourceA;
                case 1:
                    return bgmSourceB;
                default:
                    return null;
            }
        }

        private void StartBgmFade(
            AudioSource fadeOut,
            AudioSource fadeIn,
            float fadeInTarget,
            float duration,
            bool clearAfterFade)
        {
            CompletePendingBgmFade();
            _fadeOutSource = fadeOut;
            _fadeInSource = fadeIn;
            _fadeOutStartVolume = fadeOut == null ? 0f : fadeOut.volume;
            _fadeInTargetVolume = fadeInTarget;
            _fadeElapsed = 0f;
            _fadeDuration = Mathf.Max(0f, duration);
            _clearBgmAfterFade = clearAfterFade;
            if (_fadeDuration <= 0f)
            {
                CompletePendingBgmFade();
            }
        }

        private void UpdateBgmFade(float deltaTime)
        {
            if (_fadeOutSource == null && _fadeInSource == null)
            {
                return;
            }

            _fadeElapsed += Mathf.Max(0f, deltaTime);
            var t = _fadeDuration <= 0f
                ? 1f
                : Mathf.Clamp01(_fadeElapsed / _fadeDuration);
            if (_fadeOutSource != null)
            {
                _fadeOutSource.volume = Mathf.Lerp(_fadeOutStartVolume, 0f, t);
            }

            if (_fadeInSource != null)
            {
                _fadeInSource.volume = Mathf.Lerp(0f, _fadeInTargetVolume, t);
            }

            if (t >= 1f)
            {
                CompletePendingBgmFade();
            }
        }

        private void CompletePendingBgmFade()
        {
            if (_fadeOutSource != null)
            {
                StopAndClear(_fadeOutSource);
            }

            if (_fadeInSource != null)
            {
                _fadeInSource.volume = _fadeInTargetVolume;
            }

            var clearState = _clearBgmAfterFade;
            _fadeOutSource = null;
            _fadeInSource = null;
            _fadeOutStartVolume = 0f;
            _fadeInTargetVolume = 0f;
            _fadeElapsed = 0f;
            _fadeDuration = 0f;
            _clearBgmAfterFade = false;
            if (clearState)
            {
                ClearBgmState();
            }
        }

        private void ClearBgmState()
        {
            _activeBgmIndex = -1;
            _activeBgmCueId = string.Empty;
        }

        private int FindOneShotSourceIndex()
        {
            var now = Time.realtimeSinceStartup;
            var oldestIndex = -1;
            var oldestSequence = long.MaxValue;
            for (var index = 0; index < oneShotPool.Length; index++)
            {
                if (_oneShotBusyUntil[index] <= now)
                {
                    return index;
                }

                if (_oneShotSequences[index] < oldestSequence)
                {
                    oldestSequence = _oneShotSequences[index];
                    oldestIndex = index;
                }
            }

            return oldestIndex;
        }

        private float NextPitch(AudioCueDefinition cue)
        {
            if (!cue.HasPitchVariation)
            {
                return cue.PitchMin;
            }

            var value = _pitchRandom == null ? 0.5 : _pitchRandom.NextDouble();
            return Mathf.Lerp(cue.PitchMin, cue.PitchMax, (float)value);
        }

        private float CueLinearVolume(AudioCueDefinition cue)
        {
            switch (cue.Route)
            {
                case AudioBusRoute.Master:
                    return cue.Volume * _volumeSettings.Master;
                case AudioBusRoute.Music:
                    return cue.Volume * _volumeSettings.Master * _volumeSettings.Music;
                case AudioBusRoute.Sfx:
                    return cue.Volume * _volumeSettings.Master * _volumeSettings.Sfx;
                default:
                    return cue.Volume;
            }
        }

        private void RefreshSourceVolumes()
        {
            RefreshSourceVolume(bgmSourceA);
            RefreshSourceVolume(bgmSourceB);
            RefreshSourceVolume(loopingSfxSource);
            if (oneShotPool == null)
            {
                return;
            }

            for (var index = 0; index < oneShotPool.Length; index++)
            {
                RefreshSourceVolume(oneShotPool[index]);
            }
        }

        private void RefreshSourceVolume(AudioSource source)
        {
            if (source == null || source.clip == null || cueCatalog == null)
            {
                return;
            }

            for (var index = 0; index < cueCatalog.Cues.Count; index++)
            {
                var cue = cueCatalog.Cues[index];
                if (cue != null && cue.Clip == source.clip)
                {
                    source.volume = CueLinearVolume(cue);
                    return;
                }
            }
        }

        private void ApplyMixerVolumes()
        {
            if (mixer == null)
            {
                return;
            }

            ApplyMixerVolume(masterVolumeParameter, _volumeSettings.Master);
            ApplyMixerVolume(musicVolumeParameter, _volumeSettings.Music);
            ApplyMixerVolume(sfxVolumeParameter, _volumeSettings.Sfx);
        }

        private void ApplyMixerVolume(string parameter, float linearVolume)
        {
            if (string.IsNullOrWhiteSpace(parameter))
            {
                return;
            }

            var decibels = linearVolume <= SilentLinearVolume
                ? -80f
                : Mathf.Log10(linearVolume) * 20f;
            if (!mixer.SetFloat(parameter, decibels))
            {
                AddDiagnostic(
                    "MixerParameterUnavailable",
                    AudioDiagnosticSeverity.Warning,
                    "AudioMixer exposed parameter is unavailable: " + parameter);
            }
        }

        private void UpdateLoopingSfxFade(float deltaTime)
        {
            if (!_loopStopPending || loopingSfxSource == null)
            {
                return;
            }

            _loopFadeElapsed += Mathf.Max(0f, deltaTime);
            var t = _loopFadeDuration <= 0f
                ? 1f
                : Mathf.Clamp01(_loopFadeElapsed / _loopFadeDuration);
            loopingSfxSource.volume = Mathf.Lerp(_loopFadeStartVolume, 0f, t);
            if (t < 1f)
            {
                return;
            }

            StopAndClear(loopingSfxSource);
            _loopStopPending = false;
        }

        private static void SetSourcePaused(AudioSource source, bool paused)
        {
            if (source == null || source.clip == null)
            {
                return;
            }

            if (paused)
            {
                source.Pause();
            }
            else
            {
                source.UnPause();
            }
        }

        private static void StopAndClear(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            source.Stop();
            source.clip = null;
            source.volume = 0f;
        }

        private void AddDiagnostic(
            string code,
            AudioDiagnosticSeverity severity,
            string message,
            string cueId = null)
        {
            if (_diagnostics.Count >= DiagnosticLimit)
            {
                _diagnostics.RemoveAt(0);
            }

            _diagnostics.Add(new AudioDiagnostic(code, severity, message, cueId));
        }
    }
}
