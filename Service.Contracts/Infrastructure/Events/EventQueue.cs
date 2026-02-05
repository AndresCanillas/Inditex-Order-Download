using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using Services.Core;

namespace Service.Contracts
{
    public class EventQueue : IEventQueue
    {
        private volatile bool isRunning;
        private ConcurrentQueue<EQEventInfo> queue;
        private ConcurrentDictionary<string, ConcurrentDictionary<string, Action<EQEventInfo>>> subscribers;
        private ILogService log;
        private object syncObj = new object();
        private List<Action<EQEventInfo>> globalSubscribers = new List<Action<EQEventInfo>>();

        public event Action<EQEventInfo> OnEventRegistered
        {
            add
            {
                lock (syncObj)
                {
                    globalSubscribers.Add(value);
                }
            }
            remove
            {
                lock (syncObj)
                {
                    globalSubscribers.Remove(value);
                }
            }
        }

        public ConcurrentQueue<EQEventInfo> CurrentEvents => queue;

        public EventQueue(ILogService log)
		{
			this.log = log;
			queue = new ConcurrentQueue<EQEventInfo>();
			subscribers = new ConcurrentDictionary<string, ConcurrentDictionary<string, Action<EQEventInfo>>>();
		}

		public void Send<T>(T eventInfo) where T : EQEventInfo
		{
			eventInfo.EventName = typeof(T).Name;
			queue.Enqueue(eventInfo as EQEventInfo);
			WakeSendTask();
		}


		public void Send(object evt, EventSource source)
		{
			EQEventInfo eventInfo = (EQEventInfo)evt;
			eventInfo.Source = source;
			eventInfo.EventName = evt.GetType().Name;
			queue.Enqueue(eventInfo);
			WakeSendTask();
		}


		private void WakeSendTask()
		{
			if (!isRunning)
			{
				bool swStartTask = false;
				lock (queue)
				{
					if (!isRunning)
						swStartTask = true;
				}
				if (swStartTask)
				{
					ThreadPool.QueueUserWorkItem(sendNotifications);
				}
			}
		}


		public void SendWithDelay<T>(int delay, T eventInfo) where T : EQEventInfo
		{
			Task.Delay(delay).ContinueWith((s) => Send(eventInfo));
		}


		public void InvokeSubscribers<T>(T e) where T : EQEventInfo
		{
			if(String.IsNullOrWhiteSpace(e.EventName))
				e.EventName = typeof(T).Name;

			// Invoke typed subscribers
			if (subscribers.TryGetValue(e.EventName, out var actions))
			{
				foreach (var action in actions.Values)
				{
					try
					{
						action(e);
					}
					catch (Exception ex)
					{
						log.LogException($"EventName: {e.EventName}. Error while executing Event notification callback.", ex);
					}
				}
			}

			// Invoke global (non-typed) subscribers
			List<Action<EQEventInfo>> globalSnapshot;
			lock (syncObj)
			{
				globalSnapshot = new List<Action<EQEventInfo>>(globalSubscribers);
			}
			foreach (var action in globalSnapshot)
			{
				action(e);
			}
		}


		public string Subscribe<T>(Action<T> action) where T : EQEventInfo
		{
			ConcurrentDictionary<string, Action<EQEventInfo>> actions;
			string eventname = typeof(T).Name;
			if (!subscribers.TryGetValue(eventname, out actions))
			{
				actions = new ConcurrentDictionary<string, Action<EQEventInfo>>();
				if (!subscribers.TryAdd(eventname, actions))
					actions = subscribers[eventname];
			}
			string token = Guid.NewGuid().ToString();
			actions[token] = (e) => action(e as T);
			return token;
		}


		public void Unsubscribe<T>(string token) where T : EQEventInfo
		{
			ConcurrentDictionary<string, Action<EQEventInfo>> actions;
			string eventname = typeof(T).Name;
			if (subscribers.TryGetValue(eventname, out actions))
			{
				Action<EQEventInfo> action;
				actions.TryRemove(token, out action);
			}
		}


		private void sendNotifications(object state)
		{
			lock (syncObj)
			{
				if (isRunning)
					return;
				isRunning = true;
			}

			try
			{
				do
				{
					while (queue.Count > 0)
					{
						EQEventInfo e;
						if (queue.TryDequeue(out e))
						{
							InvokeSubscribers(e);
						}
					}
					lock (queue)
					{
						if (queue.Count == 0)
							break;
					}
				} while (true);

			}
			catch(Exception ex)
			{
				log.LogException(ex);
			}
			finally
			{
				lock (syncObj)
				{
					isRunning = false;
				}
			}
		}

        public void Dispose()
        {
            
        }
    }
}
