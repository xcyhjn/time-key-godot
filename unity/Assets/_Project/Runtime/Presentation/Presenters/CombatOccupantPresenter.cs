using System;
using System.Collections.Generic;
using TimeKey.Domain;
using TimeKey.Presentation.Occupants;
using TimeKey.Presentation.Terrain;
using UnityEngine;

namespace TimeKey.Presentation.Presenters
{
    [DisallowMultipleComponent]
    public sealed class CombatOccupantPresenter : MonoBehaviour
    {
        [Serializable]
        private sealed class CreationViewRegistration
        {
            [SerializeField] private string creationId = string.Empty;
            [SerializeField] private GameObject prefab = null;

            public string CreationId => creationId;

            public GameObject Prefab => prefab;
        }

        [SerializeField] private Camera sceneCamera = null;
        [SerializeField] private List<CreationViewRegistration> creationViews =
            new List<CreationViewRegistration>();
        [SerializeField] private GameObject poisonStatusPrefab = null;

        private readonly Dictionary<HexCoord, HexTileColumn> _columns =
            new Dictionary<HexCoord, HexTileColumn>();
        private readonly Dictionary<string, CombatOccupantView> _views =
            new Dictionary<string, CombatOccupantView>(StringComparer.Ordinal);
        private readonly Dictionary<string, PoisonStatusView> _poisonStatuses =
            new Dictionary<string, PoisonStatusView>(StringComparer.Ordinal);

        public int OccupantViewCount => _views.Count;

        public int PoisonStatusCount => _poisonStatuses.Count;

        public void RegisterColumn(HexCoord coordinate, HexTileColumn column)
        {
            if (column == null)
            {
                throw new ArgumentNullException(nameof(column));
            }

            _columns[coordinate] = column;
        }

        public void RegisterExisting(
            CombatOccupantSnapshot occupant,
            CombatOccupantView view)
        {
            if (occupant == null)
            {
                throw new ArgumentNullException(nameof(occupant));
            }

            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            view.Initialize(occupant.RuntimeId);
            _views[occupant.RuntimeId] = view;
            ApplyStatus(occupant, view);
        }

        public void Apply(IReadOnlyList<OccupantEffectResult> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            ValidateConfiguration();
            for (var index = 0; index < results.Count; index++)
            {
                var result = results[index] ??
                    throw new ArgumentException("Occupant results cannot contain null.", nameof(results));
                if (result.After == null)
                {
                    continue;
                }

                var view = GetOrCreateView(result.After);
                ApplyStatus(result.After, view);
            }
        }

        public CombatOccupantView GetView(string runtimeId)
        {
            return !string.IsNullOrWhiteSpace(runtimeId) && _views.TryGetValue(runtimeId, out var view)
                ? view
                : null;
        }

        public PoisonStatusView GetPoisonStatus(string runtimeId)
        {
            return !string.IsNullOrWhiteSpace(runtimeId) &&
                   _poisonStatuses.TryGetValue(runtimeId, out var view)
                ? view
                : null;
        }

        private CombatOccupantView GetOrCreateView(CombatOccupantSnapshot occupant)
        {
            if (_views.TryGetValue(occupant.RuntimeId, out var existing))
            {
                return existing;
            }

            if (!_columns.TryGetValue(occupant.Coordinate, out var column))
            {
                throw new InvalidOperationException(
                    "No registered board column exists for occupant " + occupant.RuntimeId + ".");
            }

            var prefab = FindCreationPrefab(occupant.CreationId);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    "No occupant Prefab is registered for creation " + occupant.CreationId + ".");
            }

            var instance = Instantiate(prefab, column.OccupantAnchor, false);
            var view = instance.GetComponent<CombatOccupantView>();
            if (view == null)
            {
                throw new InvalidOperationException(prefab.name + " is missing CombatOccupantView.");
            }

            view.name = "Occupant-" + occupant.RuntimeId;
            view.Initialize(occupant.RuntimeId);
            foreach (var billboard in view.GetComponentsInChildren<CameraFacingBillboard>(true))
            {
                billboard.Initialize(sceneCamera);
            }

            _views.Add(occupant.RuntimeId, view);
            return view;
        }

        private GameObject FindCreationPrefab(string creationId)
        {
            for (var index = 0; index < creationViews.Count; index++)
            {
                var registration = creationViews[index];
                if (registration != null &&
                    string.Equals(registration.CreationId, creationId, StringComparison.Ordinal))
                {
                    return registration.Prefab;
                }
            }

            return null;
        }

        private void ApplyStatus(CombatOccupantSnapshot occupant, CombatOccupantView view)
        {
            if (occupant.PoisonStacks <= 0)
            {
                return;
            }

            if (!_poisonStatuses.TryGetValue(occupant.RuntimeId, out var status))
            {
                var statusObject = Instantiate(poisonStatusPrefab, view.StatusAnchor, false);
                status = statusObject.GetComponent<PoisonStatusView>();
                if (status == null)
                {
                    throw new InvalidOperationException(
                        poisonStatusPrefab.name + " is missing PoisonStatusView.");
                }

                status.name = "PoisonStatus-" + occupant.RuntimeId;
                var billboard = status.GetComponent<CameraFacingBillboard>();
                if (billboard != null)
                {
                    billboard.Initialize(sceneCamera);
                }

                _poisonStatuses.Add(occupant.RuntimeId, status);
            }

            status.SetStacks(occupant.PoisonStacks);
        }

        private void ValidateConfiguration()
        {
            if (sceneCamera == null)
            {
                throw new InvalidOperationException(name + " is missing its scene camera.");
            }

            if (poisonStatusPrefab == null)
            {
                throw new InvalidOperationException(name + " is missing its poison status Prefab.");
            }

            for (var index = 0; index < creationViews.Count; index++)
            {
                var registration = creationViews[index];
                if (registration == null ||
                    string.IsNullOrWhiteSpace(registration.CreationId) ||
                    registration.Prefab == null ||
                    registration.Prefab.GetComponent<CombatOccupantView>() == null)
                {
                    throw new InvalidOperationException(name + " has an incomplete creation Prefab registration.");
                }
            }

            if (poisonStatusPrefab.GetComponent<PoisonStatusView>() == null)
            {
                throw new InvalidOperationException(
                    poisonStatusPrefab.name + " is missing PoisonStatusView.");
            }
        }
    }
}
