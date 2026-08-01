namespace TimeKey.Domain
{
    public interface ITimelineActionProcessor
    {
        TurnLifecycleStepResult Resolve(
            TurnLifecycleContext context,
            TimelineActionPlanEntry action);
    }

    public interface IBuildingBehaviorProcessor
    {
        TurnLifecycleStepResult Run(TurnLifecycleContext context);
    }

    public interface ITimelineClearingProcessor
    {
        TurnLifecycleStepResult Clear(TurnLifecycleContext context);
    }

    public interface ITurnStartStatusProcessor
    {
        TurnLifecycleStepResult Process(TurnLifecycleContext context);
    }

    public interface INextTurnHook
    {
        TurnLifecycleStepResult Run(TurnLifecycleContext context);
    }

    public interface IEnemyIntentRefresher
    {
        TurnLifecycleStepResult Refresh(TurnLifecycleContext context);
    }

    internal sealed class NoOpTurnLifecyclePorts :
        ITimelineActionProcessor,
        IBuildingBehaviorProcessor,
        ITimelineClearingProcessor,
        ITurnStartStatusProcessor,
        INextTurnHook,
        IEnemyIntentRefresher
    {
        public static NoOpTurnLifecyclePorts Instance { get; } = new NoOpTurnLifecyclePorts();

        private NoOpTurnLifecyclePorts()
        {
        }

        public TurnLifecycleStepResult Resolve(
            TurnLifecycleContext context,
            TimelineActionPlanEntry action)
        {
            return TurnLifecycleStepResult.Successful;
        }

        public TurnLifecycleStepResult Run(TurnLifecycleContext context)
        {
            return TurnLifecycleStepResult.Successful;
        }

        public TurnLifecycleStepResult Clear(TurnLifecycleContext context)
        {
            return TurnLifecycleStepResult.Successful;
        }

        public TurnLifecycleStepResult Process(TurnLifecycleContext context)
        {
            return TurnLifecycleStepResult.Successful;
        }

        public TurnLifecycleStepResult Refresh(TurnLifecycleContext context)
        {
            return TurnLifecycleStepResult.Successful;
        }
    }
}
