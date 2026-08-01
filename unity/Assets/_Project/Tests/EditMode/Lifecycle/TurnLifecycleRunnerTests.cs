using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TimeKey.Domain;

namespace TimeKey.Tests.EditMode.Lifecycle
{
    public sealed class TurnLifecycleRunnerTests
    {
        [Test]
        public void InitialStart_UsesSharedStartTurnTailAndUnlocksAfterIntentRefresh()
        {
            var ports = new RecordingPorts();
            var runner = CreateRunner(ports);

            var result = runner.Run(TurnLifecycleRequest.InitialStart());

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Sequence, Is.EqualTo(1));
            Assert.That(result.PhaseHistory, Is.EqualTo(new[]
            {
                TurnLifecyclePhase.ProcessingTurnStartStatuses,
                TurnLifecyclePhase.RefreshingEnemyIntents,
                TurnLifecyclePhase.PlayerReady
            }));
            Assert.That(ports.Events, Is.EqualTo(new[] { "status", "hook", "intent" }));
            Assert.That(ports.HookCalls, Is.EqualTo(1));
            Assert.That(ports.ObservedInputLocks, Is.All.True);
            Assert.That(runner.CurrentPhase, Is.EqualTo(TurnLifecyclePhase.PlayerReady));
            Assert.That(runner.IsInputLocked, Is.False);
        }

        [Test]
        public void EndTurn_EntersFrozenPhasesOnceAndResolvesMixedActionsInGridOrder()
        {
            var ports = new RecordingPorts();
            var runner = CreateRunner(ports);
            var plan = new TimelineActionPlan(new[]
            {
                Entry("column-two", TimelineActorKind.Player, 2, 0),
                Entry("row-two", TimelineActorKind.Enemy, 1, 2),
                new TimelineActionPlanEntry(
                    new TimelineActionIdentity("row-zero"),
                    TimelineActorKind.Player,
                    "same-display",
                    new TimelineCell(1, 0),
                    new[] { new TimelineCell(1, 0), new TimelineCell(1, 1) })
            });

            var result = runner.Run(TurnLifecycleRequest.EndTurn(plan));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.PhaseHistory, Is.EqualTo(new[]
            {
                TurnLifecyclePhase.EndTurnRequested,
                TurnLifecyclePhase.ResolvingTimeline,
                TurnLifecyclePhase.RunningBuildingBehaviors,
                TurnLifecyclePhase.ClearingTimeline,
                TurnLifecyclePhase.ProcessingTurnStartStatuses,
                TurnLifecyclePhase.RefreshingEnemyIntents,
                TurnLifecyclePhase.PlayerReady
            }));
            Assert.That(
                result.PhaseHistory.Distinct().Count(),
                Is.EqualTo(result.PhaseHistory.Count));
            Assert.That(ports.Events, Is.EqualTo(new[]
            {
                "timeline:row-zero",
                "timeline:row-two",
                "timeline:column-two",
                "building",
                "clear",
                "status",
                "hook",
                "intent"
            }));
            Assert.That(
                result.CompletedActionIds.Select(actionId => actionId.Value),
                Is.EqualTo(new[] { "row-zero", "row-two", "column-two" }));
            Assert.That(ports.ObservedInputLocks, Is.All.True);
        }

        [Test]
        public void ReentrantRequest_ReturnsBusyWithoutExecutingAnotherCycle()
        {
            var ports = new RecordingPorts();
            var runner = CreateRunner(ports);
            TurnLifecycleResult reentrant = null;
            ports.ResolveOverride = (context, action) =>
            {
                reentrant = runner.Run(TurnLifecycleRequest.InitialStart());
                return TurnLifecycleStepResult.Successful;
            };

            var outer = runner.Run(TurnLifecycleRequest.EndTurn(
                new TimelineActionPlan(new[] { Entry("only", TimelineActorKind.Player, 0, 0) })));

            Assert.That(outer.Succeeded, Is.True);
            Assert.That(reentrant, Is.Not.Null);
            Assert.That(reentrant.Failure, Is.EqualTo(TurnLifecycleFailure.Busy));
            Assert.That(reentrant.Sequence, Is.EqualTo(1));
            Assert.That(reentrant.PhaseBefore, Is.EqualTo(TurnLifecyclePhase.ResolvingTimeline));
            Assert.That(reentrant.PhaseHistory, Is.Empty);
            Assert.That(ports.Events.Count(item => item == "timeline:only"), Is.EqualTo(1));
            Assert.That(ports.Events.Count(item => item == "status"), Is.EqualTo(1));
        }

        [Test]
        public void TwoCompletedCycles_IncrementSequenceAndRunEachPhaseOncePerCycle()
        {
            var ports = new RecordingPorts();
            var runner = CreateRunner(ports);
            var request = TurnLifecycleRequest.EndTurn(
                new TimelineActionPlan(Array.Empty<TimelineActionPlanEntry>()));

            var first = runner.Run(request);
            var second = runner.Run(request);

            Assert.That(first.Succeeded, Is.True);
            Assert.That(second.Succeeded, Is.True);
            Assert.That(first.Sequence, Is.EqualTo(1));
            Assert.That(second.Sequence, Is.EqualTo(2));
            Assert.That(ports.Events.Count(item => item == "building"), Is.EqualTo(2));
            Assert.That(ports.Events.Count(item => item == "clear"), Is.EqualTo(2));
            Assert.That(ports.Events.Count(item => item == "status"), Is.EqualTo(2));
            Assert.That(ports.Events.Count(item => item == "hook"), Is.EqualTo(2));
            Assert.That(ports.Events.Count(item => item == "intent"), Is.EqualTo(2));
            Assert.That(runner.Sequence, Is.EqualTo(2));
            Assert.That(runner.IsInputLocked, Is.False);
        }

        [Test]
        public void ProcessorFailure_IsStructuredAndCompletedActionsAreNotReplayed()
        {
            var ports = new RecordingPorts
            {
                ResolveOverride = (context, action) =>
                    action.ActionId == new TimelineActionIdentity("second")
                        ? TurnLifecycleStepResult.Failed("fixture failure")
                        : TurnLifecycleStepResult.Successful
            };
            var runner = CreateRunner(ports);
            var request = TurnLifecycleRequest.EndTurn(new TimelineActionPlan(new[]
            {
                Entry("first", TimelineActorKind.Player, 0, 0),
                Entry("second", TimelineActorKind.Enemy, 1, 0)
            }));

            var failed = runner.Run(request);
            var eventCountAfterFailure = ports.Events.Count;
            var repeated = runner.Run(request);

            Assert.That(failed.Failure, Is.EqualTo(TurnLifecycleFailure.TimelineActionFailed));
            Assert.That(failed.FailureReason, Is.EqualTo("fixture failure"));
            Assert.That(failed.PhaseAfter, Is.EqualTo(TurnLifecyclePhase.ResolvingTimeline));
            Assert.That(failed.IsInputLockedAfter, Is.True);
            Assert.That(failed.CompletedActionIds, Is.EqualTo(new[]
            {
                new TimelineActionIdentity("first")
            }));
            Assert.That(ports.Events, Is.EqualTo(new[]
            {
                "timeline:first",
                "timeline:second"
            }));
            Assert.That(repeated.Failure, Is.EqualTo(TurnLifecycleFailure.Faulted));
            Assert.That(ports.Events, Has.Count.EqualTo(eventCountAfterFailure));
            Assert.That(runner.IsFaulted, Is.True);
            Assert.That(runner.IsInputLocked, Is.True);
        }

        [Test]
        public void DuplicateActionIdentity_FailsPreflightWithoutLockingOrCallingProcessors()
        {
            var ports = new RecordingPorts();
            var runner = CreateRunner(ports);
            var plan = new TimelineActionPlan(new[]
            {
                Entry("duplicate", TimelineActorKind.Player, 0, 0),
                Entry("duplicate", TimelineActorKind.Enemy, 1, 0)
            });

            var result = runner.Run(TurnLifecycleRequest.EndTurn(plan));

            Assert.That(result.Failure, Is.EqualTo(TurnLifecycleFailure.InvalidTimelinePlan));
            Assert.That(result.TimelinePlanFailure,
                Is.EqualTo(TimelinePlanFailure.DuplicateActionIdentity));
            Assert.That(result.Sequence, Is.Zero);
            Assert.That(result.PhaseHistory, Is.Empty);
            Assert.That(ports.Events, Is.Empty);
            Assert.That(runner.CurrentPhase, Is.EqualTo(TurnLifecyclePhase.PlayerReady));
            Assert.That(runner.IsInputLocked, Is.False);
        }

        private static TurnLifecycleRunner CreateRunner(RecordingPorts ports)
        {
            var runner = new TurnLifecycleRunner(ports, ports, ports, ports, ports, ports);
            ports.Runner = runner;
            return runner;
        }

        private static TimelineActionPlanEntry Entry(
            string actionId,
            TimelineActorKind actorKind,
            int x,
            int y)
        {
            var cell = new TimelineCell(x, y);
            return new TimelineActionPlanEntry(
                new TimelineActionIdentity(actionId),
                actorKind,
                "same-display",
                cell,
                new[] { cell });
        }

        private sealed class RecordingPorts :
            ITimelineActionProcessor,
            IBuildingBehaviorProcessor,
            ITimelineClearingProcessor,
            ITurnStartStatusProcessor,
            INextTurnHook,
            IEnemyIntentRefresher
        {
            public List<string> Events { get; } = new List<string>();

            public List<bool> ObservedInputLocks { get; } = new List<bool>();

            public TurnLifecycleRunner Runner { get; set; }

            public int HookCalls { get; private set; }

            public Func<TurnLifecycleContext, TimelineActionPlanEntry, TurnLifecycleStepResult>
                ResolveOverride { get; set; }

            public TurnLifecycleStepResult Resolve(
                TurnLifecycleContext context,
                TimelineActionPlanEntry action)
            {
                Observe("timeline:" + action.ActionId.Value);
                return ResolveOverride == null
                    ? TurnLifecycleStepResult.Successful
                    : ResolveOverride(context, action);
            }

            public TurnLifecycleStepResult Run(TurnLifecycleContext context)
            {
                Observe("building");
                return TurnLifecycleStepResult.Successful;
            }

            TurnLifecycleStepResult INextTurnHook.Run(TurnLifecycleContext context)
            {
                HookCalls++;
                Observe("hook");
                return TurnLifecycleStepResult.Successful;
            }

            public TurnLifecycleStepResult Clear(TurnLifecycleContext context)
            {
                Observe("clear");
                return TurnLifecycleStepResult.Successful;
            }

            public TurnLifecycleStepResult Process(TurnLifecycleContext context)
            {
                Observe("status");
                return TurnLifecycleStepResult.Successful;
            }

            public TurnLifecycleStepResult Refresh(TurnLifecycleContext context)
            {
                Observe("intent");
                return TurnLifecycleStepResult.Successful;
            }

            private void Observe(string value)
            {
                Events.Add(value);
                ObservedInputLocks.Add(Runner.IsInputLocked);
            }
        }
    }
}
