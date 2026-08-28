using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimeKey.Presentation.Audio
{
    public enum AudioBusRoute
    {
        Master,
        Music,
        Sfx
    }

    public enum AudioDiagnosticSeverity
    {
        Info,
        Warning,
        Error
    }

    public sealed class AudioDiagnostic
    {
        public AudioDiagnostic(
            string code,
            AudioDiagnosticSeverity severity,
            string message,
            string cueId = null)
        {
            Code = code ?? string.Empty;
            Severity = severity;
            Message = message ?? string.Empty;
            CueId = cueId ?? string.Empty;
        }

        public string Code { get; }

        public AudioDiagnosticSeverity Severity { get; }

        public string Message { get; }

        public string CueId { get; }

        public bool IsError => Severity == AudioDiagnosticSeverity.Error;

        public override string ToString()
        {
            return string.IsNullOrEmpty(CueId)
                ? Code + ": " + Message
                : Code + " [" + CueId + "]: " + Message;
        }
    }

    [Serializable]
    public sealed class AudioCueDefinition
    {
        [SerializeField] private string stableId = string.Empty;
        [SerializeField] private AudioClip clip = null;
        [SerializeField] private AudioBusRoute route = AudioBusRoute.Sfx;
        [SerializeField] private bool loop;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private int priority;
        [SerializeField, Min(0.01f)] private float pitchMin = 1f;
        [SerializeField, Min(0.01f)] private float pitchMax = 1f;

        public AudioCueDefinition(
            string stableId,
            AudioClip clip,
            AudioBusRoute route,
            bool loop = false,
            float volume = 1f,
            int priority = 0,
            float pitchMin = 1f,
            float pitchMax = 1f)
        {
            this.stableId = stableId ?? string.Empty;
            this.clip = clip;
            this.route = route;
            this.loop = loop;
            this.volume = Mathf.Clamp01(volume);
            this.priority = priority;
            this.pitchMin = Mathf.Max(0.01f, pitchMin);
            this.pitchMax = Mathf.Max(this.pitchMin, pitchMax);
        }

        public string StableId => stableId ?? string.Empty;

        public AudioClip Clip => clip;

        public AudioBusRoute Route => route;

        public bool Loop => loop;

        public float Volume => Mathf.Clamp01(volume);

        public int Priority => priority;

        public float PitchMin => Mathf.Max(0.01f, pitchMin);

        public float PitchMax => Mathf.Max(PitchMin, pitchMax);

        public bool HasPitchVariation => !Mathf.Approximately(PitchMin, PitchMax);
    }

    public static class AudioCueIds
    {
        public const string MainMenuBgm = "bgm.main_menu";
        public const string BattleBgm = "bgm.battle";
        public const string BattleBossBgm = "bgm.battle_boss";
        public const string ClockTick = "sfx.clock_tick";
        public const string ChooseRole = "sfx.choose_role";
        public const string WalkDim = "sfx.walk_dim";
        public const string DrawCard = "sfx.draw_card";
        public const string CardShuffle = "sfx.card_shuffle";
        public const string ConfirmTimeline = "sfx.confirm_timeline";
        public const string TileDamage = "sfx.tile_damage";
        public const string VictoryShort = "sfx.victory_short";
        public const string VictoryLong = "sfx.victory_long";
        public const string GameOver = "sfx.game_over";

        private static readonly string[] StableIds =
        {
            MainMenuBgm,
            BattleBgm,
            BattleBossBgm,
            ClockTick,
            ChooseRole,
            WalkDim,
            DrawCard,
            CardShuffle,
            ConfirmTimeline,
            TileDamage,
            VictoryShort,
            VictoryLong,
            GameOver
        };

        public static IReadOnlyList<string> All => StableIds;
    }

    [CreateAssetMenu(
        fileName = "Wave04AudioCueCatalog",
        menuName = "TimeKey/Audio Cue Catalog")]
    public sealed class AudioCueCatalog : ScriptableObject
    {
        [SerializeField] private List<AudioCueDefinition> cues =
            new List<AudioCueDefinition>();

        public IReadOnlyList<AudioCueDefinition> Cues => cues;

        public static AudioCueCatalog CreateTransient(
            IEnumerable<AudioCueDefinition> entries)
        {
            var catalog = CreateInstance<AudioCueCatalog>();
            catalog.cues = entries == null
                ? new List<AudioCueDefinition>()
                : new List<AudioCueDefinition>(entries);
            return catalog;
        }

        public bool TryGet(string stableId, out AudioCueDefinition cue)
        {
            if (string.IsNullOrWhiteSpace(stableId))
            {
                cue = null;
                return false;
            }

            for (var index = 0; index < cues.Count; index++)
            {
                var candidate = cues[index];
                if (candidate != null && candidate.StableId == stableId)
                {
                    cue = candidate;
                    return true;
                }
            }

            cue = null;
            return false;
        }

        public IReadOnlyList<AudioDiagnostic> Validate()
        {
            var diagnostics = new List<AudioDiagnostic>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < cues.Count; index++)
            {
                var cue = cues[index];
                if (cue == null)
                {
                    diagnostics.Add(new AudioDiagnostic(
                        "NullCue",
                        AudioDiagnosticSeverity.Error,
                        "Catalog contains a null cue entry."));
                    continue;
                }

                var id = cue.StableId;
                if (string.IsNullOrWhiteSpace(id))
                {
                    diagnostics.Add(new AudioDiagnostic(
                        "EmptyStableId",
                        AudioDiagnosticSeverity.Error,
                        "Audio cue stable id cannot be empty."));
                    continue;
                }

                if (!seen.Add(id))
                {
                    diagnostics.Add(new AudioDiagnostic(
                        "DuplicateStableId",
                        AudioDiagnosticSeverity.Error,
                        "Audio cue stable id is duplicated.",
                        id));
                }

                if (id.StartsWith("bgm.", StringComparison.Ordinal) &&
                    cue.Route != AudioBusRoute.Music)
                {
                    diagnostics.Add(new AudioDiagnostic(
                        "BgmRouteMismatch",
                        AudioDiagnosticSeverity.Error,
                        "BGM cue must route to Music.",
                        id));
                }

                if (id.StartsWith("sfx.", StringComparison.Ordinal) &&
                    cue.Route == AudioBusRoute.Music)
                {
                    diagnostics.Add(new AudioDiagnostic(
                        "SfxRouteMismatch",
                        AudioDiagnosticSeverity.Error,
                        "SFX cue cannot route to Music.",
                        id));
                }
            }

            return diagnostics;
        }
    }
}
