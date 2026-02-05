namespace Services.Core
{
    public class ActionExecutionTimeMetric : AggregateMetric
    {
        public ActionExecutionTimeMetric()
            : base(AggregatePeriod.Hourly)
        {
        }

        public override string AggregateBy => nameof(ActionName);
        public string ActionName { get; set; } = string.Empty;
    }
}
