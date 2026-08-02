using System;
using System.Linq;
using TimeKey.Application.SceneFlow;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TimeKey.Presentation.OutOfBattleShell
{
    public sealed class OutOfBattleRoomConfirmationRequest
    {
        public OutOfBattleRoomConfirmationRequest(string roomId)
        {
            if (string.IsNullOrWhiteSpace(roomId))
            {
                throw new ArgumentException(
                    "A stable room identity is required.",
                    nameof(roomId));
            }

            RoomId = roomId;
        }

        public string RoomId { get; }
    }

    [DisallowMultipleComponent]
    public sealed class OutOfBattleShellPresenter : MonoBehaviour
    {
        [SerializeField] private Text seedLabel = null;
        [SerializeField] private Text eraLabel = null;
        [SerializeField] private Text phaseLabel = null;
        [SerializeField] private Text timecoinsLabel = null;
        [SerializeField] private OutOfBattleRoomView roomView = null;
        [SerializeField] private Button confirmButton = null;
        [SerializeField] private Button cancelButton = null;
        [SerializeField] private Font silverFont = null;
        [SerializeField] private string roomId = "combat-room-01";
        [SerializeField] private string roomTitle = "时隙战场";

        private bool _bound;
        private bool _hasSnapshot;
        private bool _selected;
        private bool _confirmationPending;
        private bool _settled;
        private bool _transitionLocked;

        public event Action<OutOfBattleRoomConfirmationRequest>
            RoomConfirmationRequested;

        public string RoomId => roomId;

        public bool IsSelected => _selected;

        public bool IsConfirmationPending => _confirmationPending;

        public OutOfBattleRoomState RoomState => roomView == null
            ? OutOfBattleRoomState.Disabled
            : roomView.State;

        private void OnEnable()
        {
            if (!DependenciesAssigned())
            {
                return;
            }

            Bind();
            roomView.Configure(roomTitle);
            ApplySilverFont();
            RefreshInputState();
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HandleEscape();
            }

            if (_bound)
            {
                RefreshInputState();
            }
        }

        public void Apply(OutOfBattleShellState snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            ValidateDependencies();
            seedLabel.text = "种子 " + snapshot.RunSeed;
            eraLabel.text = "时代 " + snapshot.Era;
            phaseLabel.text = "阶段 " + snapshot.Phase;
            timecoinsLabel.text = "时间币 " + snapshot.Timecoins;
            _hasSnapshot = true;
            _selected = false;
            _confirmationPending = false;
            _settled = snapshot.SettledRoomIds.Contains(roomId);
            roomView.Configure(roomTitle);
            ApplySilverFont();
            RefreshInputState();
        }

        public void SetRoomIdentity(string stableRoomId, string displayTitle)
        {
            if (string.IsNullOrWhiteSpace(stableRoomId))
            {
                throw new ArgumentException(
                    "A stable room identity is required.",
                    nameof(stableRoomId));
            }

            if (_hasSnapshot)
            {
                throw new InvalidOperationException(
                    "Room identity must be configured before applying a snapshot.");
            }

            roomId = stableRoomId;
            roomTitle = string.IsNullOrWhiteSpace(displayTitle)
                ? "战斗房间"
                : displayTitle;
            roomView?.Configure(roomTitle);
        }

        public void SetTransitionLocked(bool locked)
        {
            _transitionLocked = locked;
            RefreshInputState();
        }

        public bool RejectPendingConfirmation()
        {
            if (!_confirmationPending || _settled)
            {
                return false;
            }

            _confirmationPending = false;
            _selected = true;
            RefreshInputState();
            confirmButton.Select();
            return true;
        }

        public bool HandleEscape()
        {
            if (!_selected || _confirmationPending || _settled ||
                _transitionLocked || SceneInputLockState.IsLocked)
            {
                return false;
            }

            _selected = false;
            if (EventSystem.current != null &&
                (EventSystem.current.currentSelectedGameObject == confirmButton.gameObject ||
                 EventSystem.current.currentSelectedGameObject == cancelButton.gameObject))
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            RefreshInputState();
            return true;
        }

        public void RefreshInputState()
        {
            if (!DependenciesAssigned())
            {
                return;
            }

            var locked = !_hasSnapshot || _transitionLocked ||
                SceneInputLockState.IsLocked;
            roomView.SetSettled(_settled);
            roomView.SetSelected(_selected);
            roomView.SetConfirming(_confirmationPending);
            roomView.SetInteractionLocked(locked);
            confirmButton.interactable = !locked && _selected &&
                !_confirmationPending && !_settled;
            cancelButton.interactable = !locked && _selected &&
                !_confirmationPending && !_settled;
        }

        private void Bind()
        {
            if (_bound)
            {
                return;
            }

            roomView.SelectionRequested += OnRoomSelectionRequested;
            confirmButton.onClick.AddListener(OnConfirm);
            cancelButton.onClick.AddListener(OnCancel);
            _bound = true;
        }

        private void Unbind()
        {
            if (!_bound)
            {
                return;
            }

            roomView.SelectionRequested -= OnRoomSelectionRequested;
            confirmButton.onClick.RemoveListener(OnConfirm);
            cancelButton.onClick.RemoveListener(OnCancel);
            _bound = false;
        }

        private void OnRoomSelectionRequested()
        {
            if (!CanInteract() || _confirmationPending)
            {
                return;
            }

            _selected = true;
            RefreshInputState();
            confirmButton.Select();
        }

        private void OnConfirm()
        {
            if (!CanInteract() || !_selected || _confirmationPending)
            {
                return;
            }

            _confirmationPending = true;
            RefreshInputState();
            RoomConfirmationRequested?.Invoke(
                new OutOfBattleRoomConfirmationRequest(roomId));
        }

        private void OnCancel()
        {
            HandleEscape();
        }

        private bool CanInteract()
        {
            return _hasSnapshot && !_settled && !_transitionLocked &&
                !SceneInputLockState.IsLocked;
        }

        private void ApplySilverFont()
        {
            foreach (var text in GetComponentsInChildren<Text>(true))
            {
                text.font = silverFont;
            }

            roomView.ApplyFont(silverFont);
        }

        private bool DependenciesAssigned()
        {
            return seedLabel != null && eraLabel != null && phaseLabel != null &&
                timecoinsLabel != null && roomView != null && confirmButton != null &&
                cancelButton != null && silverFont != null &&
                !string.IsNullOrWhiteSpace(roomId);
        }

        private void ValidateDependencies()
        {
            if (!DependenciesAssigned())
            {
                throw new InvalidOperationException(
                    name + " is missing an out-of-battle presentation reference.");
            }
        }
    }
}
