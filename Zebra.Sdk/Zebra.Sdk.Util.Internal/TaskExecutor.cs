using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Zebra.Sdk.Util.Internal
{
	internal class TaskExecutor
	{
		private TaskExecutor()
		{
		}

		public static void Execute(Task[] tasks, int numConcurrentThreads)
		{
			List<Task> tasks1 = new List<Task>();
			int num = 0;
			while (num < (int)tasks.Length || tasks1.Count > 0)
			{
				if (tasks1.Count < numConcurrentThreads && num < (int)tasks.Length)
				{
					Task task = tasks[num];
					num++;
					tasks1.Add(task);
					task.Start();
				}
				for (int i = 0; i < tasks1.Count; i++)
				{
					if (tasks1[i].IsCompleted)
					{
						tasks1.RemoveAt(i);
					}
				}
			}
		}
	}
}