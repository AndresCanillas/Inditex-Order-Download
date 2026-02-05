using System.Collections.Generic;
using System.Linq;

namespace Services.Core
{
	class LogServiceEntries
	{
		public bool Any() => LogEntries.Any() || MetricEntries.Any();
		public IEnumerable<Logs> LogEntries { get; set; } = Enumerable.Empty<Logs>();
		public IEnumerable<Metrics> MetricEntries { get; set; } = Enumerable.Empty<Metrics>();
	}
}
