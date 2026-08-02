using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using TimeKey.Application;
using TimeKey.Composition;
using TimeKey.Domain;
using TimeKey.Domain.BattleFlow;
using TimeKey.Presentation;
using TimeKey.Presentation.Actions;
using TimeKey.Presentation.BattleFlow;
using TimeKey.Presentation.Bindings;
using TimeKey.Presentation.Localization;
using TimeKey.Presentation.Occupants;
using TimeKey.Presentation.Targeting;
using TimeKey.Presentation.Tooltips;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TimeKey.Editor
{
    public static class VerticalSliceAutomation
    {
        private const string ScenePath = "Assets/_Project/Scenes/VerticalSlice/CombatVerticalSlice.unity";

        [MenuItem("Time Key/Capture Deck Battle Flow Gate D")]
        public static void CaptureDeckBattleFlowGateD()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var evidenceDirectory = GetDeckBattleFlowGateDEvidenceDirectory();
            Directory.CreateDirectory(evidenceDirectory);
            var captures = new List<CaptureStats>();

            var controller = OpenInitializedSlice();
            controller.SetBoardView(32f);
            ValidateSimplifiedChinesePresentation(controller);
            ValidateBattleFlow(
                controller,
                expectedPhase: 1,
                expectedDraw: 7,
                expectedHand: 5,
                expectedDiscard: 0);
            CaptureResponsive(controller, evidenceDirectory, "initial-empty-discard", captures);

            var firstOccupied = PlayRecoverRound(controller);
            ValidateBattleFlow(
                controller,
                expectedPhase: 2,
                expectedDraw: 2,
                expectedHand: 5,
                expectedDiscard: 5);
            if (controller.BattleFlow.Timecoins != 36 - firstOccupied)
            {
                throw new InvalidOperationException("The first formal turn awarded incorrect timecoins.");
            }

            CaptureResponsive(controller, evidenceDirectory, "round-2-draw-discard", captures);

            if (!controller.SelectCard(VerticalSliceController.EarthquakeCardId) ||
                !controller.SelectEarthquakeTarget(new HexCoord(0, 0)) ||
                !controller.PreviewTimelineSelected(0, 0) ||
                !controller.TryPlaceSelected(0, 0))
            {
                throw new InvalidOperationException(
                    "The second-round Earthquake action could not be committed.");
            }

            if (UnityEngine.Object.FindObjectsByType<TimelineActionFrame>(
                    FindObjectsInactive.Exclude).Length < 2)
            {
                throw new InvalidOperationException(
                    "The committed action frame did not survive removal of its hand View.");
            }

            captures.Add(Capture(
                controller,
                evidenceDirectory,
                "round-2-committed-action-frame-1920x1080.png",
                1920,
                1080));
            var secondOccupied = controller.TimelineOccupiedCellCount;
            controller.ResolveTimeline();
            ValidateBattleFlow(
                controller,
                expectedPhase: 3,
                expectedDraw: 7,
                expectedHand: 5,
                expectedDiscard: 0);
            if (controller.LastBattleFlowResult == null ||
                controller.LastBattleFlowResult.DrawResult == null ||
                !controller.LastBattleFlowResult.DrawResult.Shuffle.Occurred ||
                controller.BattleFlow.Timecoins !=
                    (36 - firstOccupied) + (36 - secondOccupied))
            {
                throw new InvalidOperationException(
                    "The second formal turn did not recycle the empty draw pile deterministically.");
            }

            CaptureResponsive(controller, evidenceDirectory, "round-3-post-shuffle-empty-discard", captures);

            controller = OpenInitializedSlice();
            controller.SetBoardView(32f);
            PlayRecoverRound(controller);
            if (!controller.SelectCard(VerticalSliceController.LightingCardId) ||
                !controller.SelectTarget(VerticalSliceController.TargetId) ||
                !controller.TryPlaceSelected(0, 0))
            {
                throw new InvalidOperationException("The Victory action could not be committed.");
            }

            captures.Add(Capture(
                controller,
                evidenceDirectory,
                "victory-committed-action-frame-1920x1080.png",
                1920,
                1080));
            controller.ResolveTimeline();
            if (!controller.BattleFlow.IsInputLocked ||
                controller.BattleFlow.Settlement.Outcome != BattleOutcome.VictorySettlement)
            {
                throw new InvalidOperationException("Victory did not settle and lock combat input.");
            }

            CaptureResponsive(controller, evidenceDirectory, "victory-reward-entry", captures);
            var settlement = UnityEngine.Object.FindAnyObjectByType<BattleSettlementPresenter>();
            if (settlement == null || !settlement.IsRewardVisible)
            {
                throw new InvalidOperationException("The typed Victory reward entry is not visible.");
            }

            settlement.GetComponentInChildren<Button>(true).onClick.Invoke();
            if (!controller.BattleFlow.Settlement.IsRewardClaimed ||
                !controller.CreateBattleReturnBoundary().Succeeded)
            {
                throw new InvalidOperationException(
                    "The Victory reward or typed return boundary did not complete.");
            }

            captures.Add(Capture(
                controller,
                evidenceDirectory,
                "victory-reward-claimed-1920x1080.png",
                1920,
                1080));

            controller = OpenInitializedSlice();
            controller.SetBoardView(32f);
            var defeat = controller.ResolveBattleOutcome(BattleOutcome.Defeat);
            if (!defeat.Succeeded || !controller.BattleFlow.IsInputLocked ||
                controller.BattleFlow.Settlement.RewardEntry != null ||
                !controller.CreateBattleReturnBoundary().Succeeded)
            {
                throw new InvalidOperationException(
                    "Defeat did not lock input without creating a reward.");
            }

            CaptureResponsive(controller, evidenceDirectory, "defeat-no-reward", captures);
            WriteDeckBattleFlowGateDSummary(evidenceDirectory, captures);
            Debug.Log("TIMEKEY_DECK_BATTLE_FLOW_GATE_D_CAPTURE_PASS");
        }

        [MenuItem("Time Key/Build Deck Battle Flow Gate D")]
        public static void BuildDeckBattleFlowGateD()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var evidenceDirectory = GetDeckBattleFlowGateDEvidenceDirectory();
            Directory.CreateDirectory(evidenceDirectory);
            var buildDirectory = Path.GetFullPath(
                Path.Combine(UnityEngine.Application.dataPath, "..", "Builds", "Windows"));
            Directory.CreateDirectory(buildDirectory);
            var playerPath = Path.Combine(buildDirectory, "TimeKeySlice.exe");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = playerPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Deck battle flow Windows Player build failed: " + report.summary.result);
            }

            CopySilverAttribution(buildDirectory);
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"status\": \"passed\",");
            builder.AppendLine("  \"target\": \"StandaloneWindows64\",");
            builder.AppendLine("  \"developmentBuild\": true,");
            builder.AppendLine("  \"font\": \"Silver\",");
            builder.AppendLine("  \"buildResult\": \"" + report.summary.result + "\",");
            builder.AppendLine("  \"buildBytes\": " + report.summary.totalSize + ",");
            builder.AppendLine(
                "  \"playerPath\": \"" + playerPath.Replace("\\", "\\\\") + "\"");
            builder.AppendLine("}");
            File.WriteAllText(
                Path.Combine(evidenceDirectory, "build-summary.json"),
                builder.ToString());
            Debug.Log("TIMEKEY_DECK_BATTLE_FLOW_GATE_D_BUILD_PASS");
        }

        [MenuItem("Time Key/Build Validate Capture Simplified Chinese")]
        public static void BuildValidateCaptureSimplifiedChinese()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var evidenceDirectory = GetSimplifiedChineseEvidenceDirectory();
            Directory.CreateDirectory(evidenceDirectory);
            var captures = new List<CaptureStats>();

            var controller = OpenInitializedSlice();
            controller.SetBoardView(32f);
            controller.CardHandHost.ApplyVisualStateImmediate();
            ValidateSimplifiedChinesePresentation(controller);
            captures.Add(Capture(controller, evidenceDirectory, "initial-1280x720.png", 1280, 720));
            captures.Add(Capture(controller, evidenceDirectory, "initial-1920x1080.png", 1920, 1080));
            captures.Add(Capture(controller, evidenceDirectory, "initial-2560x1080.png", 2560, 1080));

            if (!controller.SelectCard(VerticalSliceController.LightingCardId))
            {
                throw new InvalidOperationException("The lighting card could not be selected for localization evidence.");
            }

            controller.CardHandHost.ApplyVisualStateImmediate();
            ValidateSimplifiedChinesePresentation(controller);
            captures.Add(Capture(controller, evidenceDirectory, "lighting-selected-1280x720.png", 1280, 720));
            if (!controller.SelectTarget(VerticalSliceController.TargetId))
            {
                throw new InvalidOperationException("The lighting target could not be selected for localization evidence.");
            }

            ValidateSimplifiedChinesePresentation(controller);
            captures.Add(Capture(controller, evidenceDirectory, "lighting-targeted-1280x720.png", 1280, 720));
            if (!controller.PreviewTimelineSelected(4, 0))
            {
                throw new InvalidOperationException("The lighting timeline preview could not be shown for localization evidence.");
            }

            ValidateSimplifiedChinesePresentation(controller);
            captures.Add(Capture(controller, evidenceDirectory, "lighting-preview-1920x1080.png", 1920, 1080));
            if (!controller.TryPlaceSelected(4, 0))
            {
                throw new InvalidOperationException("The lighting action could not be committed for localization evidence.");
            }

            controller.ResolveTimeline();
            ValidateSimplifiedChinesePresentation(controller);
            captures.Add(Capture(controller, evidenceDirectory, "lighting-resolved-1920x1080.png", 1920, 1080));

            controller = OpenInitializedSlice();
            controller.SetBoardView(32f);
            if (!controller.SelectCard("wind") || !controller.PreviewTimelineSelected(1, 0))
            {
                throw new InvalidOperationException("The Wind clear preview could not be shown for localization evidence.");
            }

            controller.CardHandHost.ApplyVisualStateImmediate();
            ValidateSimplifiedChinesePresentation(controller);
            captures.Add(Capture(controller, evidenceDirectory, "wind-clear-hit-1280x720.png", 1280, 720));

            var buildDirectory = Path.GetFullPath(
                Path.Combine(UnityEngine.Application.dataPath, "..", "Builds", "Windows"));
            Directory.CreateDirectory(buildDirectory);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = Path.Combine(buildDirectory, "TimeKeySlice.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException("Windows Player localization build failed: " + report.summary.result);
            }

            CopySilverAttribution(buildDirectory);
            WriteSimplifiedChineseSummary(evidenceDirectory, captures, report);
            Debug.Log("TIMEKEY_SIMPLIFIED_CHINESE_HARNESS_PASS");
        }

        [MenuItem("Time Key/Capture Turn Lifecycle Gate B")]
        public static void CaptureTurnLifecycleGateB()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var evidenceDirectory = GetTurnLifecycleGateBEvidenceDirectory();
            Directory.CreateDirectory(evidenceDirectory);
            var captures = new List<CaptureStats>();
            var controller = OpenInitializedSlice();
            controller.SetBoardView(32f);

            if (controller.CardHandHost.CardCount != 7)
            {
                throw new InvalidOperationException("The seven-card hand is incomplete.");
            }

            var middleCardId = controller.CardHandHost.Cards[3].StableId;
            if (!controller.SelectCard(middleCardId))
            {
                throw new InvalidOperationException("The middle card could not be selected.");
            }

            controller.CardHandHost.ApplyVisualStateImmediate();
            var effectFrame = UnityEngine.Object.FindAnyObjectByType<CardEffectFrame>();
            if (effectFrame == null || !effectFrame.IsVisible ||
                effectFrame.DisplayTitle != CombatChineseText.GetCardName(middleCardId))
            {
                throw new InvalidOperationException("The selected card detail frame is incomplete.");
            }

            captures.Add(Capture(
                controller,
                evidenceDirectory,
                "selected-middle-1280x720.png",
                1280,
                720));
            captures.Add(Capture(
                controller,
                evidenceDirectory,
                "selected-middle-1920x1080.png",
                1920,
                1080));
            captures.Add(Capture(
                controller,
                evidenceDirectory,
                "selected-middle-2560x1080.png",
                2560,
                1080));

            controller = OpenInitializedSlice();
            controller.SetBoardView(32f);
            var towerCoordinate = new HexCoord(0, 0);
            if (!controller.SelectCard("tower") ||
                !controller.SelectEarthquakeTarget(towerCoordinate) ||
                !controller.PreviewTimelineSelected(4, 0) ||
                !controller.TryPlaceSelected(4, 0))
            {
                throw new InvalidOperationException("The Tower action could not be committed.");
            }

            effectFrame = UnityEngine.Object.FindAnyObjectByType<CardEffectFrame>();

            var actionFrames = UnityEngine.Object.FindObjectsByType<TimelineActionFrame>(
                FindObjectsInactive.Exclude);
            if (actionFrames.Length != 2 || actionFrames[0].ActionId == actionFrames[1].ActionId)
            {
                throw new InvalidOperationException(
                    "Player and enemy actions are not rendered as two distinct action identities.");
            }

            TimelineActionFrame playerActionFrame = null;
            TimelineActionFrame enemyActionFrame = null;
            for (var index = 0; index < actionFrames.Length; index++)
            {
                if (actionFrames[index].ActorKind == TimelineActorKind.Player)
                {
                    playerActionFrame = actionFrames[index];
                }
                else
                {
                    enemyActionFrame = actionFrames[index];
                }
            }

            if (playerActionFrame == null || enemyActionFrame == null)
            {
                throw new InvalidOperationException("The action frames are missing an actor mapping.");
            }

            var playerCoordinate = playerActionFrame.OccupiedCells[0];
            var playerCell = GameObject.Find(
                string.Format("Slot-{0}-{1}", playerCoordinate.X, playerCoordinate.Y))
                .GetComponent<TimelineCellView>();
            playerCell.OnPointerEnter(null);
            if (!effectFrame.DisplayTitle.StartsWith("玩家行动", StringComparison.Ordinal) ||
                controller.BoardRangePreview.ActiveCoordinates.Count == 0)
            {
                throw new InvalidOperationException(
                    "Player action hover did not map to its card detail and board range.");
            }

            captures.Add(Capture(
                controller,
                evidenceDirectory,
                "player-action-mapping-1920x1080.png",
                1920,
                1080));
            playerCell.OnPointerExit(null);

            var enemyCoordinate = enemyActionFrame.OccupiedCells[0];
            var enemyCell = GameObject.Find(
                string.Format("Slot-{0}-{1}", enemyCoordinate.X, enemyCoordinate.Y))
                .GetComponent<TimelineCellView>();
            enemyCell.OnPointerEnter(null);
            if (!effectFrame.DisplayTitle.StartsWith("敌方意图", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Enemy intent hover did not map to its unsupported-effect detail.");
            }

            captures.Add(Capture(
                controller,
                evidenceDirectory,
                "enemy-intent-unsupported-1920x1080.png",
                1920,
                1080));
            enemyCell.OnPointerExit(null);

            controller.ResolveTimeline();
            var refreshedFrames = UnityEngine.Object.FindObjectsByType<TimelineActionFrame>(
                FindObjectsInactive.Exclude);
            if (refreshedFrames.Length != 1 ||
                refreshedFrames[0].ActorKind != TimelineActorKind.Enemy)
            {
                throw new InvalidOperationException(
                    "Resolved frames were not replaced atomically by the next enemy intent.");
            }

            captures.Add(Capture(
                controller,
                evidenceDirectory,
                "resolved-actions-next-intent-1920x1080.png",
                1920,
                1080));
            WriteTurnLifecycleGateBSummary(evidenceDirectory, captures);
            Debug.Log("TIMEKEY_TURN_LIFECYCLE_GATE_B_CAPTURE_PASS");
        }

        [MenuItem("Time Key/Capture Turn Lifecycle Gate D")]
        public static void CaptureTurnLifecycleGateD()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var evidenceDirectory = GetTurnLifecycleGateDEvidenceDirectory();
            Directory.CreateDirectory(evidenceDirectory);
            var captures = new List<CaptureStats>();
            var controller = OpenInitializedSlice();
            controller.SetBoardView(0f);

            var session = GetPrivateField<CombatApplicationSession>(
                controller,
                "_applicationSession");
            var state = GetPrivateField<CombatSliceState>(controller, "_state");
            var timeline = GetPrivateField<TimelineGrid>(controller, "_timeline");
            var binding = UnityEngine.Object.FindAnyObjectByType<CombatPresentationBinding>();
            if (session == null || state == null || timeline == null || binding == null)
            {
                throw new InvalidOperationException(
                    "The lifecycle visual harness could not resolve the shared runtime state.");
            }

            var towerCoordinate = new HexCoord(0, 0);
            if (!controller.SelectCard("tower") ||
                !controller.SelectEarthquakeTarget(towerCoordinate) ||
                !controller.PreviewTimelineSelected(0, 0) ||
                !controller.TryPlaceSelected(0, 0))
            {
                throw new InvalidOperationException("The Tower action could not be committed.");
            }

            CardDefinition poisonCard = null;
            for (var index = 0; index < session.Cards.Count; index++)
            {
                if (session.Cards[index].StableId == "poison")
                {
                    poisonCard = session.Cards[index];
                    break;
                }
            }

            if (poisonCard == null)
            {
                throw new InvalidOperationException("The Poison card is missing from the session.");
            }

            var targetCoordinate = new HexCoord(1, 0);
            TimelineAction poisonAction = null;
            for (var x = 0; x < timeline.Width && poisonAction == null; x++)
            {
                for (var y = 0; y < timeline.Height; y++)
                {
                    var candidate = new TimelineAction(
                        new TimelineActionIdentity("cycle:1/action:99"),
                        TimelineActorKind.Player,
                        0,
                        null,
                        null,
                        poisonCard.StableId,
                        VerticalSliceController.TargetId,
                        new TimelineCell(x, y),
                        poisonCard.Shape,
                        targetCoordinate,
                        poisonCard.Effects,
                        new[] { targetCoordinate });
                    if (timeline.TryPlace(candidate))
                    {
                        poisonAction = candidate;
                        break;
                    }
                }
            }

            if (poisonAction == null)
            {
                throw new InvalidOperationException(
                    "The Poison action could not share the player/enemy timeline.");
            }

            binding.Refresh(session.Current);
            var committedFrames = UnityEngine.Object.FindObjectsByType<TimelineActionFrame>(
                FindObjectsInactive.Exclude);
            if (committedFrames.Length != 3)
            {
                throw new InvalidOperationException(
                    "Tower, Poison, and enemy intent must render as three action identities.");
            }

            var poisonCell = GameObject.Find(string.Format(
                "Slot-{0}-{1}",
                poisonAction.Origin.X + poisonAction.Shape[0].X,
                poisonAction.Origin.Y + poisonAction.Shape[0].Y))
                .GetComponent<TimelineCellView>();
            poisonCell.OnPointerEnter(null);
            captures.Add(Capture(
                controller,
                evidenceDirectory,
                "cycle-1-committed-tower-poison-intent-1920x1080.png",
                1920,
                1080));
            poisonCell.OnPointerExit(null);

            var first = controller.ResolveTimeline();
            var towerEffect = FindOccupantEffect(first, CardEffectKind.Built);
            var poisonEffect = FindOccupantEffect(first, CardEffectKind.Poison);
            if (towerEffect == null || towerEffect.After == null || towerEffect.After.Hp != 100 ||
                poisonEffect == null || poisonEffect.After == null ||
                poisonEffect.After.PoisonStacks != 2)
            {
                throw new InvalidOperationException(
                    "The first cycle did not preserve the HP100 creation and Poison application results.");
            }

            var towerRuntimeId = towerEffect.After.RuntimeId;
            if (!state.TryGetOccupant(towerRuntimeId, out var towerAfterCycle) ||
                towerAfterCycle.Hp != 50 || towerAfterCycle.PoisonStacks != 1 ||
                state.TargetHp != 0)
            {
                throw new InvalidOperationException(
                    "Tower decay or Poison new-infection isolation is inconsistent after cycle one.");
            }

            var occupantPresenter = binding.OccupantPresenter;
            if (occupantPresenter.GetView(towerRuntimeId) == null ||
                occupantPresenter.GetView(towerRuntimeId).Hp != 50 ||
                occupantPresenter.GetPoisonStatus(towerRuntimeId) == null ||
                occupantPresenter.GetPoisonStatus(towerRuntimeId).Stacks != 1)
            {
                throw new InvalidOperationException(
                    "Tower HP50 and Poison stack 1 were not synchronized to Presentation.");
            }

            foreach (var yaw in new[] { 0f, 90f, 180f, 270f })
            {
                controller.SetBoardView(yaw);
                captures.Add(Capture(
                    controller,
                    evidenceDirectory,
                    string.Format("cycle-1-tower-hp50-poison1-yaw-{0}.png", (int)yaw),
                    1920,
                    1080));
            }

            controller.SetBoardView(0f);
            if (!controller.SelectCard(VerticalSliceController.EarthquakeCardId) ||
                !controller.SelectEarthquakeTarget(new HexCoord(-2, 0)) ||
                !controller.PreviewTimelineSelected(0, 0) ||
                !controller.TryPlaceSelected(0, 0))
            {
                throw new InvalidOperationException(
                    "The second continuous lifecycle could not be committed.");
            }

            controller.ResolveTimeline();
            if (state.TryGetOccupant(towerRuntimeId, out _) ||
                occupantPresenter.GetView(towerRuntimeId) != null ||
                occupantPresenter.GetPoisonStatus(towerRuntimeId) != null ||
                controller.TimelineOccupiedCellCount != 0)
            {
                throw new InvalidOperationException(
                    "The second lifecycle left Tower, status, intent, or timeline residue.");
            }

            captures.Add(Capture(
                controller,
                evidenceDirectory,
                "cycle-2-tower-removed-and-intent-cleared-1920x1080.png",
                1920,
                1080));
            WriteTurnLifecycleGateDSummary(evidenceDirectory, captures);
            Debug.Log("TIMEKEY_TURN_LIFECYCLE_GATE_D_CAPTURE_PASS");
        }

        [MenuItem("Time Key/Build Turn Lifecycle Gate D")]
        public static void BuildTurnLifecycleGateD()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var evidenceDirectory = GetTurnLifecycleGateDEvidenceDirectory();
            Directory.CreateDirectory(evidenceDirectory);
            var buildDirectory = Path.GetFullPath(
                Path.Combine(UnityEngine.Application.dataPath, "..", "Builds", "Windows"));
            Directory.CreateDirectory(buildDirectory);
            var playerPath = Path.Combine(buildDirectory, "TimeKeySlice.exe");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = playerPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Turn lifecycle Windows Player build failed: " + report.summary.result);
            }

            CopySilverAttribution(buildDirectory);
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"status\": \"passed\",");
            builder.AppendLine("  \"target\": \"StandaloneWindows64\",");
            builder.AppendLine("  \"developmentBuild\": true,");
            builder.AppendLine("  \"font\": \"Silver\",");
            builder.AppendLine("  \"silverAttributionCopied\": true,");
            builder.AppendLine("  \"buildResult\": \"" + report.summary.result + "\",");
            builder.AppendLine("  \"buildBytes\": " + report.summary.totalSize + ",");
            builder.AppendLine(
                "  \"playerPath\": \"" + playerPath.Replace("\\", "\\\\") + "\"");
            builder.AppendLine("}");
            File.WriteAllText(
                Path.Combine(evidenceDirectory, "turn-lifecycle-gate-d-build.json"),
                builder.ToString());
            Debug.Log("TIMEKEY_TURN_LIFECYCLE_GATE_D_BUILD_PASS");
        }

        [MenuItem("Time Key/Build, Validate and Capture Combat Board")]
        public static void BuildValidateAndCapture()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException("The vertical slice scene could not be opened.");
            }

            var root = GameObject.Find("VerticalSliceRoot");
            if (root == null)
            {
                throw new InvalidOperationException("VerticalSliceRoot is missing.");
            }

            var controller = root.GetComponent<VerticalSliceController>();
            if (controller == null)
            {
                throw new InvalidOperationException("VerticalSliceController is missing.");
            }

            var composition = root.GetComponent<CombatCompositionRoot>();
            if (composition == null)
            {
                throw new InvalidOperationException("CombatCompositionRoot is missing.");
            }

            composition.Initialize();
            controller.BuildSceneGraph();
            ValidateScene(controller, composition);

            var evidenceDirectory = GetRemainingCardsGateDEvidenceDirectory();
            Directory.CreateDirectory(evidenceDirectory);
            var captures = new List<CaptureStats>();

            controller.SetBoardView(32f);
            controller.CardHandHost.ApplyVisualStateImmediate();
            captures.Add(Capture(controller, evidenceDirectory, "seven-card-hand-1280x720.png", 1280, 720));
            captures.Add(Capture(controller, evidenceDirectory, "seven-card-hand-1920x1080.png", 1920, 1080));
            captures.Add(Capture(controller, evidenceDirectory, "seven-card-hand-2560x1080.png", 2560, 1080));

            if (!controller.SelectCard(VerticalSliceController.LightingCardId))
            {
                throw new InvalidOperationException("The original LIGHTING card could not be selected.");
            }

            captures.Add(Capture(controller, evidenceDirectory, "lighting-selected-1920x1080.png", 1920, 1080));
            if (!controller.SelectTarget(VerticalSliceController.TargetId))
            {
                throw new InvalidOperationException("The lighting target could not be selected.");
            }

            captures.Add(Capture(controller, evidenceDirectory, "lighting-targeted-1920x1080.png", 1920, 1080));
            foreach (var yaw in new[] { 0, 90, 180, 270 })
            {
                controller.SetBoardView(yaw);
                captures.Add(Capture(
                    controller,
                    evidenceDirectory,
                    string.Format("lighting-targeted-yaw-{0:000}-1920x1080.png", yaw),
                    1920,
                    1080));
            }

            controller.SetBoardView(32f);
            if (!controller.PreviewTimelineSelected(0, 0))
            {
                throw new InvalidOperationException("The lighting timeline preview was not legal.");
            }

            captures.Add(Capture(
                controller,
                evidenceDirectory,
                "lighting-timeline-valid-1920x1080.png",
                1920,
                1080));
            if (!controller.TryPlaceSelected(0, 0))
            {
                throw new InvalidOperationException("The lighting action could not be committed.");
            }

            captures.Add(Capture(controller, evidenceDirectory, "lighting-before-1920x1080.png", 1920, 1080));
            var lightingSnapshot = controller.ResolveTimeline();
            if (lightingSnapshot.TargetHpAfter != 0 || !lightingSnapshot.EnemyIntentResolved)
            {
                throw new InvalidOperationException("Lighting did not resolve 10 HP to 0 HP with the enemy intent.");
            }

            captures.Add(Capture(controller, evidenceDirectory, "lighting-after-1920x1080.png", 1920, 1080));

            controller = OpenInitializedSlice();
            controller.SetBoardView(32f);
            var center = new HexCoord(0, 0);
            var centerTopBefore = controller.GetTileColumn(center).TopBounds.max.y;
            var targetYBefore = controller.TargetWorldPosition.y;
            if (!controller.SelectCard(VerticalSliceController.EarthquakeCardId))
            {
                throw new InvalidOperationException("The original EARTHQUAKE card could not be selected.");
            }

            controller.CardHandHost.ApplyVisualStateImmediate();
            captures.Add(Capture(controller, evidenceDirectory, "earthquake-selected-1920x1080.png", 1920, 1080));

            if (!controller.SelectEarthquakeTarget(center))
            {
                throw new InvalidOperationException("The earthquake center hex could not be selected.");
            }

            if (controller.BoardRangePreview.ActiveCoordinates.Count != 7 ||
                controller.BoardRangePreview.MissingCoordinates.Count != 0)
            {
                throw new InvalidOperationException("Earthquake did not project to seven existing hexes.");
            }

            foreach (var yaw in new[] { 0, 90, 180, 270 })
            {
                controller.SetBoardView(yaw);
                captures.Add(Capture(
                    controller,
                    evidenceDirectory,
                    string.Format("earthquake-range-yaw-{0:000}-1920x1080.png", yaw),
                    1920,
                    1080));
            }

            controller.SetBoardView(32f);
            if (controller.PreviewTimelineSelected(11, 0))
            {
                throw new InvalidOperationException("The right-edge two-cell shape was reported as legal.");
            }

            captures.Add(Capture(
                controller,
                evidenceDirectory,
                "earthquake-timeline-invalid-1920x1080.png",
                1920,
                1080));

            if (!controller.PreviewTimelineSelected(0, 0))
            {
                throw new InvalidOperationException("The frozen legal timeline cell was reported as invalid.");
            }

            captures.Add(Capture(
                controller,
                evidenceDirectory,
                "earthquake-timeline-valid-1920x1080.png",
                1920,
                1080));

            if (!controller.TryPlaceSelected(0, 0))
            {
                throw new InvalidOperationException("The frozen interaction path could not be arranged.");
            }

            captures.Add(Capture(controller, evidenceDirectory, "earthquake-before-1920x1080.png", 1920, 1080));

            var snapshot = controller.ResolveTimeline();
            if (snapshot.EffectResults.Count != 7 ||
                snapshot.TargetHpAfter != 10 ||
                !snapshot.EnemyIntentResolved ||
                Math.Abs(controller.GetTileColumn(center).TopBounds.max.y - centerTopBefore - 0.64f) > 0.01f ||
                Math.Abs(controller.TargetWorldPosition.y - targetYBefore - 0.64f) > 0.01f)
            {
                throw new InvalidOperationException("The earthquake resolution does not match the +2 layer contract.");
            }

            controller.SetBoardView(32f);
            captures.Add(Capture(controller, evidenceDirectory, "earthquake-after-1920x1080.png", 1920, 1080));

            foreach (var yaw in new[] { 0, 90, 180, 270 })
            {
                controller.SetBoardView(yaw);
                Physics.SyncTransforms();
                var bounds = controller.GetTileColumn(center).TopBounds;
                var screenPoint = controller.SceneCamera.WorldToScreenPoint(
                    new Vector3(bounds.center.x, bounds.max.y - 0.02f, bounds.center.z));
                if (!controller.TrySelectWorldAtScreenPoint(screenPoint) ||
                    !controller.SelectedTile.HasValue ||
                    controller.SelectedTile.Value != center)
                {
                    throw new InvalidOperationException("Raised center hex selection failed at yaw " + yaw);
                }
            }

            CaptureRemainingCardsFinalYawEvidence(evidenceDirectory, captures);

            var buildDirectory = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", "Builds", "Windows"));
            Directory.CreateDirectory(buildDirectory);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = Path.Combine(buildDirectory, "TimeKeySlice.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException("Windows Player build failed: " + report.summary.result);
            }

            CopySilverAttribution(buildDirectory);
            CaptureRemainingCardsGateA();
            CaptureRemainingCardsGateB();
            CaptureRemainingCardsGateC();
            MergeEvidenceCaptures(GetRemainingCardsGateAEvidenceDirectory(), evidenceDirectory, captures);
            MergeEvidenceCaptures(GetRemainingCardsGateBEvidenceDirectory(), evidenceDirectory, captures);
            MergeEvidenceCaptures(GetRemainingCardsGateCEvidenceDirectory(), evidenceDirectory, captures);
            WriteSummary(evidenceDirectory, captures, report);
            Debug.Log("TIMEKEY_REMAINING_CARDS_GATE_D_HARNESS_PASS");
        }

        [MenuItem("Time Key/Capture Remaining Cards Gate A")]
        public static void CaptureRemainingCardsGateA()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException("The vertical slice scene could not be opened.");
            }

            var root = GameObject.Find("VerticalSliceRoot");
            var controller = root == null ? null : root.GetComponent<VerticalSliceController>();
            var composition = root == null ? null : root.GetComponent<CombatCompositionRoot>();
            if (controller == null || composition == null)
            {
                throw new InvalidOperationException("The vertical slice root is incomplete.");
            }

            composition.Initialize();
            controller.BuildSceneGraph();
            ValidateScene(controller, composition);
            controller.SetBoardView(32f);

            var evidenceDirectory = GetRemainingCardsGateAEvidenceDirectory();
            Directory.CreateDirectory(evidenceDirectory);
            var captures = new List<CaptureStats>();
            if (!controller.SelectCard("recover"))
            {
                throw new InvalidOperationException("The RECOVER card could not be selected.");
            }

            controller.CardHandHost.ApplyVisualStateImmediate();
            captures.Add(Capture(controller, evidenceDirectory, "recover-selected.png", 1280, 720));
            if (!controller.SelectTarget(VerticalSliceController.TargetId))
            {
                throw new InvalidOperationException("The recover target could not be selected.");
            }

            captures.Add(Capture(controller, evidenceDirectory, "recover-targeted.png", 1280, 720));
            if (!controller.PreviewTimelineSelected(0, 0))
            {
                throw new InvalidOperationException("The recover timeline preview was not legal.");
            }

            captures.Add(Capture(controller, evidenceDirectory, "recover-timeline-valid.png", 1280, 720));
            if (!controller.TryPlaceSelected(0, 0))
            {
                throw new InvalidOperationException("The recover action could not be committed.");
            }

            captures.Add(Capture(controller, evidenceDirectory, "recover-before-resolve.png", 1280, 720));
            var snapshot = controller.ResolveTimeline();
            if (snapshot.OccupantEffectResults.Count != 1 ||
                snapshot.OccupantEffectResults[0].EffectKind != CardEffectKind.Recover ||
                snapshot.OccupantEffectResults[0].Before.Hp != 10 ||
                snapshot.OccupantEffectResults[0].After.Hp != 100 ||
                controller.CurrentTargetHp != 100)
            {
                throw new InvalidOperationException("Recover did not resolve from 10 HP to 100 HP.");
            }

            captures.Add(Capture(controller, evidenceDirectory, "recover-after-resolve.png", 1280, 720));
            WriteRemainingCardsGateASummary(evidenceDirectory, captures);
            Debug.Log("TIMEKEY_REMAINING_CARDS_GATE_A_CAPTURE_PASS");
        }

        [MenuItem("Time Key/Capture Remaining Cards Gate B")]
        public static void CaptureRemainingCardsGateB()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var evidenceDirectory = GetRemainingCardsGateBEvidenceDirectory();
            Directory.CreateDirectory(evidenceDirectory);
            var captures = new List<CaptureStats>();

            var towerController = OpenInitializedSlice();
            towerController.SetBoardView(32f);
            if (!towerController.SelectCard("tower"))
            {
                throw new InvalidOperationException("The TOWER card could not be selected.");
            }

            towerController.CardHandHost.ApplyVisualStateImmediate();
            captures.Add(Capture(towerController, evidenceDirectory, "tower-selected.png", 1280, 720));
            var towerCoordinate = new HexCoord(0, 0);
            if (!towerController.SelectEarthquakeTarget(towerCoordinate))
            {
                throw new InvalidOperationException("The empty Tower tile could not be selected.");
            }

            captures.Add(Capture(towerController, evidenceDirectory, "tower-targeted.png", 1280, 720));
            if (!towerController.PreviewTimelineSelected(4, 0))
            {
                throw new InvalidOperationException("The Tower timeline preview was not legal.");
            }

            captures.Add(Capture(towerController, evidenceDirectory, "tower-timeline-valid.png", 1280, 720));
            if (!towerController.TryPlaceSelected(4, 0))
            {
                throw new InvalidOperationException("The Tower action could not be committed.");
            }

            var towerSnapshot = towerController.ResolveTimeline();
            if (towerSnapshot.OccupantEffectResults.Count != 1 ||
                towerSnapshot.OccupantEffectResults[0].After == null ||
                towerSnapshot.OccupantEffectResults[0].After.CreationId != "tower" ||
                towerSnapshot.OccupantEffectResults[0].After.Hp != 100)
            {
                throw new InvalidOperationException("Tower did not create one 100 HP occupant.");
            }

            var towerObject = GameObject.Find(
                "Occupant-" + towerSnapshot.OccupantEffectResults[0].After.RuntimeId);
            if (towerObject == null ||
                towerObject.GetComponent<CombatOccupantView>() == null ||
                towerObject.GetComponentsInChildren<Collider>(true).Length != 0)
            {
                throw new InvalidOperationException("The rendered Tower Prefab is incomplete.");
            }

            captures.Add(Capture(towerController, evidenceDirectory, "tower-after-resolve.png", 1280, 720));

            var poisonController = OpenInitializedSlice();
            poisonController.SetBoardView(32f);
            if (!poisonController.SelectCard("poison"))
            {
                throw new InvalidOperationException("The POISON card could not be selected.");
            }

            poisonController.CardHandHost.ApplyVisualStateImmediate();
            captures.Add(Capture(poisonController, evidenceDirectory, "poison-selected.png", 1280, 720));
            if (!poisonController.SelectTarget(VerticalSliceController.TargetId))
            {
                throw new InvalidOperationException("The poison target could not be selected.");
            }

            captures.Add(Capture(poisonController, evidenceDirectory, "poison-targeted.png", 1280, 720));
            if (!poisonController.PreviewTimelineSelected(4, 0))
            {
                throw new InvalidOperationException("The poison timeline preview was not legal.");
            }

            captures.Add(Capture(poisonController, evidenceDirectory, "poison-timeline-valid.png", 1280, 720));
            if (!poisonController.TryPlaceSelected(4, 0))
            {
                throw new InvalidOperationException("The poison action could not be committed.");
            }

            var poisonSnapshot = poisonController.ResolveTimeline();
            var poisonStatus = GameObject.Find("PoisonStatus-" + VerticalSliceController.TargetId);
            if (poisonSnapshot.OccupantEffectResults.Count != 1 ||
                poisonSnapshot.OccupantEffectResults[0].Before.PoisonStacks != 0 ||
                poisonSnapshot.OccupantEffectResults[0].After.PoisonStacks != 2 ||
                poisonStatus == null ||
                poisonStatus.GetComponent<PoisonStatusView>() == null ||
                poisonStatus.GetComponent<PoisonStatusView>().Stacks != 2)
            {
                throw new InvalidOperationException("Poison did not render its +2 status result.");
            }

            captures.Add(Capture(poisonController, evidenceDirectory, "poison-after-resolve.png", 1280, 720));
            WriteRemainingCardsGateBSummary(evidenceDirectory, captures);
            Debug.Log("TIMEKEY_REMAINING_CARDS_GATE_B_CAPTURE_PASS");
        }

        [MenuItem("Time Key/Capture Remaining Cards Gate C")]
        public static void CaptureRemainingCardsGateC()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var evidenceDirectory = GetRemainingCardsGateCEvidenceDirectory();
            Directory.CreateDirectory(evidenceDirectory);
            var captures = new List<CaptureStats>();

            var windController = OpenInitializedSlice();
            windController.SetBoardView(32f);
            if (!windController.SelectCard("wind"))
            {
                throw new InvalidOperationException("The WIND card could not be selected.");
            }

            windController.CardHandHost.ApplyVisualStateImmediate();
            captures.Add(Capture(windController, evidenceDirectory, "wind-selected.png", 1280, 720));
            if (windController.PreviewTimelineSelected(11, 0))
            {
                throw new InvalidOperationException("The right-edge Wind clear mask was reported as legal.");
            }

            captures.Add(Capture(windController, evidenceDirectory, "wind-clear-invalid.png", 1280, 720));
            if (!windController.PreviewTimelineSelected(0, 0))
            {
                throw new InvalidOperationException("The Wind empty clear preview was not legal.");
            }

            captures.Add(Capture(windController, evidenceDirectory, "wind-clear-empty.png", 1280, 720));
            if (!windController.PreviewTimelineSelected(1, 0))
            {
                throw new InvalidOperationException("The Wind clear hit preview was not legal.");
            }

            var clearPreview = UnityEngine.Object.FindFirstObjectByType<ClearTimelinePreview>();
            if (clearPreview == null ||
                clearPreview.ActiveCoordinates.Count != 4 ||
                GameObject.Find("Slot-2-1").GetComponent<TimelineCellView>().DisplayText != CombatChineseText.ClearHit)
            {
                throw new InvalidOperationException("The Wind preview did not expose its 2x2 occupied state.");
            }

            captures.Add(Capture(windController, evidenceDirectory, "wind-clear-hit.png", 1280, 720));
            if (!windController.CancelSelectedCard() ||
                GameObject.Find("Slot-2-1").GetComponent<TimelineCellView>().DisplayText !=
                CombatChineseText.EnemyIntentTimelineLabel)
            {
                throw new InvalidOperationException("Cancelling Wind did not restore the original action view.");
            }

            captures.Add(Capture(windController, evidenceDirectory, "wind-after-cancel.png", 1280, 720));
            if (!windController.SelectCard("wind") ||
                !windController.PreviewTimelineSelected(1, 0) ||
                !windController.TryPlaceSelected(1, 0) ||
                windController.TimelineOccupiedCellCount != 0)
            {
                throw new InvalidOperationException("Wind did not remove the complete enemy action.");
            }

            captures.Add(Capture(windController, evidenceDirectory, "wind-after-clear.png", 1280, 720));

            var tornadoController = OpenInitializedSlice();
            tornadoController.SetBoardView(32f);
            if (!tornadoController.SelectCard("tornado"))
            {
                throw new InvalidOperationException("The TORNADO card could not be selected.");
            }

            tornadoController.CardHandHost.ApplyVisualStateImmediate();
            captures.Add(Capture(tornadoController, evidenceDirectory, "tornado-selected.png", 1280, 720));
            if (tornadoController.PreviewTimelineSelected(1, 0))
            {
                throw new InvalidOperationException("The shifted Tornado mask was reported as legal.");
            }

            captures.Add(Capture(tornadoController, evidenceDirectory, "tornado-clear-invalid.png", 1280, 720));
            if (!tornadoController.PreviewTimelineSelected(0, 0))
            {
                throw new InvalidOperationException("The 12x1 Tornado empty clear was not legal.");
            }

            clearPreview = UnityEngine.Object.FindFirstObjectByType<ClearTimelinePreview>();
            if (clearPreview == null || clearPreview.ActiveCoordinates.Count != 12)
            {
                throw new InvalidOperationException("The Tornado preview did not cover all 12 row cells.");
            }

            captures.Add(Capture(tornadoController, evidenceDirectory, "tornado-clear-empty.png", 1280, 720));
            if (!tornadoController.TryPlaceSelected(0, 0) ||
                tornadoController.TimelineOccupiedCellCount != 1)
            {
                throw new InvalidOperationException("The legal Tornado empty clear changed the enemy action.");
            }

            captures.Add(Capture(tornadoController, evidenceDirectory, "tornado-after-empty-clear.png", 1280, 720));

            tornadoController = OpenInitializedSlice();
            tornadoController.SetBoardView(32f);
            if (!tornadoController.SelectCard("tornado") ||
                !tornadoController.PreviewTimelineSelected(0, 1))
            {
                throw new InvalidOperationException("The Tornado hit clear preview was not legal.");
            }

            captures.Add(Capture(tornadoController, evidenceDirectory, "tornado-clear-hit.png", 1280, 720));
            if (!tornadoController.TryPlaceSelected(0, 1) ||
                tornadoController.TimelineOccupiedCellCount != 0)
            {
                throw new InvalidOperationException("Tornado did not remove the complete enemy action.");
            }

            captures.Add(Capture(tornadoController, evidenceDirectory, "tornado-after-clear.png", 1280, 720));
            WriteRemainingCardsGateCSummary(evidenceDirectory, captures);
            Debug.Log("TIMEKEY_REMAINING_CARDS_GATE_C_CAPTURE_PASS");
        }

        private static VerticalSliceController OpenInitializedSlice()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException("The vertical slice scene could not be opened.");
            }

            var root = GameObject.Find("VerticalSliceRoot");
            var controller = root == null ? null : root.GetComponent<VerticalSliceController>();
            var composition = root == null ? null : root.GetComponent<CombatCompositionRoot>();
            if (controller == null || composition == null)
            {
                throw new InvalidOperationException("The vertical slice root is incomplete.");
            }

            composition.Initialize();
            controller.BuildSceneGraph();
            ValidateScene(controller, composition);
            return controller;
        }

        private static int PlayRecoverRound(VerticalSliceController controller)
        {
            if (!controller.SelectCard("recover") ||
                !controller.SelectTarget(VerticalSliceController.TargetId) ||
                !controller.TryPlaceSelected(0, 0))
            {
                throw new InvalidOperationException("The Recover round could not be committed.");
            }

            var occupiedCells = controller.TimelineOccupiedCellCount;
            controller.ResolveTimeline();
            return occupiedCells;
        }

        private static void ValidateBattleFlow(
            VerticalSliceController controller,
            int expectedPhase,
            int expectedDraw,
            int expectedHand,
            int expectedDiscard)
        {
            var snapshot = controller.BattleFlow;
            if (snapshot == null || snapshot.Era != 1 || snapshot.Phase != expectedPhase ||
                snapshot.DrawPile.Count != expectedDraw ||
                snapshot.Hand.Count != expectedHand ||
                snapshot.DiscardPile.Count != expectedDiscard)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Battle-flow snapshot mismatch: phase={0}, draw={1}, hand={2}, discard={3}.",
                        snapshot == null ? -1 : snapshot.Phase,
                        snapshot == null ? -1 : snapshot.DrawPile.Count,
                        snapshot == null ? -1 : snapshot.Hand.Count,
                        snapshot == null ? -1 : snapshot.DiscardPile.Count));
            }
        }

        private static void CaptureResponsive(
            VerticalSliceController controller,
            string evidenceDirectory,
            string prefix,
            ICollection<CaptureStats> captures)
        {
            captures.Add(Capture(
                controller,
                evidenceDirectory,
                prefix + "-1280x720.png",
                1280,
                720));
            captures.Add(Capture(
                controller,
                evidenceDirectory,
                prefix + "-1920x1080.png",
                1920,
                1080));
            captures.Add(Capture(
                controller,
                evidenceDirectory,
                prefix + "-2560x1080.png",
                2560,
                1080));
        }

        private static void CaptureRemainingCardsFinalYawEvidence(
            string evidenceDirectory,
            ICollection<CaptureStats> captures)
        {
            var towerController = OpenInitializedSlice();
            var towerCoordinate = new HexCoord(0, 0);
            if (!towerController.SelectCard("tower") ||
                !towerController.SelectEarthquakeTarget(towerCoordinate) ||
                !towerController.PreviewTimelineSelected(4, 0) ||
                !towerController.TryPlaceSelected(4, 0))
            {
                throw new InvalidOperationException("The final Tower interaction path could not be arranged.");
            }

            var towerSnapshot = towerController.ResolveTimeline();
            if (towerSnapshot.OccupantEffectResults.Count != 1 ||
                towerSnapshot.OccupantEffectResults[0].After == null)
            {
                throw new InvalidOperationException("The final Tower interaction path did not create an occupant.");
            }

            var towerObject = GameObject.Find(
                "Occupant-" + towerSnapshot.OccupantEffectResults[0].After.RuntimeId);
            var towerColumn = towerController.GetTileColumn(towerCoordinate);
            if (towerObject == null || towerObject.transform.parent != towerColumn.OccupantAnchor)
            {
                throw new InvalidOperationException("The final Tower view is not attached to its occupant anchor.");
            }

            CaptureSelectableYawSeries(
                towerController,
                towerCoordinate,
                "tower-after-resolve",
                evidenceDirectory,
                captures);

            var poisonController = OpenInitializedSlice();
            if (!poisonController.SelectCard("poison") ||
                !poisonController.SelectTarget(VerticalSliceController.TargetId) ||
                !poisonController.PreviewTimelineSelected(4, 0) ||
                !poisonController.TryPlaceSelected(4, 0))
            {
                throw new InvalidOperationException("The final Poison interaction path could not be arranged.");
            }

            var poisonSnapshot = poisonController.ResolveTimeline();
            var poisonStatus = GameObject.Find("PoisonStatus-" + VerticalSliceController.TargetId);
            if (poisonSnapshot.OccupantEffectResults.Count != 1 ||
                poisonSnapshot.OccupantEffectResults[0].After.PoisonStacks != 2 ||
                poisonStatus == null)
            {
                throw new InvalidOperationException("The final Poison interaction path did not render two stacks.");
            }

            CaptureSelectableYawSeries(
                poisonController,
                new HexCoord(1, 0),
                "poison-after-resolve",
                evidenceDirectory,
                captures);
        }

        private static void CaptureSelectableYawSeries(
            VerticalSliceController controller,
            HexCoord coordinate,
            string filePrefix,
            string evidenceDirectory,
            ICollection<CaptureStats> captures)
        {
            foreach (var yaw in new[] { 0, 90, 180, 270 })
            {
                controller.SetBoardView(yaw);
                Physics.SyncTransforms();
                var bounds = controller.GetTileColumn(coordinate).TopBounds;
                var screenPoint = controller.SceneCamera.WorldToScreenPoint(
                    new Vector3(bounds.center.x, bounds.max.y - 0.02f, bounds.center.z));
                if (!controller.TrySelectWorldAtScreenPoint(screenPoint) ||
                    !controller.SelectedTile.HasValue ||
                    controller.SelectedTile.Value != coordinate)
                {
                    throw new InvalidOperationException(
                        filePrefix + " tile selection failed at yaw " + yaw + ".");
                }

                captures.Add(Capture(
                    controller,
                    evidenceDirectory,
                    string.Format("{0}-yaw-{1:000}-1920x1080.png", filePrefix, yaw),
                    1920,
                    1080));
            }
        }

        private static void ValidateScene(
            VerticalSliceController controller,
            CombatCompositionRoot composition)
        {
            if (controller.SceneCamera == null || controller.SceneCamera.orthographic)
            {
                throw new InvalidOperationException("A perspective combat-board camera is required.");
            }

            if (controller.BoardCamera == null)
            {
                throw new InvalidOperationException("The orbit camera controller is missing.");
            }

            if (controller.SceneCanvas == null ||
                controller.SceneCanvas.renderMode != RenderMode.ScreenSpaceCamera)
            {
                throw new InvalidOperationException("A screen-space camera Canvas is required.");
            }

            if (controller.TimelineSlotCount != 36)
            {
                throw new InvalidOperationException("The timeline must contain exactly 36 slots.");
            }

            if (controller.BoardTileCount != 19 || UnityEngine.Object.FindObjectsByType<MeshFilter>().Length < 19)
            {
                throw new InvalidOperationException("The scene must contain the frozen 19-hex combat board.");
            }

            if (GameObject.Find("OriginalArt-center_altar") == null)
            {
                throw new InvalidOperationException("The original center altar art is missing.");
            }

            var scaler = controller.SceneCanvas.GetComponent<CanvasScaler>();
            if (scaler == null || scaler.referenceResolution != new Vector2(1920f, 1080f))
            {
                throw new InvalidOperationException("The CanvasScaler reference resolution is not frozen.");
            }

            if (controller.CardHandHost == null ||
                controller.CardHandHost.CardCount != 5 ||
                controller.CardHand == null ||
                composition.CardCount != 7)
            {
                throw new InvalidOperationException(
                    "The five-card formal hand or seven-card content catalog is incomplete.");
            }

            foreach (var card in controller.CardHandHost.Cards)
            {
                if (card.Artwork == null || card.Artwork.sprite == null)
                {
                    throw new InvalidOperationException(
                        "Original card artwork is missing for " + card.StableId + ".");
                }
            }

            if (controller.BoardRangePreview == null || controller.BoardRangePreview.RegisteredCount != 19)
            {
                throw new InvalidOperationException("The board range preview is not registered to all 19 tiles.");
            }

            if (controller.TimelinePreview == null || controller.TimelinePreview.RegisteredCount != 36)
            {
                throw new InvalidOperationException("The timeline preview is not registered to all 36 cells.");
            }

            var clearTimelinePreview = UnityEngine.Object.FindFirstObjectByType<ClearTimelinePreview>();
            if (clearTimelinePreview == null || clearTimelinePreview.RegisteredCount != 36)
            {
                throw new InvalidOperationException("The clear timeline preview is not registered to all 36 cells.");
            }
        }

        private static CaptureStats Capture(
            VerticalSliceController controller,
            string directory,
            string fileName,
            int width,
            int height)
        {
            var camera = controller.SceneCamera;
            var renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);

            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();

                var stats = Measure(texture, width, height);
                if (stats.LuminanceRange < 0.10f || stats.SampledOpaquePixels == 0)
                {
                    throw new InvalidOperationException(fileName + " appears blank or single-tone.");
                }

                File.WriteAllBytes(Path.Combine(directory, fileName), texture.EncodeToPNG());
                return new CaptureStats(
                    fileName,
                    stats.Width,
                    stats.Height,
                    stats.LuminanceRange,
                    stats.SampledOpaquePixels);
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(texture);
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        private static CaptureStats Measure(Texture2D texture, int width, int height)
        {
            var minimum = 1f;
            var maximum = 0f;
            var opaque = 0;
            const int stride = 16;

            for (var y = stride / 2; y < height; y += stride)
            {
                for (var x = stride / 2; x < width; x += stride)
                {
                    var color = texture.GetPixel(x, y);
                    var luminance = (0.2126f * color.r) + (0.7152f * color.g) + (0.0722f * color.b);
                    minimum = Mathf.Min(minimum, luminance);
                    maximum = Mathf.Max(maximum, luminance);
                    if (color.a > 0.5f)
                    {
                        opaque++;
                    }
                }
            }

            return new CaptureStats(string.Empty, width, height, maximum - minimum, opaque);
        }

        private static void MergeEvidenceCaptures(
            string sourceDirectory,
            string destinationDirectory,
            ICollection<CaptureStats> captures)
        {
            var files = Directory.GetFiles(sourceDirectory, "*.png", SearchOption.TopDirectoryOnly);
            Array.Sort(files, StringComparer.Ordinal);
            for (var index = 0; index < files.Length; index++)
            {
                var fileName = Path.GetFileName(files[index]);
                var bytes = File.ReadAllBytes(files[index]);
                var texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
                try
                {
                    if (!texture.LoadImage(bytes))
                    {
                        throw new InvalidOperationException(fileName + " could not be loaded for final evidence.");
                    }

                    var stats = Measure(texture, texture.width, texture.height);
                    File.WriteAllBytes(Path.Combine(destinationDirectory, fileName), bytes);
                    captures.Add(new CaptureStats(
                        fileName,
                        texture.width,
                        texture.height,
                        stats.LuminanceRange,
                        stats.SampledOpaquePixels));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
        }

        private static string GetRemainingCardsGateDEvidenceDirectory()
        {
            var repositoryRoot = Environment.GetEnvironmentVariable("TIMEKEY_REPOSITORY_ROOT");
            if (string.IsNullOrWhiteSpace(repositoryRoot))
            {
                repositoryRoot = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", ".."));
            }

            if (!Directory.Exists(Path.Combine(repositoryRoot, "docs", "migration", "unity-3d")))
            {
                throw new DirectoryNotFoundException("TIMEKEY_REPOSITORY_ROOT does not contain migration docs.");
            }

            return Path.Combine(
                repositoryRoot,
                "docs",
                "migration",
                "unity-3d",
                "04-verification",
                "evidence",
                "remaining-cards-gate-d");
        }

        private static string GetDeckBattleFlowGateDEvidenceDirectory()
        {
            var repositoryRoot = Environment.GetEnvironmentVariable("TIMEKEY_REPOSITORY_ROOT");
            if (string.IsNullOrWhiteSpace(repositoryRoot))
            {
                repositoryRoot = Path.GetFullPath(
                    Path.Combine(UnityEngine.Application.dataPath, "..", ".."));
            }

            if (!Directory.Exists(Path.Combine(repositoryRoot, "docs", "migration", "unity-3d")))
            {
                throw new DirectoryNotFoundException(
                    "TIMEKEY_REPOSITORY_ROOT does not contain migration docs.");
            }

            return Path.Combine(
                repositoryRoot,
                "docs",
                "migration",
                "unity-3d",
                "04-verification",
                "evidence",
                "deck-battle-flow-gate-d");
        }

        private static void CopySilverAttribution(string buildDirectory)
        {
            var source = Path.Combine(
                UnityEngine.Application.dataPath,
                "_Project",
                "Resources",
                "Fonts",
                "Silver-ATTRIBUTION.txt");
            if (!File.Exists(source))
            {
                throw new FileNotFoundException("Silver attribution is missing from the project.", source);
            }

            File.Copy(source, Path.Combine(buildDirectory, "Silver-ATTRIBUTION.txt"), true);
        }

        private static string GetSimplifiedChineseEvidenceDirectory()
        {
            var repositoryRoot = Environment.GetEnvironmentVariable("TIMEKEY_REPOSITORY_ROOT");
            if (string.IsNullOrWhiteSpace(repositoryRoot))
            {
                repositoryRoot = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", ".."));
            }

            if (!Directory.Exists(Path.Combine(repositoryRoot, "docs", "migration", "unity-3d")))
            {
                throw new DirectoryNotFoundException("TIMEKEY_REPOSITORY_ROOT does not contain migration docs.");
            }

            return Path.Combine(
                repositoryRoot,
                "docs",
                "migration",
                "unity-3d",
                "04-verification",
                "evidence",
                "simplified-chinese-localization");
        }

        private static string GetTurnLifecycleGateBEvidenceDirectory()
        {
            var repositoryRoot = Environment.GetEnvironmentVariable("TIMEKEY_REPOSITORY_ROOT");
            if (string.IsNullOrWhiteSpace(repositoryRoot))
            {
                repositoryRoot = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", ".."));
            }

            return Path.Combine(
                repositoryRoot,
                "docs",
                "migration",
                "unity-3d",
                "04-verification",
                "evidence",
                "turn-lifecycle-gate-b");
        }

        private static string GetTurnLifecycleGateDEvidenceDirectory()
        {
            var repositoryRoot = Environment.GetEnvironmentVariable("TIMEKEY_REPOSITORY_ROOT");
            if (string.IsNullOrWhiteSpace(repositoryRoot))
            {
                repositoryRoot = Path.GetFullPath(
                    Path.Combine(UnityEngine.Application.dataPath, "..", ".."));
            }

            return Path.Combine(
                repositoryRoot,
                "docs",
                "migration",
                "unity-3d",
                "04-verification",
                "evidence",
                "turn-lifecycle-gate-d");
        }

        private static OccupantEffectResult FindOccupantEffect(
            ResolutionSnapshot snapshot,
            CardEffectKind kind)
        {
            for (var index = 0; index < snapshot.OccupantEffectResults.Count; index++)
            {
                if (snapshot.OccupantEffectResults[index].EffectKind == kind)
                {
                    return snapshot.OccupantEffectResults[index];
                }
            }

            return null;
        }

        private static T GetPrivateField<T>(object target, string fieldName)
            where T : class
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            return field == null ? null : field.GetValue(target) as T;
        }

        private static void ValidateSimplifiedChinesePresentation(VerticalSliceController controller)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(VerticalSliceSceneAuthoring.ChineseFontAssetPath);
            if (font == null)
            {
                throw new InvalidOperationException("Silver font is missing from the localization build.");
            }

            var texts = controller.SceneCanvas.GetComponentsInChildren<Text>(true);
            for (var index = 0; index < texts.Length; index++)
            {
                var current = texts[index];
                if (current.font != font)
                {
                    throw new InvalidOperationException(current.name + " is not using the Silver font.");
                }

                for (var characterIndex = 0; characterIndex < current.text.Length; characterIndex++)
                {
                    var character = current.text[characterIndex];
                    if ((character >= 'A' && character <= 'Z') ||
                        (character >= 'a' && character <= 'z'))
                    {
                        throw new InvalidOperationException(
                            current.name + " still contains player-facing English: " + current.text);
                    }
                }
            }
        }

        private static void WriteSimplifiedChineseSummary(
            string directory,
            IReadOnlyList<CaptureStats> captures,
            BuildReport report)
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"status\": \"passed\",");
            builder.AppendLine("  \"locale\": \"zh-CN\",");
            builder.AppendLine("  \"font\": \"Silver\",");
            builder.AppendLine("  \"playerFacingEnglishCount\": 0,");
            builder.AppendLine("  \"screenshots\": [");
            for (var index = 0; index < captures.Count; index++)
            {
                AppendCapture(builder, captures[index], index < captures.Count - 1);
            }
            builder.AppendLine("  ],");
            builder.AppendLine("  \"buildResult\": \"" + report.summary.result + "\",");
            builder.AppendLine("  \"buildBytes\": " + report.summary.totalSize);
            builder.AppendLine("}");
            File.WriteAllText(Path.Combine(directory, "localization-summary.json"), builder.ToString());
        }

        private static void WriteTurnLifecycleGateBSummary(
            string directory,
            IReadOnlyList<CaptureStats> captures)
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"status\": \"passed\",");
            builder.AppendLine("  \"font\": \"Silver\",");
            builder.AppendLine("  \"responsiveViewports\": [\"1280x720\", \"1920x1080\", \"2560x1080\"],");
            builder.AppendLine("  \"distinctActionFrames\": 2,");
            builder.AppendLine("  \"unsupportedEnemyIntentVisible\": true,");
            builder.AppendLine("  \"resolvedActionFrames\": 0,");
            builder.AppendLine("  \"nextCycleIntentFrames\": 1,");
            builder.AppendLine("  \"screenshots\": [");
            for (var index = 0; index < captures.Count; index++)
            {
                AppendCapture(builder, captures[index], index < captures.Count - 1);
            }
            builder.AppendLine("  ]");
            builder.AppendLine("}");
            File.WriteAllText(
                Path.Combine(directory, "turn-lifecycle-gate-b-summary.json"),
                builder.ToString());
        }

        private static void WriteTurnLifecycleGateDSummary(
            string directory,
            IReadOnlyList<CaptureStats> captures)
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"status\": \"passed\",");
            builder.AppendLine("  \"continuousLifecycles\": 2,");
            builder.AppendLine("  \"towerCreatedHp\": 100,");
            builder.AppendLine("  \"towerSameCycleHp\": 50,");
            builder.AppendLine("  \"towerNextCycleRemoved\": true,");
            builder.AppendLine("  \"poisonAppliedStacks\": 2,");
            builder.AppendLine("  \"newInfectionStacks\": 1,");
            builder.AppendLine("  \"newInfectionDamagedSameCycle\": false,");
            builder.AppendLine("  \"unsupportedEnemyEffectApplied\": false,");
            builder.AppendLine("  \"yawEvidence\": [0, 90, 180, 270],");
            builder.AppendLine("  \"font\": \"Silver\",");
            builder.AppendLine("  \"screenshots\": [");
            for (var index = 0; index < captures.Count; index++)
            {
                AppendCapture(builder, captures[index], index < captures.Count - 1);
            }
            builder.AppendLine("  ]");
            builder.AppendLine("}");
            File.WriteAllText(
                Path.Combine(directory, "turn-lifecycle-gate-d-summary.json"),
                builder.ToString());
        }

        private static string GetRemainingCardsGateAEvidenceDirectory()
        {
            var repositoryRoot = Environment.GetEnvironmentVariable("TIMEKEY_REPOSITORY_ROOT");
            if (string.IsNullOrWhiteSpace(repositoryRoot))
            {
                repositoryRoot = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", ".."));
            }

            return Path.Combine(
                repositoryRoot,
                "docs",
                "migration",
                "unity-3d",
                "04-verification",
                "evidence",
                "remaining-cards-gate-a");
        }

        private static string GetRemainingCardsGateBEvidenceDirectory()
        {
            var repositoryRoot = Environment.GetEnvironmentVariable("TIMEKEY_REPOSITORY_ROOT");
            if (string.IsNullOrWhiteSpace(repositoryRoot))
            {
                repositoryRoot = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", ".."));
            }

            return Path.Combine(
                repositoryRoot,
                "docs",
                "migration",
                "unity-3d",
                "04-verification",
                "evidence",
                "remaining-cards-gate-b");
        }

        private static string GetRemainingCardsGateCEvidenceDirectory()
        {
            var repositoryRoot = Environment.GetEnvironmentVariable("TIMEKEY_REPOSITORY_ROOT");
            if (string.IsNullOrWhiteSpace(repositoryRoot))
            {
                repositoryRoot = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", ".."));
            }

            return Path.Combine(
                repositoryRoot,
                "docs",
                "migration",
                "unity-3d",
                "04-verification",
                "evidence",
                "remaining-cards-gate-c");
        }

        private static void WriteRemainingCardsGateASummary(
            string directory,
            IReadOnlyList<CaptureStats> captures)
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"status\": \"passed\",");
            builder.AppendLine("  \"card\": \"recover\",");
            builder.AppendLine("  \"targetHpBefore\": 10,");
            builder.AppendLine("  \"targetHpAfter\": 100,");
            builder.AppendLine("  \"screenshots\": [");
            for (var index = 0; index < captures.Count; index++)
            {
                AppendCapture(builder, captures[index], index < captures.Count - 1);
            }
            builder.AppendLine("  ]");
            builder.AppendLine("}");
            File.WriteAllText(Path.Combine(directory, "gate-a-summary.json"), builder.ToString());
        }

        private static void WriteRemainingCardsGateBSummary(
            string directory,
            IReadOnlyList<CaptureStats> captures)
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"status\": \"passed\",");
            builder.AppendLine("  \"towerHp\": 100,");
            builder.AppendLine("  \"poisonStacks\": 2,");
            builder.AppendLine("  \"screenshots\": [");
            for (var index = 0; index < captures.Count; index++)
            {
                AppendCapture(builder, captures[index], index < captures.Count - 1);
            }
            builder.AppendLine("  ]");
            builder.AppendLine("}");
            File.WriteAllText(Path.Combine(directory, "gate-b-summary.json"), builder.ToString());
        }

        private static void WriteRemainingCardsGateCSummary(
            string directory,
            IReadOnlyList<CaptureStats> captures)
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"status\": \"passed\",");
            builder.AppendLine("  \"windMaskCells\": 4,");
            builder.AppendLine("  \"windEmptyClearLegal\": true,");
            builder.AppendLine("  \"windRemovedActions\": 1,");
            builder.AppendLine("  \"tornadoMaskCells\": 12,");
            builder.AppendLine("  \"tornadoEmptyClearRemovedActions\": 0,");
            builder.AppendLine("  \"tornadoHitClearRemovedActions\": 1,");
            builder.AppendLine("  \"screenshots\": [");
            for (var index = 0; index < captures.Count; index++)
            {
                AppendCapture(builder, captures[index], index < captures.Count - 1);
            }
            builder.AppendLine("  ]");
            builder.AppendLine("}");
            File.WriteAllText(Path.Combine(directory, "gate-c-summary.json"), builder.ToString());
        }

        private static void WriteSummary(
            string directory,
            IReadOnlyList<CaptureStats> captures,
            BuildReport report)
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"status\": \"passed\",");
            builder.AppendLine("  \"scene\": \"RemainingCardsSevenCardVerticalSlice\",");
            builder.AppendLine("  \"stableHierarchy\": \"serialized-before-play\",");
            builder.AppendLine("  \"applicationBoundary\": \"CombatApplicationSession\",");
            builder.AppendLine("  \"compositionRoot\": \"CombatCompositionRoot\",");
            builder.AppendLine("  \"catalogCards\": 7,");
            builder.AppendLine("  \"savedPrefabs\": 8,");
            builder.AppendLine("  \"seed\": 731,");
            builder.AppendLine("  \"boardTiles\": 19,");
            builder.AppendLine("  \"cameraYawEvidence\": [0, 90, 180, 270],");
            builder.AppendLine("  \"cardArt\": [\"earthquake\", \"lighting\", \"poison\", \"recover\", \"tornado\", \"tower\", \"wind\"],");
            builder.AppendLine("  \"cardInteractions\": [\"ordinary-timeline\", \"occupant-effect\", \"timeline-clear\"],");
            builder.AppendLine("  \"remainingCardsEvidence\": [\"remaining-cards-gate-a\", \"remaining-cards-gate-b\", \"remaining-cards-gate-c\"],");
            builder.AppendLine("  \"lighting\": {\"hpBefore\": 10, \"hpAfter\": 0, \"enemyIntentResolved\": true},");
            builder.AppendLine("  \"earthquake\": {\"rangeResults\": 7, \"layerDelta\": 2, \"topDelta\": 0.64},");
            builder.AppendLine("  \"recover\": {\"hpBefore\": 10, \"hpAfter\": 100},");
            builder.AppendLine("  \"tower\": {\"created\": 1, \"hp\": 100, \"coordinate\": \"0,0\"},");
            builder.AppendLine("  \"poison\": {\"stacksBefore\": 0, \"stacksAfter\": 2},");
            builder.AppendLine("  \"wind\": {\"maskCells\": 4, \"removedActions\": 1},");
            builder.AppendLine("  \"tornado\": {\"maskCells\": 12, \"emptyRemovedActions\": 0, \"hitRemovedActions\": 1},");
            builder.AppendLine("  \"earthquakeRangeResults\": 7,");
            builder.AppendLine("  \"layerDelta\": 2,");
            builder.AppendLine("  \"layerSpacing\": 0.32,");
            builder.AppendLine("  \"topDelta\": 0.64,");
            builder.AppendLine("  \"screenshots\": [");
            for (var index = 0; index < captures.Count; index++)
            {
                AppendCapture(builder, captures[index], index < captures.Count - 1);
            }
            builder.AppendLine("  ],");
            builder.AppendLine("  \"buildResult\": \"" + report.summary.result + "\",");
            builder.AppendLine("  \"buildBytes\": " + report.summary.totalSize);
            builder.AppendLine("}");
            File.WriteAllText(Path.Combine(directory, "harness-summary.json"), builder.ToString());
        }

        private static void WriteDeckBattleFlowGateDSummary(
            string directory,
            IReadOnlyList<CaptureStats> captures)
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"status\": \"passed\",");
            builder.AppendLine("  \"seed\": 731,");
            builder.AppendLine("  \"starterDeckCount\": 12,");
            builder.AppendLine("  \"formalDrawCount\": 5,");
            builder.AppendLine("  \"responsiveViewports\": [\"1280x720\", \"1920x1080\", \"2560x1080\"],");
            builder.AppendLine("  \"multiTurnAdvance\": true,");
            builder.AppendLine("  \"drawPileExhaustedAndRecycled\": true,");
            builder.AppendLine("  \"emptyDiscardVisible\": true,");
            builder.AppendLine("  \"deterministicShuffleObserved\": true,");
            builder.AppendLine("  \"timecoinsUpdatedByDomain\": true,");
            builder.AppendLine("  \"committedActionFrameSurvivedHandRemoval\": true,");
            builder.AppendLine("  \"victoryRewardClaimedOnce\": true,");
            builder.AppendLine("  \"defeatRewardEntryAbsent\": true,");
            builder.AppendLine("  \"typedReturnBoundary\": true,");
            builder.AppendLine("  \"font\": \"Silver\",");
            builder.AppendLine("  \"screenshots\": [");
            for (var index = 0; index < captures.Count; index++)
            {
                AppendCapture(builder, captures[index], index < captures.Count - 1);
            }

            builder.AppendLine("  ]");
            builder.AppendLine("}");
            File.WriteAllText(
                Path.Combine(directory, "visual-summary.json"),
                builder.ToString());
        }

        private static void AppendCapture(StringBuilder builder, CaptureStats stats, bool trailingComma)
        {
            builder.Append("    {\"file\": \"");
            builder.Append(stats.FileName);
            builder.Append("\", \"width\": ");
            builder.Append(stats.Width);
            builder.Append(", \"height\": ");
            builder.Append(stats.Height);
            builder.Append(", \"luminanceRange\": ");
            builder.Append(stats.LuminanceRange.ToString("0.000", CultureInfo.InvariantCulture));
            builder.Append(", \"sampledOpaquePixels\": ");
            builder.Append(stats.SampledOpaquePixels);
            builder.AppendLine(trailingComma ? "}," : "}");
        }

        private readonly struct CaptureStats
        {
            public CaptureStats(string fileName, int width, int height, float luminanceRange, int sampledOpaquePixels)
            {
                FileName = fileName;
                Width = width;
                Height = height;
                LuminanceRange = luminanceRange;
                SampledOpaquePixels = sampledOpaquePixels;
            }

            public string FileName { get; }

            public int Width { get; }

            public int Height { get; }

            public float LuminanceRange { get; }

            public int SampledOpaquePixels { get; }
        }
    }
}
