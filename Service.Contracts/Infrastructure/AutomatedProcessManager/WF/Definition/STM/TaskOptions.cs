using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.WF
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class TaskOptionsAttribute : Attribute
	{
		public int MaxExecutionThreads = 2;
		public int MaxRetriesBeforeReject = 5;
		public TimeSpan RetryDelayTime = TimeSpan.FromSeconds(60);

		public TaskOptionsAttribute()
		{
		}

		public TaskOptionsAttribute(int maxExecutionThreads, int maxRetriesBeforeReject = 5, int retryDelayTimeInSeconds = 60)
		{
			MaxExecutionThreads = maxExecutionThreads;
			MaxRetriesBeforeReject = maxRetriesBeforeReject;
			RetryDelayTime = TimeSpan.FromSeconds(retryDelayTimeInSeconds);
		}

		internal void Validate()
		{
			if (MaxExecutionThreads < 1 || MaxExecutionThreads > 5)
				throw new InvalidOperationException("MaxExecutionThreads must be in the range [1, 5]");

			if (MaxRetriesBeforeReject < 1 || MaxRetriesBeforeReject > 100)
				throw new InvalidOperationException("MaxRetriesBeforeReject must be in the range [1, 100]");

			if (RetryDelayTime.TotalSeconds < 1 || RetryDelayTime.TotalSeconds > 3600)
				throw new InvalidOperationException("RetryDelayTime must be in the range [1, 3600] seconds");
		}
	}
}