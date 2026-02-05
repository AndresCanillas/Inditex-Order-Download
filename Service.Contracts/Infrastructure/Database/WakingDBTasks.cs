using Services.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Service.Contracts.Database
{
	public class WakingDBTasks
	{
		private object syncObj = new object();
		private ManualResetEvent waitHandle = new ManualResetEvent(false);
		private bool running;
		private ConcurrentQueue<DBOperation> queue = new ConcurrentQueue<DBOperation>();
		private Func<IDBX> getConnection;
		private ILogService log;

		public WakingDBTasks(Func<IDBX> getConnection, ILogService log)
		{
			this.getConnection = getConnection;
			this.log = log;
		}

		public void Enqueue(Action<IDBX> task)
		{
			queue.Enqueue(new DBOperation(task));
			Wake();
		}

		public void Wake()
		{
			lock (syncObj)
			{
				if (running) return;
				waitHandle.Reset();
				running = true;
			}
			Task.Factory.StartNew(() =>
			{
				runOperations();
				lock (syncObj)
				{
					running = false;
					waitHandle.Set();
				}
			});
		}

		private void runOperations()
		{
			DBOperation op;
			IDBX conn = getConnection();
			do
			{
				if (queue.TryDequeue(out op))
				{
					if (op.LastExecuteDate.AddSeconds(5) >= DateTime.Now)
						return;
					op.LastExecuteDate = DateTime.Now;
					try
					{
						op.Operation(conn);
						op.Completed = true;
					}
					catch (Exception ex)
					{
						op.RetryCount++;
						if (op.RetryCount >= 10)
							log.LogException(ex);
						else
							queue.Enqueue(op);
					}
				}
			} while (op != null);
		}

		public void WaitForCompletion()
		{
			waitHandle.WaitOne();
		}

		public bool WaitForCompletion(TimeSpan timeout)
		{
			return waitHandle.WaitOne(timeout);
		}
	}


	class DBOperation
	{
		public Action<IDBX> Operation;
		public bool Completed;
		public DateTime LastExecuteDate = DateTime.MinValue;
		public int RetryCount;

		public DBOperation(Action<IDBX> operation)
		{
			Operation = operation;
		}
	}
}
