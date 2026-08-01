using System;
using UnityEngine;

namespace TimeKey.Presentation.Occupants
{
    [DisallowMultipleComponent]
    public sealed class CombatOccupantView : MonoBehaviour
    {
        [SerializeField] private Transform statusAnchor = null;

        public string RuntimeId { get; private set; }

        public Transform StatusAnchor => statusAnchor;

        public void Initialize(string runtimeId)
        {
            if (string.IsNullOrWhiteSpace(runtimeId))
            {
                throw new ArgumentException("A runtime occupant ID is required.", nameof(runtimeId));
            }

            if (statusAnchor == null)
            {
                throw new InvalidOperationException(name + " is missing its status anchor.");
            }

            RuntimeId = runtimeId.Trim();
        }
    }
}
