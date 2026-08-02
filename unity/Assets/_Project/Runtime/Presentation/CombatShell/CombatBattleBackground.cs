using System;
using UnityEngine;

namespace TimeKey.Presentation.CombatShell
{
    [DisallowMultipleComponent]
    public sealed class CombatBattleBackground : MonoBehaviour
    {
        [SerializeField] private Renderer seaRenderer = null;
        [SerializeField] private Renderer shallowRenderer = null;
        [SerializeField] private Renderer[] horizonRenderers = Array.Empty<Renderer>();

        public Renderer SeaRenderer => seaRenderer;

        public Renderer ShallowRenderer => shallowRenderer;

        public Renderer[] HorizonRenderers => horizonRenderers;

        public bool IsConfigured => seaRenderer != null && shallowRenderer != null &&
            horizonRenderers != null && horizonRenderers.Length == 4;

        public void SetVisible(bool visible)
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException(
                    name + " is missing a saved background renderer reference.");
            }

            seaRenderer.enabled = visible;
            shallowRenderer.enabled = visible;
            for (var index = 0; index < horizonRenderers.Length; index++)
            {
                if (horizonRenderers[index] == null)
                {
                    throw new InvalidOperationException(
                        name + " contains an empty horizon renderer reference.");
                }

                horizonRenderers[index].enabled = visible;
            }
        }
    }
}
