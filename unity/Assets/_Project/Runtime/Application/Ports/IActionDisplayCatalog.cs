using TimeKey.Domain;

namespace TimeKey.Application
{
    public interface IActionDisplayCatalog
    {
        TimelineActionDisplayPayload GetDisplay(TimelineAction action);
    }

    internal sealed class StableIdActionDisplayCatalog : IActionDisplayCatalog
    {
        public static StableIdActionDisplayCatalog Instance { get; } =
            new StableIdActionDisplayCatalog();

        private StableIdActionDisplayCatalog()
        {
        }

        public TimelineActionDisplayPayload GetDisplay(TimelineAction action)
        {
            return new TimelineActionDisplayPayload(action.CardId, string.Empty, action.CardId);
        }
    }
}
