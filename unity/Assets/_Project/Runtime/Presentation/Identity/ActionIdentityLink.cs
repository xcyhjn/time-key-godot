using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TimeKey.Domain;
using TimeKey.Domain.Deck;

namespace TimeKey.Presentation.Identity
{
    public sealed class ActionIdentityLink
    {
        public ActionIdentityLink(
            TimelineActionIdentity actionId,
            string cardStableId = null,
            CardInstanceId? cardInstanceId = null,
            string sourceRuntimeId = null,
            string targetRuntimeId = null)
        {
            if (!actionId.IsValid)
            {
                throw new ArgumentException("A valid action identity is required.", nameof(actionId));
            }

            ActionId = actionId;
            CardStableId = Optional(cardStableId);
            CardInstanceId = cardInstanceId;
            SourceRuntimeId = Optional(sourceRuntimeId);
            TargetRuntimeId = Optional(targetRuntimeId);
        }

        public TimelineActionIdentity ActionId { get; }

        public string CardStableId { get; }

        public CardInstanceId? CardInstanceId { get; }

        public string SourceRuntimeId { get; }

        public string TargetRuntimeId { get; }

        private static string Optional(string value)
        {
            return value == null ? null : value.Trim();
        }
    }

    public sealed class ActionIdentityIndex
    {
        private readonly Dictionary<TimelineActionIdentity, ActionIdentityLink> _byAction =
            new Dictionary<TimelineActionIdentity, ActionIdentityLink>();
        private readonly Dictionary<CardInstanceId, TimelineActionIdentity> _byCardInstance =
            new Dictionary<CardInstanceId, TimelineActionIdentity>();
        private readonly Dictionary<string, List<TimelineActionIdentity>> _byStableId =
            new Dictionary<string, List<TimelineActionIdentity>>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<TimelineActionIdentity>> _byMapRuntimeId =
            new Dictionary<string, List<TimelineActionIdentity>>(StringComparer.Ordinal);

        public int Count => _byAction.Count;

        public void Add(ActionIdentityLink link)
        {
            if (link == null)
            {
                throw new ArgumentNullException(nameof(link));
            }

            if (_byAction.ContainsKey(link.ActionId))
            {
                throw new InvalidOperationException("Action identity is already indexed.");
            }

            if (link.CardInstanceId.HasValue &&
                _byCardInstance.ContainsKey(link.CardInstanceId.Value))
            {
                throw new InvalidOperationException("Card instance is already mapped to an action.");
            }

            _byAction.Add(link.ActionId, link);
            if (link.CardInstanceId.HasValue)
            {
                _byCardInstance.Add(link.CardInstanceId.Value, link.ActionId);
            }

            AddTo(_byStableId, link.CardStableId, link.ActionId);
            AddTo(_byMapRuntimeId, link.SourceRuntimeId, link.ActionId);
            AddTo(_byMapRuntimeId, link.TargetRuntimeId, link.ActionId);
        }

        public bool TryGetAction(TimelineActionIdentity actionId, out ActionIdentityLink link)
        {
            return _byAction.TryGetValue(actionId, out link);
        }

        public bool TryGetAction(CardInstanceId cardInstanceId, out ActionIdentityLink link)
        {
            if (_byCardInstance.TryGetValue(cardInstanceId, out var actionId))
            {
                return _byAction.TryGetValue(actionId, out link);
            }

            link = null;
            return false;
        }

        public IReadOnlyList<TimelineActionIdentity> FindByStableId(string stableId)
        {
            return CopyIds(_byStableId, stableId);
        }

        public IReadOnlyList<TimelineActionIdentity> FindByMapRuntimeId(string runtimeId)
        {
            return CopyIds(_byMapRuntimeId, runtimeId);
        }

        public bool Remove(TimelineActionIdentity actionId)
        {
            if (!_byAction.TryGetValue(actionId, out var link))
            {
                return false;
            }

            _byAction.Remove(actionId);
            if (link.CardInstanceId.HasValue)
            {
                _byCardInstance.Remove(link.CardInstanceId.Value);
            }

            RemoveFrom(_byStableId, link.CardStableId, actionId);
            RemoveFrom(_byMapRuntimeId, link.SourceRuntimeId, actionId);
            RemoveFrom(_byMapRuntimeId, link.TargetRuntimeId, actionId);
            return true;
        }

        public void Clear()
        {
            _byAction.Clear();
            _byCardInstance.Clear();
            _byStableId.Clear();
            _byMapRuntimeId.Clear();
        }

        private static void AddTo(
            Dictionary<string, List<TimelineActionIdentity>> index,
            string key,
            TimelineActionIdentity actionId)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            if (!index.TryGetValue(key, out var values))
            {
                values = new List<TimelineActionIdentity>();
                index.Add(key, values);
            }

            values.Add(actionId);
        }

        private static void RemoveFrom(
            Dictionary<string, List<TimelineActionIdentity>> index,
            string key,
            TimelineActionIdentity actionId)
        {
            if (string.IsNullOrWhiteSpace(key) || !index.TryGetValue(key, out var values))
            {
                return;
            }

            values.Remove(actionId);
            if (values.Count == 0)
            {
                index.Remove(key);
            }
        }

        private static IReadOnlyList<TimelineActionIdentity> CopyIds(
            Dictionary<string, List<TimelineActionIdentity>> index,
            string key)
        {
            if (string.IsNullOrWhiteSpace(key) || !index.TryGetValue(key, out var values))
            {
                return Array.Empty<TimelineActionIdentity>();
            }

            return new ReadOnlyCollection<TimelineActionIdentity>(
                new List<TimelineActionIdentity>(values));
        }
    }
}
