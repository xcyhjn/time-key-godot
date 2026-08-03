using System.Collections;
using NUnit.Framework;
using TimeKey.Application.EraClock;
using TimeKey.Presentation.EraClock;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TimeKey.Tests.PlayMode.EraClock
{
    public sealed class EraClockPresenterTests
    {
        private GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void InitialBindSnapsAllTerminalValuesAndDisablesRaycasts()
        {
            Rig rig = CreateRig(phaseDuration: 0.2f, rolloverDuration: 0.4f, anchorDuration: 0.5f);

            rig.Presenter.ApplySnapshot(Snapshot(era: 2, phase: 3, sequence: 1));

            Assert.That(rig.Presenter.State, Is.EqualTo(EraClockPresenterState.Settled));
            Assert.That(rig.EraLabel.text, Is.EqualTo("第2时代"));
            Assert.That(rig.PhaseLabel.text, Is.EqualTo("3 / 8"));
            Assert.That(SignedAngle(rig.Pointer.localEulerAngles.z), Is.EqualTo(-90f).Within(0.1f));
            Assert.That(rig.Progress.fillAmount, Is.GreaterThanOrEqualTo(3f / 8f));
            Assert.That(rig.Progress.fillAmount, Is.LessThan(4f / 8f));
            Assert.That(Vector3.Distance(rig.ClockRoot.position, rig.CenterAnchor.position), Is.LessThan(0.1f));
            Assert.That(rig.RootGroup.blocksRaycasts, Is.False);
            Assert.That(rig.Face.raycastTarget, Is.False);
            Assert.That(rig.Ring.raycastTarget, Is.False);
            Assert.That(rig.PointerImage.raycastTarget, Is.False);
        }

        [UnityTest]
        public IEnumerator AdjacentPhaseAnimatesButPublishesFinalLabelsImmediately()
        {
            Rig rig = CreateRig(phaseDuration: 0.2f, rolloverDuration: 0.4f, anchorDuration: 0.5f);
            rig.Presenter.ApplySnapshot(Snapshot(era: 1, phase: 3, sequence: 1));

            rig.Presenter.ApplySnapshot(Snapshot(era: 1, phase: 4, sequence: 2));

            Assert.That(rig.Presenter.State, Is.EqualTo(EraClockPresenterState.PhaseTransition));
            Assert.That(rig.PhaseLabel.text, Is.EqualTo("4 / 8"));
            Assert.That(rig.Progress.fillAmount, Is.GreaterThanOrEqualTo(3f / 8f));
            Assert.That(rig.Progress.fillAmount, Is.LessThan(4f / 8f));
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(rig.Progress.fillAmount, Is.GreaterThan(3f / 8f));
            Assert.That(rig.Progress.fillAmount, Is.LessThan(4f / 8f));
            yield return new WaitForSecondsRealtime(0.16f);
            AssertTerminal(rig, era: 1, phase: 4, EraClockAnchorTarget.Center);
        }

        [UnityTest]
        public IEnumerator RolloverPassesThroughResetPulseBeforePhaseOneTerminal()
        {
            Rig rig = CreateRig(phaseDuration: 0.1f, rolloverDuration: 0.3f, anchorDuration: 0.5f);
            rig.Presenter.ApplySnapshot(Snapshot(era: 4, phase: 8, sequence: 8));

            rig.Presenter.ApplySnapshot(Snapshot(era: 5, phase: 1, sequence: 9));

            Assert.That(rig.Presenter.State, Is.EqualTo(EraClockPresenterState.RolloverTransition));
            Assert.That(rig.EraLabel.text, Is.EqualTo("第5时代"));
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(rig.Pulse.alpha, Is.GreaterThan(0f));
            Assert.That(rig.Progress.fillAmount, Is.LessThan(0.05f));
            yield return new WaitForSecondsRealtime(0.22f);
            AssertTerminal(rig, era: 5, phase: 1, EraClockAnchorTarget.Center);
        }

        [UnityTest]
        public IEnumerator NewSnapshotCancelsOldCoroutineAndLatestSnapshotWins()
        {
            Rig rig = CreateRig(phaseDuration: 0.25f, rolloverDuration: 0.4f, anchorDuration: 0.5f);
            rig.Presenter.ApplySnapshot(Snapshot(era: 1, phase: 1, sequence: 1));
            rig.Presenter.ApplySnapshot(Snapshot(era: 1, phase: 2, sequence: 2));
            yield return new WaitForSecondsRealtime(0.05f);

            rig.Presenter.ApplySnapshot(Snapshot(era: 1, phase: 3, sequence: 3));
            yield return new WaitForSecondsRealtime(0.32f);

            AssertTerminal(rig, era: 1, phase: 3, EraClockAnchorTarget.Center);
            Assert.That(rig.Presenter.CurrentSnapshot.Sequence, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator SettledAnchorTracksLayoutChangesAfterResize()
        {
            Rig rig = CreateRig(phaseDuration: 0.1f, rolloverDuration: 0.3f, anchorDuration: 0.2f);
            rig.Presenter.ApplySnapshot(Snapshot(era: 1, phase: 1, sequence: 1));

            rig.Presenter.ApplySnapshot(
                Snapshot(era: 1, phase: 1, sequence: 2, EraClockAnchorTarget.Hud));
            yield return new WaitForSecondsRealtime(0.25f);
            AssertTerminal(rig, era: 1, phase: 1, EraClockAnchorTarget.Hud);

            rig.HudAnchor.anchoredPosition = new Vector2(180f, -140f);
            yield return null;

            Assert.That(Vector3.Distance(rig.ClockRoot.position, rig.HudAnchor.position), Is.LessThan(0.1f));
        }

        [Test]
        public void ZeroDurationCompletesPhaseRolloverAndAnchorInSameFrame()
        {
            Rig rig = CreateRig(phaseDuration: 0f, rolloverDuration: 0f, anchorDuration: 0f);
            rig.Presenter.ApplySnapshot(Snapshot(era: 2, phase: 8, sequence: 1));

            rig.Presenter.ApplySnapshot(
                Snapshot(era: 3, phase: 1, sequence: 2, EraClockAnchorTarget.Hud));

            AssertTerminal(rig, era: 3, phase: 1, EraClockAnchorTarget.Hud);
            Assert.That(rig.Presenter.LastCompletionReason, Is.EqualTo(EraClockCompletionReason.Immediate));
        }

        [UnityTest]
        public IEnumerator RebindStopsAnimationAndSnapsLatestSnapshotToNewAnchors()
        {
            Rig rig = CreateRig(phaseDuration: 0.3f, rolloverDuration: 0.5f, anchorDuration: 0.4f);
            rig.Presenter.ApplySnapshot(Snapshot(era: 1, phase: 1, sequence: 1));
            rig.Presenter.ApplySnapshot(
                Snapshot(era: 1, phase: 2, sequence: 2, EraClockAnchorTarget.Hud));
            yield return new WaitForSecondsRealtime(0.05f);

            RectTransform newCenter = CreateAnchor("ReboundCenter", new Vector2(-200f, 0f));
            RectTransform newHud = CreateAnchor("ReboundHud", new Vector2(260f, -100f));
            EraClockViewBindings rebound = new EraClockViewBindings(
                rig.ClockRoot,
                rig.Face,
                rig.Ring,
                rig.Pointer,
                rig.PointerImage,
                rig.Progress,
                rig.EraLabel,
                rig.PhaseLabel,
                newCenter,
                newHud,
                rig.RootGroup,
                rig.Pulse);

            rig.Presenter.Rebind(rebound);

            Assert.That(rig.Presenter.State, Is.EqualTo(EraClockPresenterState.Settled));
            Assert.That(Vector3.Distance(rig.ClockRoot.position, newHud.position), Is.LessThan(0.1f));
            Assert.That(rig.PhaseLabel.text, Is.EqualTo("2 / 8"));
            Assert.That(rig.Presenter.LastCompletionReason, Is.EqualTo(EraClockCompletionReason.Rebound));
        }

        [Test]
        public void StaleAndRepeatedSnapshotsDoNotReplaceCurrentPresentation()
        {
            Rig rig = CreateRig(phaseDuration: 0f, rolloverDuration: 0f, anchorDuration: 0f);
            EraClockPresentationSnapshot current = Snapshot(era: 2, phase: 6, sequence: 10);
            rig.Presenter.ApplySnapshot(current);

            EraClockApplyResult repeated = rig.Presenter.ApplySnapshot(current);
            EraClockApplyResult stale = rig.Presenter.ApplySnapshot(
                Snapshot(era: 2, phase: 5, sequence: 9));

            Assert.That(repeated.Status, Is.EqualTo(EraClockApplyStatus.Repeated));
            Assert.That(stale.Status, Is.EqualTo(EraClockApplyStatus.Stale));
            Assert.That(rig.Presenter.CurrentSnapshot, Is.SameAs(current));
            Assert.That(rig.PhaseLabel.text, Is.EqualTo("6 / 8"));
        }

        [UnityTest]
        public IEnumerator DisableAndEnableLeavesLatestTerminalState()
        {
            Rig rig = CreateRig(phaseDuration: 0.3f, rolloverDuration: 0.5f, anchorDuration: 0.4f);
            rig.Presenter.ApplySnapshot(Snapshot(era: 1, phase: 1, sequence: 1));
            rig.Presenter.ApplySnapshot(Snapshot(era: 1, phase: 2, sequence: 2));
            yield return null;

            rig.Presenter.enabled = false;

            Assert.That(rig.Presenter.State, Is.EqualTo(EraClockPresenterState.Disabled));
            Assert.That(rig.Progress.fillAmount, Is.EqualTo(2f / 8f).Within(0.001f));
            rig.Presenter.enabled = true;
            Assert.That(rig.Presenter.State, Is.EqualTo(EraClockPresenterState.Settled));
            Assert.That(rig.Presenter.LastCompletionReason, Is.EqualTo(EraClockCompletionReason.Disabled));
        }

        private Rig CreateRig(float phaseDuration, float rolloverDuration, float anchorDuration)
        {
            root = new GameObject("EraClockTestRoot", typeof(RectTransform), typeof(Canvas));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(1920f, 1080f);

            RectTransform centerAnchor = CreateAnchor("CenterAnchor", Vector2.zero);
            centerAnchor.anchorMin = centerAnchor.anchorMax = new Vector2(0.5f, 0.5f);
            RectTransform hudAnchor = CreateAnchor("HudAnchor", new Vector2(0f, -110f));
            hudAnchor.anchorMin = hudAnchor.anchorMax = new Vector2(0.5f, 1f);

            var clockObject = new GameObject("EraClock", typeof(RectTransform), typeof(CanvasGroup));
            clockObject.transform.SetParent(root.transform, false);
            var clockRoot = clockObject.GetComponent<RectTransform>();
            clockRoot.sizeDelta = new Vector2(320f, 320f);
            var rootGroup = clockObject.GetComponent<CanvasGroup>();

            RawImage face = CreateRawImage("ClockFace", clockRoot, "Art/Shell/MainMenu/clock_noring");
            RawImage ring = CreateRawImage("ClockRing", clockRoot, "Art/Shell/MainMenu/ring");
            RawImage pointerImage = CreateRawImage("Pointer", clockRoot, "Art/Shell/MainMenu/point");
            RectTransform pointer = pointerImage.rectTransform;

            var progressObject = new GameObject("EraProgress", typeof(RectTransform), typeof(Image));
            progressObject.transform.SetParent(clockRoot, false);
            var progress = progressObject.GetComponent<Image>();
            progress.color = new Color(0.25f, 0.84f, 0.7f, 1f);
            progress.type = Image.Type.Filled;
            progress.fillMethod = Image.FillMethod.Horizontal;
            progress.fillOrigin = 0;
            progress.rectTransform.anchorMin = new Vector2(0.15f, 0.02f);
            progress.rectTransform.anchorMax = new Vector2(0.85f, 0.02f);
            progress.rectTransform.sizeDelta = new Vector2(0f, 14f);

            Font silver = Resources.Load<Font>("Fonts/Silver");
            Assert.That(silver, Is.Not.Null);
            Text eraLabel = CreateText("EraLabel", clockRoot, silver, new Vector2(0f, -185f));
            Text phaseLabel = CreateText("PhaseLabel", clockRoot, silver, new Vector2(0f, -220f));

            var pulseObject = new GameObject("RolloverPulse", typeof(RectTransform), typeof(CanvasGroup));
            pulseObject.transform.SetParent(clockRoot, false);
            var pulse = pulseObject.GetComponent<CanvasGroup>();
            pulse.alpha = 0f;

            var presenter = clockObject.AddComponent<EraClockPresenter>();
            presenter.Rebind(
                new EraClockViewBindings(
                    clockRoot,
                    face,
                    ring,
                    pointer,
                    pointerImage,
                    progress,
                    eraLabel,
                    phaseLabel,
                    centerAnchor,
                    hudAnchor,
                    rootGroup,
                    pulse),
                new EraClockAnimationSettings(
                    phaseDuration,
                    rolloverDuration,
                    anchorDuration));

            return new Rig(
                presenter,
                clockRoot,
                face,
                ring,
                pointer,
                pointerImage,
                progress,
                eraLabel,
                phaseLabel,
                centerAnchor,
                hudAnchor,
                rootGroup,
                pulse);
        }

        private RectTransform CreateAnchor(string name, Vector2 anchoredPosition)
        {
            var anchorObject = new GameObject(name, typeof(RectTransform));
            anchorObject.transform.SetParent(root.transform, false);
            var anchor = anchorObject.GetComponent<RectTransform>();
            anchor.anchoredPosition = anchoredPosition;
            return anchor;
        }

        private static RawImage CreateRawImage(
            string name,
            RectTransform parent,
            string resourcePath)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            imageObject.transform.SetParent(parent, false);
            var image = imageObject.GetComponent<RawImage>();
            image.texture = Resources.Load<Texture>(resourcePath);
            Assert.That(image.texture, Is.Not.Null, resourcePath);
            image.rectTransform.anchorMin = Vector2.zero;
            image.rectTransform.anchorMax = Vector2.one;
            image.rectTransform.offsetMin = Vector2.zero;
            image.rectTransform.offsetMax = Vector2.zero;
            return image;
        }

        private static Text CreateText(
            string name,
            RectTransform parent,
            Font font,
            Vector2 anchoredPosition)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = 30;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.rectTransform.sizeDelta = new Vector2(360f, 44f);
            text.rectTransform.anchoredPosition = anchoredPosition;
            return text;
        }

        private static EraClockPresentationSnapshot Snapshot(
            int era,
            int phase,
            long sequence,
            EraClockAnchorTarget anchorTarget = EraClockAnchorTarget.Center)
        {
            return new EraClockPresentationSnapshot(era, phase, sequence, anchorTarget);
        }

        private static void AssertTerminal(
            Rig rig,
            int era,
            int phase,
            EraClockAnchorTarget anchorTarget)
        {
            Assert.That(rig.Presenter.State, Is.EqualTo(EraClockPresenterState.Settled));
            Assert.That(rig.EraLabel.text, Is.EqualTo($"第{era}时代"));
            Assert.That(rig.PhaseLabel.text, Is.EqualTo($"{phase} / 8"));
            Assert.That(
                SignedAngle(rig.Pointer.localEulerAngles.z),
                Is.EqualTo(-(phase - 1) * 45f).Within(0.2f));
            Assert.That(rig.Progress.fillAmount, Is.EqualTo(phase / 8f).Within(0.001f));
            RectTransform anchor = anchorTarget == EraClockAnchorTarget.Center
                ? rig.CenterAnchor
                : rig.HudAnchor;
            Assert.That(Vector3.Distance(rig.ClockRoot.position, anchor.position), Is.LessThan(0.1f));
            Assert.That(rig.Pulse.alpha, Is.EqualTo(0f).Within(0.001f));
        }

        private static float SignedAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }

        private sealed class Rig
        {
            public Rig(
                EraClockPresenter presenter,
                RectTransform clockRoot,
                RawImage face,
                RawImage ring,
                RectTransform pointer,
                RawImage pointerImage,
                Image progress,
                Text eraLabel,
                Text phaseLabel,
                RectTransform centerAnchor,
                RectTransform hudAnchor,
                CanvasGroup rootGroup,
                CanvasGroup pulse)
            {
                Presenter = presenter;
                ClockRoot = clockRoot;
                Face = face;
                Ring = ring;
                Pointer = pointer;
                PointerImage = pointerImage;
                Progress = progress;
                EraLabel = eraLabel;
                PhaseLabel = phaseLabel;
                CenterAnchor = centerAnchor;
                HudAnchor = hudAnchor;
                RootGroup = rootGroup;
                Pulse = pulse;
            }

            public EraClockPresenter Presenter { get; }
            public RectTransform ClockRoot { get; }
            public RawImage Face { get; }
            public RawImage Ring { get; }
            public RectTransform Pointer { get; }
            public RawImage PointerImage { get; }
            public Image Progress { get; }
            public Text EraLabel { get; }
            public Text PhaseLabel { get; }
            public RectTransform CenterAnchor { get; }
            public RectTransform HudAnchor { get; }
            public CanvasGroup RootGroup { get; }
            public CanvasGroup Pulse { get; }
        }
    }
}
