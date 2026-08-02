using System;
using System.Collections.Generic;
using TimeKey.Application.SceneFlow;
using UnityEngine;

namespace TimeKey.Composition.SceneFlow
{
    [Serializable]
    public sealed class SceneRouteEntry
    {
        [SerializeField] private SceneId sceneId = SceneId.None;
        [SerializeField] private string sceneName = string.Empty;

        public SceneId SceneId => sceneId;

        public string SceneName => sceneName;
    }

    [CreateAssetMenu(menuName = "Time Key/Scene Flow/Route Catalog")]
    public sealed class SceneRouteCatalog : ScriptableObject
    {
        [SerializeField] private List<SceneRouteEntry> entries =
            new List<SceneRouteEntry>();

        public string GetSceneName(SceneId sceneId)
        {
            for (var index = 0; index < entries.Count; index++)
            {
                if (entries[index].SceneId == sceneId &&
                    !string.IsNullOrWhiteSpace(entries[index].SceneName))
                {
                    return entries[index].SceneName;
                }
            }

            throw new InvalidOperationException("No scene route is registered for " + sceneId + ".");
        }
    }
}
