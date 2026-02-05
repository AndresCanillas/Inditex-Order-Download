using System;

namespace Services.Core
{
	public abstract class BaseMetric
	{
		public string EnvironmentName { get; set; } = string.Empty;
		public string MachineName { get; set; } = string.Empty;
		public string ComponentName { get; set; } = string.Empty;
		public string InstanceName { get; set; } = string.Empty;
		public DateTime DateUtc { get; set; } = DateTime.UtcNow;
		public double Value { get; set; }
	}
}
