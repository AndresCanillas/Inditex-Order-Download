using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Services.Core
{
	public class LogServiceMetricEntry
	{
		public string MetricType { get; set; } = string.Empty;
		public string EnvironmentName { get; set; } = string.Empty;
		public string MachineName { get; set; } = string.Empty;
		public string ComponentName { get; set; } = string.Empty;
		public string InstanceName { get; set; } = string.Empty;
		public DateTime DateUtc { get; set; }
		public double Value { get; set; }
        public bool IsAggregate { get; set; }
        public string AggregateBy { get; set; }
        public AggregatePeriod AggregatePeriod { get; set; }
        public Dictionary<string, string> ExtraProperties { get; set; } = new Dictionary<string, string>();

        public string Properties => JsonConvert.SerializeObject(ExtraProperties);
    }
}
