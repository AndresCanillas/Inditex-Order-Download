using Services.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Service.Contracts.WF
{
	delegate void ItemQueueCallback(List<ItemData> item);

	class ReadyItemQueue : IDisposable
	{
		private TaskDataModel taskModel;
		private ILogService log;
		private ConcurrentDictionary<int, SubscriberInfo> subscribers;
		private CancellationTokenSource cts;


		public ReadyItemQueue(TaskDataModel taskModel, ILogService log)
		{
			this.taskModel = taskModel;
			this.log = log;
			subscribers = new ConcurrentDictionary<int, SubscriberInfo>();
			cts = new CancellationTokenSource();
		}


		public int WorkflowID { get; set; }


		public void Dispose()
		{
			cts.Dispose();
		}


		public void Start()
		{
			Task.Factory.StartNew(SearchReadyItems, cts.Token);
		}


		public void Stop()
		{
			cts.Cancel();
		}


		public void RegisterForTask(int taskid, ItemQueueCallback itemHandler)
		{
			subscribers.TryAdd(taskid, new SubscriberInfo(taskid, itemHandler));
		}


		private async Task SearchReadyItems()
		{
			Random rnd = new Random();
			while (!cts.IsCancellationRequested)
			{
				try
				{
					await InitializeSubscribersReadyItems();
					DispatchItems();
				}
				catch (Exception ex)
				{
					log.LogException(ex);
				}

				if (!cts.IsCancellationRequested)
					await Task.Delay(rnd.Next(8000, 10000), cts.Token);
			}
		}


		private async Task InitializeSubscribersReadyItems()
		{
			foreach (var subscriber in subscribers.Values)
				subscriber.ReadyItems.Clear();

			if (cts.IsCancellationRequested)
				return;

			var items = await taskModel.GetReadyItems(WorkflowID);

			if (cts.IsCancellationRequested)
				return;

			foreach (var item in items)
			{
				if (subscribers.TryGetValue(item.TaskID.Value, out var subscriber))
					subscriber.ReadyItems.Add(item);
			}
		}


		// IMPORTANT: The subscriber callback SHOULD NOT process the items directly, instead it is expected to grab the items and
		// register them somewhere, so they are processed later by the task runner (on a separate thread).
		//
		// Attempting to process the items directly in the callback will cause tasks to block each other, preventing them from 
		// processing items concurrently.
		//
		// Also notice we send a copy of the ReadyItems list to the callback, and not the ReadyItems list directly, this is
		// important to prevent sharing state (the list) between ReadyItemQueue and the target task whose callback we are
		// invoking.

		private void DispatchItems()
		{
			if (cts.IsCancellationRequested)
				return;

			foreach (var subscriber in subscribers.Values)
			{
				if (subscriber.ReadyItems.Count > 0)
					subscriber.Callback(new List<ItemData>(subscriber.ReadyItems));
			}
		}


		class SubscriberInfo
		{
			public int TaskID;
			public ItemQueueCallback Callback;
			public List<ItemData> ReadyItems;

			public SubscriberInfo(int taskid, ItemQueueCallback callback)
			{
				TaskID = taskid;
				Callback = callback;
				ReadyItems = new List<ItemData>();
			}
		}
	}


	public static class ConcurrentQueueExtensions
	{
		public static void Enqueue<T>(this ConcurrentQueue<T> queue, IEnumerable<T> elements)
		{
			if (queue == null)
				throw new ArgumentNullException(nameof(queue));
			if(elements == null)
				throw new ArgumentNullException(nameof(elements));

			foreach (var e in elements)
				queue.Enqueue(e);
		}

		public static List<T> DequeueAll<T>(this ConcurrentQueue<T> queue)
		{
			var result = new List<T>();
			while(queue.TryDequeue(out var e))
			{
				result.Add(e);
			}
			return result;
		}
	}
}
