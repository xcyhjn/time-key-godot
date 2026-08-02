using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TimeKey.Application.SceneFlow;
using TimeKey.Presentation.CombatShell;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TimeKey.Tests.PlayMode.CombatShell
{
    public sealed class CombatShellPresentationTests
    {
        private GameObject _root;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneInputLockState.SetLocked(false);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SceneInputLockState.SetLocked(false);
            if (_root != null)
            {
                Object.Destroy(_root);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator TopHud_AppliesSnapshotFontModalAndInputLocks()
        {
            var rig = CreateTopHudRig();
            rig.Presenter.Apply(new CombatTopHudDisplay(
                2, 3, 17, 7, 5, 1, 80, 100, "银 · 时钥行者", false));

            Assert.That(rig.Help.text, Is.EqualTo("帮助"));
            Assert.That(rig.Identity.text, Is.EqualTo("银 · 时钥行者"));
            Assert.That(rig.Round.text, Is.EqualTo("时代 2  /  阶段 3"));
            Assert.That(rig.Clock.text, Is.EqualTo("02 : 03"));
            Assert.That(rig.Timecoins.text, Is.EqualTo("时间币 17"));
            Assert.That(rig.DeckZones.text, Is.EqualTo("牌库 7  手牌 5  弃牌 1"));
            Assert.That(rig.EnemyHealth.text, Is.EqualTo("敌方总生命  80 / 100"));
            foreach (var text in _root.GetComponentsInChildren<Text>(true))
            {
                Assert.That(text.font, Is.EqualTo(rig.Silver), text.name);
            }

            Assert.That(rig.Pause.interactable, Is.True);
            Assert.That(rig.Settings.interactable, Is.True);
            rig.Pause.onClick.Invoke();
            Assert.That(rig.Presenter.IsModalOpen, Is.True);
            Assert.That(SceneInputLockState.IsLocked, Is.True);
            Assert.That(rig.ModalTitle.text, Is.EqualTo("战斗暂停"));
            Assert.That(rig.Pause.interactable, Is.False);
            rig.ModalClose.onClick.Invoke();
            Assert.That(rig.Presenter.IsModalOpen, Is.False);
            Assert.That(SceneInputLockState.IsLocked, Is.False);
            rig.Settings.onClick.Invoke();
            Assert.That(rig.ModalTitle.text, Is.EqualTo("战斗设置"));
            rig.ModalClose.onClick.Invoke();

            SceneInputLockState.SetLocked(true);
            rig.Pause.onClick.Invoke();
            rig.ModalClose.onClick.Invoke();
            Assert.That(SceneInputLockState.IsLocked, Is.True);
            SceneInputLockState.SetLocked(false);

            rig.Presenter.Apply(new CombatTopHudDisplay(
                2, 3, 17, 7, 5, 1, 80, 100, "银 · 观星者", false));
            Assert.That(rig.Identity.text, Is.EqualTo("银 · 观星者"));

            SceneInputLockState.SetLocked(true);
            yield return null;
            Assert.That(rig.Pause.interactable, Is.False);
            Assert.That(rig.Settings.interactable, Is.False);

            SceneInputLockState.SetLocked(false);
            rig.Presenter.Apply(new CombatTopHudDisplay(
                2, 3, 17, 7, 5, 1, 80, 100, "银 · 时钥行者", true));
            Assert.That(rig.Pause.interactable, Is.False);
            Assert.That(rig.Settings.interactable, Is.False);
        }

        [UnityTest]
        public IEnumerator Background_UsesSavedRendererSetForVisibility()
        {
            _root = new GameObject("CombatBackgroundRig");
            var background = _root.AddComponent<CombatBattleBackground>();
            var sea = CreateRenderer("Sea", _root.transform);
            var shallow = CreateRenderer("Shallow", _root.transform);
            var horizons = new[]
            {
                CreateRenderer("North", _root.transform),
                CreateRenderer("East", _root.transform),
                CreateRenderer("South", _root.transform),
                CreateRenderer("West", _root.transform)
            };
            SetField(background, "seaRenderer", sea);
            SetField(background, "shallowRenderer", shallow);
            SetField(background, "horizonRenderers", horizons);

            Assert.That(background.IsConfigured, Is.True);
            background.SetVisible(false);
            foreach (var renderer in _root.GetComponentsInChildren<Renderer>(true))
            {
                Assert.That(renderer.enabled, Is.False, renderer.name);
            }

            background.SetVisible(true);
            foreach (var renderer in _root.GetComponentsInChildren<Renderer>(true))
            {
                Assert.That(renderer.enabled, Is.True, renderer.name);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator Entrance_CompletesAtSavedAlphaAndScale()
        {
            _root = new GameObject("CombatShellEntranceRig");
            _root.SetActive(false);
            var topHud = new GameObject("TopHUD").AddComponent<CanvasGroup>();
            topHud.transform.SetParent(_root.transform, false);
            var background = new GameObject("Background").transform;
            background.SetParent(_root.transform, false);
            background.localScale = new Vector3(2f, 3f, 4f);
            var entrance = _root.AddComponent<CombatShellEntrancePresenter>();
            SetField(entrance, "topHud", topHud);
            SetField(entrance, "backgroundRoot", background);
            SetField(entrance, "duration", 0.1f);

            _root.SetActive(true);
            Assert.That(topHud.alpha, Is.EqualTo(0f));
            Assert.That(entrance.IsComplete, Is.False);

            var deadline = Time.realtimeSinceStartup + 1f;
            while (!entrance.IsComplete && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(entrance.IsComplete, Is.True);
            Assert.That(topHud.alpha, Is.EqualTo(1f));
            Assert.That(background.localScale, Is.EqualTo(new Vector3(2f, 3f, 4f)));
        }

        private TopHudRig CreateTopHudRig()
        {
            _root = new GameObject("CombatTopHudRig");
            _root.SetActive(false);
            var presenter = _root.AddComponent<CombatTopHudPresenter>();
            var silver = Resources.Load<Font>("Fonts/Silver");
            Assert.That(silver, Is.Not.Null);

            var help = CreateText("Help", _root.transform);
            var identity = CreateText("Identity", _root.transform);
            var round = CreateText("Round", _root.transform);
            var clock = CreateText("Clock", _root.transform);
            var timecoins = CreateText("Timecoins", _root.transform);
            var deckZones = CreateText("DeckZones", _root.transform);
            var enemyHealth = CreateText("EnemyHealth", _root.transform);
            var pause = CreateButton("Pause", _root.transform);
            var settings = CreateButton("Settings", _root.transform);
            var modal = new GameObject("Modal").AddComponent<CanvasGroup>();
            modal.transform.SetParent(_root.transform, false);
            var modalTitle = CreateText("ModalTitle", modal.transform);
            var modalBody = CreateText("ModalBody", modal.transform);
            var modalClose = CreateButton("ModalClose", modal.transform);

            SetField(presenter, "helpLabel", help);
            SetField(presenter, "identityLabel", identity);
            SetField(presenter, "roundLabel", round);
            SetField(presenter, "clockLabel", clock);
            SetField(presenter, "timecoinsLabel", timecoins);
            SetField(presenter, "deckZonesLabel", deckZones);
            SetField(presenter, "enemyHealthLabel", enemyHealth);
            SetField(presenter, "pauseButton", pause);
            SetField(presenter, "settingsButton", settings);
            SetField(presenter, "modalGroup", modal);
            SetField(presenter, "modalTitle", modalTitle);
            SetField(presenter, "modalBody", modalBody);
            SetField(presenter, "modalCloseButton", modalClose);
            SetField(presenter, "silverFont", silver);
            _root.SetActive(true);

            return new TopHudRig(
                presenter,
                help,
                identity,
                round,
                clock,
                timecoins,
                deckZones,
                enemyHealth,
                pause,
                settings,
                modalTitle,
                modalClose,
                silver);
        }

        private static Text CreateText(string name, Transform parent)
        {
            var target = new GameObject(name, typeof(RectTransform));
            target.transform.SetParent(parent, false);
            return target.AddComponent<Text>();
        }

        private static Button CreateButton(string name, Transform parent)
        {
            var target = new GameObject(name, typeof(RectTransform), typeof(Image));
            target.transform.SetParent(parent, false);
            var button = target.AddComponent<Button>();
            button.targetGraphic = target.GetComponent<Image>();
            CreateText("Label", target.transform);
            return button;
        }

        private static MeshRenderer CreateRenderer(string name, Transform parent)
        {
            var target = new GameObject(name);
            target.transform.SetParent(parent, false);
            return target.AddComponent<MeshRenderer>();
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing serialized field " + fieldName + ".");
            field.SetValue(target, value);
        }

        private sealed class TopHudRig
        {
            public TopHudRig(
                CombatTopHudPresenter presenter,
                Text help,
                Text identity,
                Text round,
                Text clock,
                Text timecoins,
                Text deckZones,
                Text enemyHealth,
                Button pause,
                Button settings,
                Text modalTitle,
                Button modalClose,
                Font silver)
            {
                Presenter = presenter;
                Help = help;
                Identity = identity;
                Round = round;
                Clock = clock;
                Timecoins = timecoins;
                DeckZones = deckZones;
                EnemyHealth = enemyHealth;
                Pause = pause;
                Settings = settings;
                ModalTitle = modalTitle;
                ModalClose = modalClose;
                Silver = silver;
            }

            public CombatTopHudPresenter Presenter { get; }
            public Text Help { get; }
            public Text Identity { get; }
            public Text Round { get; }
            public Text Clock { get; }
            public Text Timecoins { get; }
            public Text DeckZones { get; }
            public Text EnemyHealth { get; }
            public Button Pause { get; }
            public Button Settings { get; }
            public Text ModalTitle { get; }
            public Button ModalClose { get; }
            public Font Silver { get; }
        }
    }
}
