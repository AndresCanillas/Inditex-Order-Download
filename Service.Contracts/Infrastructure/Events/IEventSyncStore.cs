using Service.Contracts.Database;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Contracts
{
	/* ====================================================================================================
	 * EventSyncStore
	 * 
	 * Is a singleton service that must be instantiated and configured as soon as the application starts.
	 * Its main purpose is to ensure that events that are of interest to remote parties are not lost if
	 * said events are detected while the intended subscribers are offline.
	 * 
	 * EventSyncClient depends on this singleton in order to retrieve the events that have not been delivered
	 * to a remote subscriber and to acknowledge events whose delivery has been confirmed.
	 * ==================================================================================================== */
	public interface IEventSyncStore
	{
		/// <summary>
		/// Event trigered every time an event that has been retried too many times is raised.
		/// </summary>
		event EventHandler<EQNotification> OnDeadLetter;
		/// <summary>
		/// Initializes the provider and connection string required to persist data
		/// </summary>
		void Configure(string serviceName, string provider, string connStr);
		/// <summary>
		/// Updates (or inserts) a subscriber and the events said subscriber is interested in receiving. If the susbcriber is being updated, the system makes sure to review
		/// the list of subscriptions provided, and makes sure to remove any pending events that might be in the database that are no longer required by the remote system.
		/// </summary>
		void UpdateSubscriber(string subscriberid, List<EQSubscription> subscriptions);
		/// <summary>
		/// Removes a subscriber from the database and all its pending notifications.
		/// </summary>
		void RemoveSubscriber(string subscriberid);
		/// <summary>
		/// Registers a delegate that should be invoked in order to deliver pending notifications to a remote subscriber (this is usually invoked from EventSyncClient
		/// when a connection to the remote party is stablished)
		/// IMPORTANT: The delegate will be automatically removed if invoking it causes an exception.
		/// </summary>
		void RegisterHandler(string subscriberid, Action<EQNotification> action);
		/// <summary>
		/// Removes the delegate used to deliver notifications to a remote subscriber (this is usually invoked from EventSyncClient when it is disposed).
		/// </summary>
		void RemoveHandler(string subscriberid, Action<EQNotification> action);
		/// <summary>
		/// Removes all handler delegates used to deliver notifications to remote subscribers.
		/// </summary>
		void ClearHandlers();
		/// <summary>
		/// Acknowledges the delivery of a notification, said notification is removed from the database so it is never sent again.
		/// </summary>
		void AcknowledgeNotification(string subscriberid, int eventid);
		/// <summary>
		/// Delays a notification so it is delivered again at a later time
		/// </summary>
		void DelayNotification(string subscriberid, int eventid, TimeSpan delay);
		/// <summary>
		/// Allows to update the state of the event (used for instance if the event needs to be retried later) and any state change needs to be preserved
		/// </summary>
		/// <param name="eventid">ID of the event</param>
		/// <param name="eventState">The new state of the event</param>
		void UpdateEventState(int eventid, string eventState);
    }


	public class EQService : IEntity
	{
		[PK, Identity]
		public int ID { get; set; }
		public string Name{ get; set; }
	}


	public class EQSubscriber : IEntity
	{
		[PK, Identity]
		public int ID { get; set; }
		public string SubscriberID { get; set; }
		public string ActiveSubscriptions { get; set; }
		public DateTime LastSeenOnline { get; set; }
		public int? ServiceID { get; set; }
        public int? RetentionThreshold { get;set; }
    }

	public class EQEvent: IEntity
	{
		[PK, Identity]
		public int ID { get; set; }
		public string EventName { get; set; }
		public string EventData { get; set; }
		public DateTime Date { get; set; }
		public int? ServiceID { get; set; }
	}

	public class EQTracking
	{
		[PK]
		public int SubscriberID { get; set; }
		[PK]
		public int EventID { get; set; }
		public bool EventSent { get; set; }
		public int RetryCount { get; set; }
		public DateTime? DelayedUntil { get; set; }
	}

	public class EQEventProj : IEntity
	{
		[PK, Identity]
		public int ID { get; set; }
		public string EventName { get; set; }
		public string EventData { get; set; }
		public DateTime Date { get; set; }
		public int RetryCount { get; set; }
		public int SubscriberID { get; set; }
	}


	public class EQEventSubscriptorInfo
	{
		private object syncObj = new object();

		public EQEventSubscriptorInfo(int id, string subscriberid, List<EQSubscription> activeSubscriptions, DateTime lastSeenOnline)
		{
			ID = id;
			SubscriberID = subscriberid;
			ActiveSubscriptions = activeSubscriptions;
			LastSeenOnline = lastSeenOnline;
			NotificationHandlers = new ConcurrentList<Action<EQNotification>>();
		}

		public int ID;
		public string SubscriberID;
		public List<EQSubscription> ActiveSubscriptions;
		public DateTime LastSeenOnline;
		public ConcurrentList<Action<EQNotification>> NotificationHandlers;

		public void Update(List<EQSubscription> subscriptions, DateTime lastSeenOnline)
		{
			lock (syncObj)
			{
				ActiveSubscriptions = subscriptions;
				LastSeenOnline = lastSeenOnline;
			}
		}

		public bool IsSubscribed(object evt)
		{
			if (evt == null)
				throw new ArgumentNullException(nameof(evt));
			bool result = false;
			string evtName = evt.GetType().Name;
			lock (syncObj)
			{
				result = (ActiveSubscriptions.FindIndex(p => p.EventName == evtName) >= 0);
			}
			return result;
		}
	}
}
