using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Services.Core;

namespace Service.Contracts
{
	public class EventSyncClient: IEventSyncClient
	{
		private volatile bool connected;
		private volatile bool connecting;
		private volatile bool disposed;
		private object syncObj = new object();
		private string subscriberid;
		private List<EQSubscription> subscriptions;
		private Dictionary<Type, EventRegistrationInfo> eventRegistrations;
		private string remoteSubscriberID;
		private List<EQSubscription> remoteSubscriptions;
		private string ip;
		private string url;
		private string secret;
		//private int hash;
		private IFactory factory;
		private IEventSyncStore eventStore;
		private IEventQueue events;
		private ILogSection log;
		private WebSocket socket;
		private readonly ConcurrentDictionary<string, int> bannedips = new ConcurrentDictionary<string, int>();
		private readonly ConcurrentQueue<EQMessage> queue = new ConcurrentQueue<EQMessage>();
		private Timer reconnectTimer;
		private Timer heartbeatTimer;

		static EventSyncClient()
		{
			ServicePointManager.ServerCertificateValidationCallback = (a, b, c, d) => true;
		}

		/// <summary>
		/// Event fired when the client becomes connected. This event is fired when the initial handshake has completed successfully.
		/// </summary>
		public event EventHandler Connected;

		/// <summary>
		/// Event fired when the client becomes disconnected. This event is fired when the connection is closed or interrupted regardless of the reason.
		/// </summary>
		public event EventHandler Disconnected;


		public EventSyncClient(IFactory factory, IAppConfig config, IEventQueue events, IAppInfo appInfo, ILogService log)
		{
			this.factory = factory;
			this.events = events;
			this.log = log.GetSection("EventSynchronization");
			eventRegistrations = new Dictionary<Type, EventRegistrationInfo>();
			subscriberid = config.GetValue("EventSyncService.SubscriberID", $"{appInfo.NodeName}/{appInfo.AppName}");
			subscriptions = new List<EQSubscription>(20);
			log.LogMessage($"Local SubscriberID: {subscriberid}");
		}

		public string SubscriberID
		{
			get => subscriberid;
			set
			{
				subscriberid = value;
			}
		}

		public bool IsConnected
		{
			get => connected;
		}

		public void Configure(IEventSyncStore eventStore, string secret)
		{
			this.eventStore = eventStore;
			this.secret = secret;
		}

		public void Subscribe<T>() where T : EQEventInfo
		{
			Type eventType = typeof(T);
			if (!eventRegistrations.ContainsKey(eventType))
			{
				eventRegistrations[eventType] = new EventRegistrationInfo(eventType);
				subscriptions.Add(new EQSubscription(eventType.Name));
			}
		}

		public void Subscribe<EventType, HandlerType>()
			where EventType : EQEventInfo
			where HandlerType : EQEventHandler<EventType>
		{
			Type eventType = typeof(EventType);
			Type handlerType = typeof(HandlerType);
			if (!eventRegistrations.ContainsKey(eventType))
			{
				eventRegistrations[eventType] = new EventRegistrationInfo(eventType, handlerType);
				subscriptions.Add(new EQSubscription(eventType.Name));
			}
			else
			{
				eventRegistrations[eventType].Handlers.Add(handlerType);
			}
		}

		public void Dispose()
		{
			Disconnect().Wait();
			disposed = true;
			if (reconnectTimer != null)
				reconnectTimer.Dispose();
			if (heartbeatTimer != null)
				heartbeatTimer.Dispose();
		}



		// ==========================================================================
		// Client-side exclusive code
		// ==========================================================================
		#region Client-side exclusive code

		public void Connect(string url)
		{
			if (String.IsNullOrWhiteSpace(url))
				throw new InvalidOperationException("url cannot be null or empty");

			this.url = url;
			lock (syncObj)
			{
				if (reconnectTimer == null)
					reconnectTimer = new Timer(checkConnection, null, 500, Timeout.Infinite);
				if (heartbeatTimer == null)
					heartbeatTimer = new Timer(sendHeartbeat, null, 30000, Timeout.Infinite);
			}
		}


		private void checkConnection(object state)
		{
			try
			{
				InternalConnect().Wait();
			}
			catch (Exception ex)
			{
				log.LogException(ex);
			}
			finally
			{
				reconnectTimer.Change(20000, Timeout.Infinite);
			}
		}


		private void sendHeartbeat(object state)
		{
			try
			{
				lock (syncObj)
				{
					if (!connected || connecting || disposed)
						return;
				}
				Send("", EQMessageType.Heartbeat);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
			}
			finally
			{
				heartbeatTimer.Change(30000, Timeout.Infinite);
			}
		}


		public async Task InternalConnect()
		{
			lock (syncObj)
			{
				if (connected || connecting || disposed)
					return;
				else
					connecting = true;
			}
			await HandshakeAsClient();
		}


		private async Task HandshakeAsClient()
		{
			try
			{
				bool alreadyConnected = false;
				lock (syncObj) if (connected) alreadyConnected = true;
				if (alreadyConnected) return;
				log.LogMessage("Connecting to Event Synchronization service...");
				socket = new ClientWebSocket();
				await (socket as ClientWebSocket).ConnectAsync(new Uri(url), CancellationToken.None);
				log.LogMessage("Connection successfull, starting send/receive loops...");
				_ = StartSendLoop();
				_ = StartReceiveLoop();
				SendHandshake();
			}
			catch (Exception ex)
			{
				log.LogException(ex);
			}
			finally
			{
				lock (syncObj)
				{
					connecting = false;
					connected = (socket != null && socket.State == WebSocketState.Open);
				}
			}
		}


		private void SendHandshake()
		{
			Random rnd = new Random();
			var tokenBytes = new byte[rnd.Next(32, 65)];
			rnd.NextBytes(tokenBytes);
			var token = Convert.ToBase64String(tokenBytes);
			var handshake = new EQHandshake()
			{
				Token = token,
				Secret = secret,
				SubscriberID = subscriberid,
				ActiveSubscriptions = subscriptions
			};
			log.LogMessage("Sending handshake...");
			Send(handshake, EQMessageType.Handshake);
		}


		public async Task Disconnect()
		{
			var disconnect = false;
			lock (syncObj)
			{
				if (connected)
					disconnect = true;
			}
			if (disconnect)
			{
				await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "200", CancellationToken.None);
				eventStore.RemoveHandler(subscriberid, NotificationHandler);
				HandleDisconnection();
			}
		}

		#endregion



		// ==========================================================================
		// Server-side exclusive code
		// ==========================================================================
		#region Server-side exclusive code

		public async Task<int> Accept(WebSocket socket, string ip)
		{
			this.socket = socket;
			this.ip = ip;
			connected = true;
			bannedips.TryGetValue(ip, out var retryCount);
			if (retryCount < 5)
			{
				try
				{
					var handshake = await WaitForHandshake();
					if (handshake.Secret == secret)
					{
						bannedips.TryRemove(ip, out _);
						remoteSubscriberID = handshake.SubscriberID;
						remoteSubscriptions = handshake.ActiveSubscriptions;
						log.LogMessage($"Accepted Event Synchronization connection from {ip}/{remoteSubscriberID}/{remoteSubscriptions}. Starting send/receive loops");
						eventStore.UpdateSubscriber(remoteSubscriberID, remoteSubscriptions);
						eventStore.RegisterHandler(remoteSubscriberID, NotificationHandler);
						_ = StartSendLoop();
						Send(new EQHandshakeAck() { SubscriberID = subscriberid, ActiveSubscriptions = subscriptions }, EQMessageType.HandshakeAck);
						if (heartbeatTimer == null)
							heartbeatTimer = new Timer(sendHeartbeat, null, 30000, Timeout.Infinite);
						await StartReceiveLoop();
						return 200;
					}
					else
					{
						log.LogWarning($"Rejected Event Synchronization connection. Remote end point did not supply valid secret: [{handshake.Secret}].");
					}
				}
				catch (OperationCanceledException)
				{
					log.LogWarning("Rejected Event Synchronization connection. Remote end point took too long to send the initial handshake.");
				}
				catch (Exception ex)
				{
					log.LogException("Rejected Event Synchronization connection due to an unexpected error.", ex);
				}
				bannedips[ip] = retryCount + 1;
			}
			else
			{
				log.LogWarning($"Rejected Event Synchronization connection from {ip}. It is listed as banned and will remain banned until service is restarted.");
			}
			await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "403", CancellationToken.None);
			return 403;
		}


		private async Task<EQHandshake> WaitForHandshake()
		{
			int retryCount = 1;
			var buffer = new byte[1024 * 10];
			do
			{
				var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), new CancellationTokenSource(180000).Token);
				if (result.MessageType == WebSocketMessageType.Text)
				{
					var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
					var message = JsonConvert.DeserializeObject<EQMessage>(text);
					if (!String.IsNullOrWhiteSpace(message.Payload))
					{
						var handshake = JsonConvert.DeserializeObject<EQHandshake>(message.Payload);
						if (handshake != null)
							return handshake;
					}
				}
			} while (retryCount++ < 5);
			throw new OperationCanceledException("Failed to receive a valid handshake after several attempts, connection will be cancelled.");
		}

		#endregion



		private void NotificationHandler(EQNotification e)
		{
			Send(e, EQMessageType.Notification);
		}


		private void Send(object evt, EQMessageType type)
		{
			LogMessage($"Sending Message", type);
			var json = JsonConvert.SerializeObject(evt);
			var msg = new EQMessage()
			{
				Type = type,
				Payload = json
			};
			queue.Enqueue(msg);
		}


		private async Task StartSendLoop()
		{
			try
			{
				do
				{
					var msg = await WaitForMessage();
					if (msg != null && socket.State == WebSocketState.Open && !disposed)
					{
						var json = JsonConvert.SerializeObject(msg);
						if (json.Length > 1000000)
							throw new InvalidOperationException("Message is too large, consider changing the definition of the event such that only a few IDs and a minimum amount of data is transmitted.");
						await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(json)), WebSocketMessageType.Text, true, CancellationToken.None);
					}
				} while (socket.State == WebSocketState.Open && !disposed);
			}
			catch (Exception)
			{
				try
				{
					await socket.CloseAsync(WebSocketCloseStatus.InternalServerError, "500", CancellationToken.None);
				}
				catch { }
			}
			finally
			{
				HandleDisconnection();
			}
		}


		private async Task<EQMessage> WaitForMessage()
		{
			while (socket.State == WebSocketState.Open && !disposed)
			{
				if (queue.Count == 0)
					await Task.Delay(200).ConfigureAwait(false);
				if (queue.TryDequeue(out var element))
					return element;
			}
			return null;
		}


		private async Task StartReceiveLoop()
		{
			var pos = 0;
			var buffer = new byte[1048576];
			try
			{
				while (socket.State == WebSocketState.Open && !disposed)
				{
					var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer, pos, buffer.Length - pos), CancellationToken.None);
					if (socket.State == WebSocketState.Open && result.MessageType != WebSocketMessageType.Close)
					{
						pos += result.Count;
						if (pos <= buffer.Length)
						{
							if (result.MessageType == WebSocketMessageType.Text)
							{
								var message = Encoding.UTF8.GetString(buffer, 0, pos);
								ProcessTextFrame(message);
								pos = 0;
							}
							else if (result.MessageType == WebSocketMessageType.Binary)
							{
								await socket.CloseAsync(WebSocketCloseStatus.ProtocolError, "Unexpected Message Type", CancellationToken.None);
								log.LogWarning("Clossing Event Synchronization connection because we received a binary frame when not expected.");
								break;
							}
						}
						else
						{
							await socket.CloseAsync(WebSocketCloseStatus.ProtocolError, "501", CancellationToken.None);
							log.LogWarning("Clossing Event Synchronization connection because we received a message that cannot be fit in the receive buffer.");
							break;
						}
					}
				}
			}
			catch (Exception ex)
			{
				if (socket.State == WebSocketState.Open)
				{
					try
					{
						await socket.CloseAsync(WebSocketCloseStatus.InternalServerError, "500", CancellationToken.None);
					}
					catch { }
				}
				log.LogWarning("Event Synchronization connection closed due to an error.");
				log.LogException(ex);
			}
			finally
			{
				HandleDisconnection();
			}
		}


		private void ProcessTextFrame(string message)
		{
			var syncMsg = JsonConvert.DeserializeObject<EQMessage>(message);
			switch (syncMsg.Type)
			{
				case EQMessageType.HandshakeAck:
					HandleHandshakeAck(syncMsg);
					break;
				case EQMessageType.Notification:
					HandleNotification(syncMsg);
					break;
				case EQMessageType.NotificationAck:
					HandleNotificationAck(syncMsg);
					break;
				case EQMessageType.Heartbeat:
					Send("", EQMessageType.HeartbeatAck);
					break; // Acknowledge the heartbeat
				case EQMessageType.HeartbeatAck:
					break; // Do nothing
			}
		}


		private void HandleHandshakeAck(EQMessage syncMsg)
		{
			try
			{
				LogMessage("Received Message", syncMsg.Type);
				var ack = JsonConvert.DeserializeObject<EQHandshakeAck>(syncMsg.Payload);
				remoteSubscriberID = ack.SubscriberID;
				remoteSubscriptions = ack.ActiveSubscriptions;
				eventStore.UpdateSubscriber(ack.SubscriberID, ack.ActiveSubscriptions);
				eventStore.RegisterHandler(ack.SubscriberID, NotificationHandler);
				Connected?.Invoke(this, EventArgs.Empty);
			}
			catch(Exception ex)
			{
				log.LogException(ex);
			}
		}



		private void HandleNotification(EQMessage syncMsg)
		{
			var notification = JsonConvert.DeserializeObject<EQNotification>(syncMsg.Payload);
			KeyValuePair<Type, EventRegistrationInfo> kvp = eventRegistrations.FirstOrDefault(p => p.Value.EventName == notification.EventName);
			if (kvp.Key == null || kvp.Value == null)
			{
				LogMessage($"Received EventID: {notification.ID} / {notification.EventName}. Event will be ignored because it has no subscribers.", EQMessageType.Notification);
			}
			else
			{
				var evt = JsonConvert.DeserializeObject(notification.EventData, kvp.Key);
				events.Send(evt, EventSource.Remote);
				LogMessage($"Received EventID: {notification.ID} / {notification.EventName}. Event was delivered using EventQueue.", EQMessageType.Notification);

				if (kvp.Value.Handlers.Count == 0)
				{
					log.LogMessage($"EventID: {notification.ID} / {notification.EventName}. Event does not have a registered subscriber.");
				}
				else
				{
					foreach (var handlerType in kvp.Value.Handlers)
					{
						try
						{
							var instance = factory.GetInstance(handlerType);
							try
							{
								Reflex.Invoke(instance, "HandleEvent", evt);
								log.LogMessage($"EventID: {notification.ID} / {notification.EventName}. Event was delivered to subscriber {handlerType.Name}.");
							}
							finally
							{
								if (instance is IDisposable)
									(instance as IDisposable).Dispose();
							}
						}
						catch (Exception ex)
						{
							log.LogException($"Exception while processing event. Will attempt to send APMErrorNotification event.", ex);

							Exception actualException = ex as Exception;
							while (actualException.InnerException != null)
								actualException = actualException.InnerException;

							var e = new APMErrorNotification() { Message = actualException.Message, StackTrace = actualException.StackTrace };

							var handlerName = actualException.GetType().ToString() + "-" + handlerType.Name;
							e.NotificationKey = handlerName.Length >= 119 ?
								handlerName.Substring(0, 119) : handlerName;
							e.HandlerType = handlerType.FullName;

							events.Send(e);

							log.LogMessage("APMErrorNotification sent");
						}
					}
				}
			}
			Send(new EQNotificationAck() { ID = notification.ID }, EQMessageType.NotificationAck);
		}


		private void HandleNotificationAck(EQMessage syncMsg)
		{
			var ack = JsonConvert.DeserializeObject<EQNotificationAck>(syncMsg.Payload);
			LogMessage($"EventID: {ack.ID}", EQMessageType.NotificationAck);
			eventStore.AcknowledgeNotification(remoteSubscriberID, ack.ID);
		}


		private void HandleDisconnection()
		{
			lock (syncObj)
			{
				if (connected)
				{
					connected = false;
					try { socket.Dispose(); } catch { }
					LogMessage("Disconnected");
					Disconnected?.Invoke(this, EventArgs.Empty);
				}
			}
			while (queue.TryDequeue(out _)) ;
		}

		private void LogMessage(string message)
		{
			if (remoteSubscriberID != null)
				log.LogMessage($"{message}. RemoteSubscriber: {remoteSubscriberID}.");
			else
				log.LogMessage($"{message}.");
		}

		private void LogMessage(string message, EQMessageType type)
		{
			if (remoteSubscriberID != null)
				log.LogMessage($"{message}. RemoteSubscriber: {remoteSubscriberID}. MessageType: {type}");
			else
				log.LogMessage($"{message}. MessageType: {type}");
		}


		class EventRegistrationInfo
		{
			public Type EventType;
			public string EventName;
			public List<Type> Handlers = new List<Type>();

			public EventRegistrationInfo(Type eventType)
			{
				EventType = eventType;
				EventName = eventType.Name;
			}

			public EventRegistrationInfo(Type eventType, Type handlerType)
			{
				EventType = eventType;
				EventName = eventType.Name;
				Handlers.Add(handlerType);
			}
		}
	}
}
