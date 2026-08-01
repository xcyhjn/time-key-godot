using System;
using UnityEngine;

namespace TimeKey.Presentation.Occupants
{
    [DisallowMultipleComponent]
    public sealed class CombatOccupantView : MonoBehaviour
    {
        [SerializeField] private Transform statusAnchor = null;
        [SerializeField] private TextMesh healthLabel = null;

        public string RuntimeId { get; private set; }

        public Transform StatusAnchor => statusAnchor;

        public int Hp { get; private set; }

        public bool HasHealth { get; private set; }

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

        public void SetHealth(int hp)
        {
            if (hp < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(hp));
            }

            HasHealth = true;
            Hp = hp;
            if (healthLabel != null)
            {
                healthLabel.text = hp.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }
    }
}
