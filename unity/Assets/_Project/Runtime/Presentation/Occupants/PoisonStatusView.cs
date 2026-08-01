using System;
using UnityEngine;

namespace TimeKey.Presentation.Occupants
{
    [DisallowMultipleComponent]
    public sealed class PoisonStatusView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer icon = null;
        [SerializeField] private TextMesh stackLabel = null;

        public int Stacks { get; private set; }

        public void SetStacks(int stacks)
        {
            if (stacks <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stacks));
            }

            if (icon == null || stackLabel == null)
            {
                throw new InvalidOperationException(name + " is missing its poison status references.");
            }

            Stacks = stacks;
            icon.enabled = true;
            stackLabel.text = stacks.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
