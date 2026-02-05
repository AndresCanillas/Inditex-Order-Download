namespace Services.Core
{
    public class WebRequestTimingMetric : AggregateMetric
    {
        public WebRequestTimingMetric()
            : base(AggregatePeriod.Hourly)
        {

        }

        public override string AggregateBy => nameof(Url);
        public string Url { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string User { get; set; }
        public int StatusCode { get; set; }
    }
}
