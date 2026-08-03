using System;
using System.Linq;
using TimeKey.Application.SceneFlow;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TimeKey.Domain.OverworldMovement;

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

    public sealed class OutOfBattleRoomInteractionState
    {
        public OutOfBattleRoomInteractionState(
            string roomId,
            bool selected,
            bool confirming)
        {
            RoomId = roomId ?? string.Empty;
            Selected = selected;
            Confirming = confirming;
        }

        public string RoomId { get; }
        public bool Selected { get; }
        public bool Confirming { get; }
    }

    [DisallowMultipleComponent]
    public sealed class OutOfBattleShellPresenter : MonoBehaviour
    {
        [SerializeField] private Text seedLabel = null;
        [SerializeField] private Text eraLabel = null;
        [SerializeField] private Text phaseLabel = null;
        [SerializeField] private Text timecoinsLabel = null;
        [SerializeField] private OutOfBattleRoomView roomView = null;
        [SerializeField] private Text roomDetailLabel = null;
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
        private bool _roomAvailable = true;
        private bool _confirmAvailable = true;
        private bool _transitionLocked;
        private string _roomDetail = "选择房间后确认进入";
        private string _confirmText = "进入";
        private string _cancelText = "返回";
        private OutOfBattleShellState _snapshot;

        public event Action<OutOfBattleRoomConfirmationRequest>
            RoomConfirmationRequested;
        public event Action<OutOfBattleRoomInteractionState>
            RoomInteractionStateChanged;

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
            _snapshot = snapshot;
            _hasSnapshot = true;
            _selected = false;
            _confirmationPending = false;
            _settled = snapshot.SettledRoomIds.Contains(roomId);
            _roomAvailable = !string.IsNullOrWhiteSpace(roomId);
            _confirmAvailable = _roomAvailable;
            roomView.Configure(roomTitle);
            ApplySilverFont();
            RefreshInputState();
        }

        public void SetCurrentRoomIdentity(MapNodeId stableNodeId, string displayTitle)
        {
            SetCurrentRoomPresentation(
                stableNodeId,
                displayTitle,
                "确认后进入该房间",
                "进入",
                confirmAvailable: true);
        }

        public void SetCurrentRoomPresentation(
            MapNodeId stableNodeId,
            string displayTitle,
            string detail,
            string confirmText,
            bool confirmAvailable)
        {
            roomId = stableNodeId.Value;
            roomTitle = string.IsNullOrWhiteSpace(displayTitle)
                ? "战斗房间"
                : displayTitle;
            _selected = false;
            _confirmationPending = false;
            _settled = _snapshot != null && _snapshot.IsRoomSettled(roomId);
            _roomAvailable = true;
            _confirmAvailable = confirmAvailable;
            _roomDetail = detail ?? string.Empty;
            _confirmText = string.IsNullOrWhiteSpace(confirmText) ? "确认" : confirmText;
            _cancelText = "返回";
            roomView.Configure(roomTitle);
            RefreshInputState();
        }

        public void SetRoomAvailable(bool available)
        {
            _roomAvailable = available;
            _confirmAvailable = available;
            if (!available)
            {
                _selected = false;
                _confirmationPending = false;
            }

            RefreshInputState();
        }

        public void ShowResolvedRoom(string detail)
        {
            _selected = false;
            _confirmationPending = false;
            _settled = true;
            _roomAvailable = false;
            _confirmAvailable = false;
            _roomDetail = detail ?? string.Empty;
            RefreshInputState();
            PublishInteractionState();
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
            (_confirmAvailable ? confirmButton : cancelButton).Select();
            PublishInteractionState();
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
            PublishInteractionState();
            return true;
        }

        public void RefreshInputState()
        {
            if (!DependenciesAssigned())
            {
                return;
            }

            var locked = !_hasSnapshot || _transitionLocked ||
                !_roomAvailable || SceneInputLockState.IsLocked;
            roomView.SetSettled(_settled);
            roomView.SetSelected(_selected);
            roomView.SetConfirming(_confirmationPending);
            roomView.SetInteractionLocked(locked);
            confirmButton.interactable = !locked && _selected &&
                !_confirmationPending && !_settled && _confirmAvailable;
            cancelButton.interactable = !locked && _selected &&
                !_confirmationPending && !_settled;
            if (roomDetailLabel != null)
            {
                roomDetailLabel.text = _roomDetail;
            }

            SetButtonText(confirmButton, _confirmText);
            SetButtonText(cancelButton, _cancelText);
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
            PublishInteractionState();
        }

        private void OnConfirm()
        {
            if (!CanInteract() || !_selected || _confirmationPending || !_confirmAvailable)
            {
                return;
            }

            _confirmationPending = true;
            RefreshInputState();
            PublishInteractionState();
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

        private void PublishInteractionState()
        {
            RoomInteractionStateChanged?.Invoke(new OutOfBattleRoomInteractionState(
                roomId,
                _selected,
                _confirmationPending));
        }

        private static void SetButtonText(Button button, string value)
        {
            var text = button == null ? null : button.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.text = value;
            }
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
