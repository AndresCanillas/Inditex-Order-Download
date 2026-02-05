using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Contracts.WF
{
	public class TaskResult
	{
        private static readonly TaskResult okResult = new TaskResult() { Status = TaskStatus.OK };
        private static readonly TaskResult waitResult = new TaskResult() { Status = TaskStatus.Wait };
        private static readonly TaskResult skipWaitResult = new TaskResult() { Status = TaskStatus.SkipWait };
        private static readonly TaskResult throwResult = new TaskResult() { Status = TaskStatus.Throw };
        private static readonly TaskResult reenqueueResult = new TaskResult() { Status = TaskStatus.ReEnqueue };

        public TaskStatus Status { get; set; } = TaskStatus.Rejected;
		public string Reason { get; set; }
		public string RouteCode { get; set; }
		public TimeSpan DelayTime { get; set; }
		public Exception Exception { get; set; }

        public static TaskResult OK() => okResult;

		public static TaskResult OK(string routeCode)
		{
			return new TaskResult()
			{
				Status = TaskStatus.OK,
				RouteCode = routeCode
			};
		}

		public static TaskResult Delay(string reason)
		{
			return new TaskResult()
			{
				Status = TaskStatus.Delayed,
				Reason = reason,
				DelayTime = TimeSpan.FromMinutes(1)
			};
		}

		public static TaskResult Delay(string reason, TimeSpan time)
		{
			// Restrict delay to a value between 1 second and 1 day
			if (time.TotalMilliseconds < 1000)
				time = TimeSpan.FromSeconds(1); 
			if (time.TotalHours > 24)
				time = TimeSpan.FromHours(24);

			return new TaskResult()
			{
				Status = TaskStatus.Delayed,
				Reason = reason,
				DelayTime = time
			};
		}


		public static TaskResult Delay(string reason, Exception exception, TimeSpan time)
		{
			// Restrict delay to a value between 5 seconds and 1 day
			if (time.TotalMilliseconds < 5000)
				time = TimeSpan.FromSeconds(5);
			if (time.TotalHours > 24)
				time = TimeSpan.FromHours(24);

			return new TaskResult()
			{
				Status = TaskStatus.Delayed,
				Reason = reason,
				DelayTime = time,
				Exception = exception
			};
		}

		public static TaskResult Reject(string reason)
		{
			return new TaskResult()
			{
				Status = TaskStatus.Rejected,
				Reason = reason
			};
		}

		public static TaskResult Reject(string reason, Exception exception)
		{
			return new TaskResult()
			{
				Status = TaskStatus.Rejected,
				Reason = reason,
				Exception = exception
			};
		}

		public static TaskResult Cancel(string reason)
		{
			return new TaskResult()
			{
				Status = TaskStatus.Cancelled,
				Reason = reason
			};
		}

		public static TaskResult Complete(string reason)
		{
			return new TaskResult()
			{
				Status = TaskStatus.Completed,
				Reason = reason,
			};
		}


        public static TaskResult Wait() => waitResult;

        public static TaskResult SkipWait() => skipWaitResult;

        public static TaskResult Throw() => throwResult;

        public static TaskResult ReEnqueue() => reenqueueResult;
	}
}
