using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using TimeKey.Application.SceneFlow;

namespace TimeKey.Tests.EditMode.SceneFlow
{
    public sealed class SceneFlowCoordinatorTests
    {
        [Test]
        public async Task TransitionAsync_ExecutesFrozenPhaseOrderAndCommitsTarget()
        {
            var effects = new RecordingEffects();
            var coordinator = new SceneFlowCoordinator(SceneId.MainMenu, effects);

            var result = await coordinator.TransitionAsync(Request(1, "one"));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(coordinator.CurrentScene, Is.EqualTo(SceneId.OutOfBattleShell));
            Assert.That(coordinator.CurrentPhase, Is.EqualTo(SceneTransitionPhase.Idle));
            Assert.That(effects.Phases, Is.EqualTo(new[]
            {
                SceneTransitionPhase.InputLocked,
                SceneTransitionPhase.Covering,
                SceneTransitionPhase.LoadingTarget,
                SceneTransitionPhase.ActivatingTarget,
                SceneTransitionPhase.BindingPayload,
                SceneTransitionPhase.WaitingForFirstRenderableFrame,
                SceneTransitionPhase.UnloadingSource,
                SceneTransitionPhase.Revealing,
                SceneTransitionPhase.InputUnlocked
            }));
        }

        [TestCase(SceneTransitionPhase.LoadingTarget)]
        [TestCase(SceneTransitionPhase.ActivatingTarget)]
        [TestCase(SceneTransitionPhase.BindingPayload)]
        [TestCase(SceneTransitionPhase.WaitingForFirstRenderableFrame)]
        [TestCase(SceneTransitionPhase.UnloadingSource)]
        public async Task TransitionAsync_EffectFailureRollsBackAndUnlocks(
            SceneTransitionPhase failedPhase)
        {
            var effects = new RecordingEffects(failedPhase);
            var coordinator = new SceneFlowCoordinator(SceneId.MainMenu, effects);

            var result = await coordinator.TransitionAsync(Request(1, "failure"));

            Assert.That(result.Failure, Is.EqualTo(SceneTransitionFailure.EffectFailed));
            Assert.That(result.FailedPhase, Is.EqualTo(failedPhase));
            Assert.That(result.ActiveScene, Is.EqualTo(SceneId.MainMenu));
            Assert.That(result.IsInputLocked, Is.False);
            Assert.That(result.PhaseHistory, Does.Contain(SceneTransitionPhase.RollingBackTarget));
            Assert.That(result.PhaseHistory, Does.Contain(SceneTransitionPhase.RestoringSource));
            Assert.That(result.PhaseHistory[^1], Is.EqualTo(SceneTransitionPhase.InputUnlocked));
        }

        [TestCase(SceneTransitionPhase.Revealing)]
        [TestCase(SceneTransitionPhase.InputUnlocked)]
        public async Task TransitionAsync_PostCommitFailureKeepsTargetAndOnlyRecoversCoverAndInput(
            SceneTransitionPhase failedPhase)
        {
            var effects = new RecordingEffects(failedPhase);
            var coordinator = new SceneFlowCoordinator(SceneId.MainMenu, effects);

            var result = await coordinator.TransitionAsync(Request(1, "post-commit"));

            Assert.That(result.Failure, Is.EqualTo(SceneTransitionFailure.EffectFailed));
            Assert.That(result.ActiveScene, Is.EqualTo(SceneId.OutOfBattleShell));
            Assert.That(coordinator.CurrentScene, Is.EqualTo(SceneId.OutOfBattleShell));
            Assert.That(result.PhaseHistory,
                Has.No.Member(SceneTransitionPhase.RollingBackTarget));
            Assert.That(result.PhaseHistory,
                Has.No.Member(SceneTransitionPhase.RestoringSource));
            Assert.That(result.IsInputLocked, Is.False);
        }

        [Test]
        public async Task TransitionAsync_SameSequenceAndFingerprintIsIdempotent()
        {
            var effects = new RecordingEffects();
            var coordinator = new SceneFlowCoordinator(SceneId.MainMenu, effects);
            var request = Request(1, "same");

            var first = await coordinator.TransitionAsync(request);
            var repeated = await coordinator.TransitionAsync(request);

            Assert.That(repeated, Is.SameAs(first));
            Assert.That(effects.Phases.Count, Is.EqualTo(9));
        }

        [Test]
        public async Task TransitionAsync_SameSequenceDifferentFingerprintConflicts()
        {
            var effects = new RecordingEffects();
            var coordinator = new SceneFlowCoordinator(SceneId.MainMenu, effects);
            await coordinator.TransitionAsync(Request(2, "first"));

            var conflict = await coordinator.TransitionAsync(Request(2, "second"));

            Assert.That(conflict.Failure, Is.EqualTo(SceneTransitionFailure.SequenceConflict));
        }

        [Test]
        public async Task TransitionAsync_RejectsStaleAndInvalidPayload()
        {
            var effects = new RecordingEffects();
            var coordinator = new SceneFlowCoordinator(SceneId.MainMenu, effects);
            await coordinator.TransitionAsync(Request(2, "latest"));

            var stale = await coordinator.TransitionAsync(Request(
                1,
                "stale",
                SceneId.OutOfBattleShell,
                SceneId.MainMenu));
            var invalidPayload = await new SceneFlowCoordinator(
                SceneId.MainMenu,
                new RecordingEffects()).TransitionAsync(
                new SceneTransitionRequest(
                    1,
                    "bad-payload",
                    SceneId.MainMenu,
                    SceneId.OutOfBattleShell,
                    new EmptySceneTransitionPayload(SceneId.Combat)));

            Assert.That(stale.Failure, Is.EqualTo(SceneTransitionFailure.Stale));
            Assert.That(invalidPayload.Failure, Is.EqualTo(SceneTransitionFailure.InvalidPayload));
        }

        [Test]
        public async Task TransitionAsync_RejectsEmptyPayloadThatBypassesCombatBoundary()
        {
            var coordinator = new SceneFlowCoordinator(
                SceneId.OutOfBattleShell,
                new RecordingEffects());
            var request = new SceneTransitionRequest(
                1,
                "empty-combat",
                SceneId.OutOfBattleShell,
                SceneId.Combat,
                new EmptySceneTransitionPayload(SceneId.Combat));

            var result = await coordinator.TransitionAsync(request);

            Assert.That(result.Failure, Is.EqualTo(SceneTransitionFailure.InvalidPayload));
            Assert.That(coordinator.CurrentScene, Is.EqualTo(SceneId.OutOfBattleShell));
        }

        [Test]
        public async Task TransitionAsync_ConcurrentDifferentRequestReturnsBusy()
        {
            var effects = new BlockingEffects();
            var coordinator = new SceneFlowCoordinator(SceneId.MainMenu, effects);
            var first = coordinator.TransitionAsync(Request(1, "first"));
            await effects.Started.Task;

            var busy = await coordinator.TransitionAsync(Request(2, "busy"));
            effects.Release.TrySetResult(true);
            var completed = await first;

            Assert.That(busy.Failure, Is.EqualTo(SceneTransitionFailure.Busy));
            Assert.That(completed.Succeeded, Is.True);
        }

        [Test]
        public async Task TransitionAsync_CancellationStillRestoresAndUnlocks()
        {
            var effects = new CancellingEffects();
            var coordinator = new SceneFlowCoordinator(SceneId.MainMenu, effects);

            var result = await coordinator.TransitionAsync(Request(1, "cancel"));

            Assert.That(result.Failure, Is.EqualTo(SceneTransitionFailure.Cancelled));
            Assert.That(result.ActiveScene, Is.EqualTo(SceneId.MainMenu));
            Assert.That(result.IsInputLocked, Is.False);
        }

        private static SceneTransitionRequest Request(
            long sequence,
            string correlation,
            SceneId source = SceneId.MainMenu,
            SceneId target = SceneId.OutOfBattleShell)
        {
            return new SceneTransitionRequest(
                sequence,
                correlation,
                source,
                target,
                new EmptySceneTransitionPayload(target));
        }

        private sealed class RecordingEffects : ISceneFlowEffects
        {
            private readonly SceneTransitionPhase? _failedPhase;
            private bool _failureInjected;

            public RecordingEffects(SceneTransitionPhase? failedPhase = null)
            {
                _failedPhase = failedPhase;
            }

            public List<SceneTransitionPhase> Phases { get; } =
                new List<SceneTransitionPhase>();

            public Task<SceneFlowEffectResult> ExecuteAsync(
                SceneTransitionPhase phase,
                SceneTransitionRequest request,
                CancellationToken cancellationToken)
            {
                Phases.Add(phase);
                if (phase == _failedPhase && !_failureInjected)
                {
                    _failureInjected = true;
                    return Task.FromResult(SceneFlowEffectResult.Failed("injected"));
                }

                return Task.FromResult(SceneFlowEffectResult.Success());
            }
        }

        private sealed class BlockingEffects : ISceneFlowEffects
        {
            public TaskCompletionSource<bool> Started { get; } =
                new TaskCompletionSource<bool>();

            public TaskCompletionSource<bool> Release { get; } =
                new TaskCompletionSource<bool>();

            public async Task<SceneFlowEffectResult> ExecuteAsync(
                SceneTransitionPhase phase,
                SceneTransitionRequest request,
                CancellationToken cancellationToken)
            {
                if (phase == SceneTransitionPhase.InputLocked)
                {
                    Started.TrySetResult(true);
                    await Release.Task;
                }

                return SceneFlowEffectResult.Success();
            }
        }

        private sealed class CancellingEffects : ISceneFlowEffects
        {
            public Task<SceneFlowEffectResult> ExecuteAsync(
                SceneTransitionPhase phase,
                SceneTransitionRequest request,
                CancellationToken cancellationToken)
            {
                if (phase == SceneTransitionPhase.LoadingTarget)
                {
                    throw new OperationCanceledException();
                }

                return Task.FromResult(SceneFlowEffectResult.Success());
            }
        }
    }
}
