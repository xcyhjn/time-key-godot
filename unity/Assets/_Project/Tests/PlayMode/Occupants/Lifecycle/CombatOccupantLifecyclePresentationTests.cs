using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TimeKey.Domain;
using TimeKey.Presentation.Occupants;
using TimeKey.Presentation.Presenters;
using UnityEngine;

namespace TimeKey.Tests.PlayMode.Occupants.Lifecycle
{
    public sealed class CombatOccupantLifecyclePresentationTests
    {
        [Test]
        public void TowerLifecycle_UpdatesHealthThenRemovesViewStatusAndNotifiesOnce()
        {
            var rig = new PresenterRig();
            try
            {
                var tower = Snapshot(
                    "tower-01",
                    new HexCoord(0, 0),
                    hp: 100,
                    poisonStacks: 2,
                    kind: "tower",
                    creationId: "tower");
                var view = rig.Register(tower);
                var removed = new List<string>();
                rig.Presenter.OccupantRemoved += removed.Add;

                Assert.That(view.Hp, Is.EqualTo(100));
                Assert.That(rig.Presenter.GetPoisonStatus(tower.RuntimeId).Stacks, Is.EqualTo(2));

                rig.Presenter.ApplyLifecycleChanges(new[]
                {
                    Change(
                        sequence: 1,
                        tower,
                        TurnLifecyclePhase.RunningBuildingBehaviors,
                        LifecycleMutationReason.TowerDecay,
                        afterHp: 50,
                        afterPoisonStacks: 2)
                });

                Assert.That(view.Hp, Is.EqualTo(50));
                Assert.That(rig.Presenter.OccupantViewCount, Is.EqualTo(1));

                var removal = Change(
                    sequence: 2,
                    tower.RuntimeId,
                    tower.Coordinate,
                    TurnLifecyclePhase.RunningBuildingBehaviors,
                    LifecycleMutationReason.TowerDecay,
                    beforeHp: 50,
                    beforePoisonStacks: 2,
                    afterHp: 0,
                    afterPoisonStacks: 0,
                    remove: true,
                    deathPolicy: LifecycleDeathPolicy.Remove);
                rig.Presenter.ApplyLifecycleChanges(new[] { removal });
                rig.Presenter.ApplyLifecycleChanges(new[] { removal });

                Assert.That(rig.Presenter.OccupantViewCount, Is.Zero);
                Assert.That(rig.Presenter.PoisonStatusCount, Is.Zero);
                Assert.That(rig.Presenter.GetView(tower.RuntimeId), Is.Null);
                Assert.That(view.gameObject.activeSelf, Is.False);
                Assert.That(removed, Is.EqualTo(new[] { tower.RuntimeId }));
            }
            finally
            {
                rig.Dispose();
            }
        }

        [Test]
        public void PoisonLifecycle_UsesTypedAfterValuesForOldSourceAndNewInfection()
        {
            var rig = new PresenterRig();
            try
            {
                var oldSource = Snapshot(
                    "enemy-old",
                    new HexCoord(0, 0),
                    hp: 100,
                    poisonStacks: 2);
                var newInfection = Snapshot(
                    "enemy-new",
                    new HexCoord(1, 0),
                    hp: 100,
                    poisonStacks: 0);
                var oldView = rig.Register(oldSource);
                var newView = rig.Register(newInfection);

                rig.Presenter.ApplyLifecycleChanges(new[]
                {
                    Change(
                        1,
                        oldSource.RuntimeId,
                        oldSource.Coordinate,
                        TurnLifecyclePhase.ProcessingTurnStartStatuses,
                        LifecycleMutationReason.PoisonTurnStart,
                        beforeHp: 100,
                        beforePoisonStacks: 2,
                        afterHp: 80,
                        afterPoisonStacks: 1),
                    Change(
                        1,
                        newInfection.RuntimeId,
                        newInfection.Coordinate,
                        TurnLifecyclePhase.ProcessingTurnStartStatuses,
                        LifecycleMutationReason.PoisonTurnStart,
                        beforeHp: 100,
                        beforePoisonStacks: 0,
                        afterHp: 100,
                        afterPoisonStacks: 1)
                });

                Assert.That(oldView.Hp, Is.EqualTo(80));
                Assert.That(newView.Hp, Is.EqualTo(100));
                Assert.That(rig.Presenter.GetPoisonStatus(oldSource.RuntimeId).Stacks, Is.EqualTo(1));
                Assert.That(rig.Presenter.GetPoisonStatus(newInfection.RuntimeId).Stacks, Is.EqualTo(1));
                Assert.That(rig.Presenter.PoisonStatusCount, Is.EqualTo(2));

                rig.Presenter.ApplyLifecycleChanges(new[]
                {
                    Change(
                        2,
                        oldSource.RuntimeId,
                        oldSource.Coordinate,
                        TurnLifecyclePhase.ProcessingTurnStartStatuses,
                        LifecycleMutationReason.PoisonTurnStart,
                        beforeHp: 80,
                        beforePoisonStacks: 1,
                        afterHp: 70,
                        afterPoisonStacks: 0)
                });

                Assert.That(oldView.Hp, Is.EqualTo(70));
                Assert.That(rig.Presenter.GetPoisonStatus(oldSource.RuntimeId), Is.Null);
                Assert.That(rig.Presenter.PoisonStatusCount, Is.EqualTo(1));
            }
            finally
            {
                rig.Dispose();
            }
        }

        [Test]
        public void PoisonDeathRemainBroken_KeepsViewAndClearsStatus()
        {
            var rig = new PresenterRig();
            try
            {
                var enemy = Snapshot(
                    "enemy-broken",
                    new HexCoord(0, 0),
                    hp: 10,
                    poisonStacks: 3);
                var view = rig.Register(enemy);

                rig.Presenter.ApplyLifecycleChanges(new[]
                {
                    Change(
                        1,
                        enemy.RuntimeId,
                        enemy.Coordinate,
                        TurnLifecyclePhase.ProcessingTurnStartStatuses,
                        LifecycleMutationReason.PoisonTurnStart,
                        beforeHp: 10,
                        beforePoisonStacks: 3,
                        afterHp: 0,
                        afterPoisonStacks: 0,
                        remove: false,
                        deathPolicy: LifecycleDeathPolicy.RemainBroken)
                });

                Assert.That(view.Hp, Is.Zero);
                Assert.That(view.gameObject.activeSelf, Is.True);
                Assert.That(rig.Presenter.OccupantViewCount, Is.EqualTo(1));
                Assert.That(rig.Presenter.PoisonStatusCount, Is.Zero);
            }
            finally
            {
                rig.Dispose();
            }
        }

        [Test]
        public void StaleLifecycleResult_CannotRegressHealthOrStatus()
        {
            var rig = new PresenterRig();
            try
            {
                var occupant = Snapshot(
                    "enemy-sequence",
                    new HexCoord(0, 0),
                    hp: 100,
                    poisonStacks: 2);
                var view = rig.Register(occupant);
                rig.Presenter.ApplyLifecycleChanges(new[]
                {
                    Change(
                        2,
                        occupant.RuntimeId,
                        occupant.Coordinate,
                        TurnLifecyclePhase.ProcessingTurnStartStatuses,
                        LifecycleMutationReason.PoisonTurnStart,
                        beforeHp: 100,
                        beforePoisonStacks: 2,
                        afterHp: 80,
                        afterPoisonStacks: 1)
                });
                rig.Presenter.ApplyLifecycleChanges(new[]
                {
                    Change(
                        1,
                        occupant.RuntimeId,
                        occupant.Coordinate,
                        TurnLifecyclePhase.ProcessingTurnStartStatuses,
                        LifecycleMutationReason.PoisonTurnStart,
                        beforeHp: 100,
                        beforePoisonStacks: 2,
                        afterHp: 90,
                        afterPoisonStacks: 2)
                });

                Assert.That(view.Hp, Is.EqualTo(80));
                Assert.That(rig.Presenter.GetPoisonStatus(occupant.RuntimeId).Stacks, Is.EqualTo(1));
            }
            finally
            {
                rig.Dispose();
            }
        }

        [Test]
        public void DisableEnableAndRebind_DoesNotDuplicateViewsOrStatuses()
        {
            var rig = new PresenterRig();
            try
            {
                var occupant = Snapshot(
                    "enemy-rebind",
                    new HexCoord(0, 0),
                    hp: 100,
                    poisonStacks: 2);
                var view = rig.Register(occupant);

                rig.Presenter.enabled = false;
                rig.Presenter.enabled = true;
                rig.Presenter.RegisterExisting(occupant, view);
                rig.Presenter.RegisterExisting(occupant, view);

                Assert.That(rig.Presenter.OccupantViewCount, Is.EqualTo(1));
                Assert.That(rig.Presenter.PoisonStatusCount, Is.EqualTo(1));
                Assert.That(view.StatusAnchor.childCount, Is.EqualTo(1));
            }
            finally
            {
                rig.Dispose();
            }
        }

        [TestCase(0f)]
        [TestCase(90f)]
        [TestCase(180f)]
        [TestCase(270f)]
        public void BoardYaw_KeepsOccupantAndStatusOnTheSameAnchor(float yaw)
        {
            var rig = new PresenterRig();
            try
            {
                var occupant = Snapshot(
                    "tower-yaw",
                    new HexCoord(0, 0),
                    hp: 100,
                    poisonStacks: 1,
                    kind: "tower",
                    creationId: "tower");
                var view = rig.Register(occupant);
                var localPosition = view.transform.localPosition;

                rig.Root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

                Assert.That(view.transform.parent, Is.SameAs(rig.OccupantAnchor));
                Assert.That(view.transform.localPosition, Is.EqualTo(localPosition));
                Assert.That(
                    rig.Presenter.GetPoisonStatus(occupant.RuntimeId).transform.parent,
                    Is.SameAs(view.StatusAnchor));
            }
            finally
            {
                rig.Dispose();
            }
        }

        private static CombatOccupantSnapshot Snapshot(
            string runtimeId,
            HexCoord coordinate,
            int hp,
            int poisonStacks,
            string kind = "entity",
            string creationId = null)
        {
            var state = new CombatSliceState(
                runtimeId,
                731,
                new CombatBoardState(),
                new[]
                {
                    new CombatOccupantState(
                        runtimeId,
                        coordinate,
                        kind,
                        CombatAttitude.Enemy,
                        hp,
                        100,
                        poisonStacks,
                        true,
                        true,
                        creationId)
                });
            Assert.That(state.TryGetOccupant(runtimeId, out var snapshot), Is.True);
            return snapshot;
        }

        private static LifecycleOccupantChangeResult Change(
            long sequence,
            CombatOccupantSnapshot before,
            TurnLifecyclePhase phase,
            LifecycleMutationReason reason,
            int afterHp,
            int afterPoisonStacks)
        {
            return Change(
                sequence,
                before.RuntimeId,
                before.Coordinate,
                phase,
                reason,
                before.Hp,
                before.PoisonStacks,
                afterHp,
                afterPoisonStacks,
                remove: false,
                deathPolicy: null);
        }

        private static LifecycleOccupantChangeResult Change(
            long sequence,
            string runtimeId,
            HexCoord coordinate,
            TurnLifecyclePhase phase,
            LifecycleMutationReason reason,
            int beforeHp,
            int beforePoisonStacks,
            int afterHp,
            int afterPoisonStacks,
            bool remove = false,
            LifecycleDeathPolicy? deathPolicy = null)
        {
            return new LifecycleOccupantChangeResult(
                sequence,
                new LifecycleOccupantMutation(
                    runtimeId,
                    coordinate,
                    phase,
                    reason,
                    beforeHp,
                    beforePoisonStacks,
                    afterHp,
                    afterPoisonStacks,
                    remove,
                    deathPolicy));
        }

        private sealed class PresenterRig : IDisposable
        {
            public PresenterRig()
            {
                Root = new GameObject("CombatOccupantLifecycleRig");
                var cameraObject = new GameObject("SceneCamera");
                cameraObject.transform.SetParent(Root.transform, false);
                var sceneCamera = cameraObject.AddComponent<Camera>();

                var column = new GameObject("Column");
                column.transform.SetParent(Root.transform, false);
                OccupantAnchor = new GameObject("OccupantAnchor").transform;
                OccupantAnchor.SetParent(column.transform, false);

                var poisonPrefab = CreatePoisonStatusPrefab();
                poisonPrefab.transform.SetParent(Root.transform, false);

                Presenter = Root.AddComponent<CombatOccupantPresenter>();
                SetField(Presenter, "sceneCamera", sceneCamera);
                SetField(Presenter, "poisonStatusPrefab", poisonPrefab);
            }

            public GameObject Root { get; }

            public Transform OccupantAnchor { get; }

            public CombatOccupantPresenter Presenter { get; }

            public CombatOccupantView Register(CombatOccupantSnapshot occupant)
            {
                var occupantObject = new GameObject("OccupantFixture-" + occupant.RuntimeId);
                occupantObject.transform.SetParent(OccupantAnchor, false);
                var statusAnchor = new GameObject("StatusAnchor").transform;
                statusAnchor.SetParent(occupantObject.transform, false);
                var view = occupantObject.AddComponent<CombatOccupantView>();
                SetField(view, "statusAnchor", statusAnchor);
                Presenter.RegisterExisting(occupant, view);
                return view;
            }

            public void Dispose()
            {
                if (Root != null)
                {
                    UnityEngine.Object.DestroyImmediate(Root);
                }
            }

            private static GameObject CreatePoisonStatusPrefab()
            {
                var prefab = new GameObject("PoisonStatusFixture");
                var icon = prefab.AddComponent<SpriteRenderer>();
                var labelObject = new GameObject("Stacks");
                labelObject.transform.SetParent(prefab.transform, false);
                var label = labelObject.AddComponent<TextMesh>();
                var view = prefab.AddComponent<PoisonStatusView>();
                SetField(view, "icon", icon);
                SetField(view, "stackLabel", label);
                return prefab;
            }

            private static void SetField(object target, string name, object value)
            {
                var field = target.GetType().GetField(
                    name,
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(field, Is.Not.Null, "Missing test field " + name + ".");
                field.SetValue(target, value);
            }
        }
    }
}
