namespace Services.Core
{
    public abstract class AggregateMetric : BaseMetric
    {
        public AggregatePeriod AggregatePeriod { get; }
        public abstract string AggregateBy { get; }

        public AggregateMetric(AggregatePeriod period)
        {
            AggregatePeriod = period;
        }
    }

    public enum AggregatePeriod
    {
        Hourly,
        Daily,
        Weekly,
        Monthly
    }
}
