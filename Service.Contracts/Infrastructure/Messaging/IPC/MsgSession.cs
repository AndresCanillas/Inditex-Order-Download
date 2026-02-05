using Service.Contracts.IPC;
using Services.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;


namespace Service.Contracts
{
	public class MsgSession : IMsgSession, IDisposable
	{
		// =============================== Static Members ===============================

		private static int currentID = 0;

		public static int GetNextID()
		{
			return Interlocked.Increment(ref currentID);
		}

		// =============================== Instance Members ===============================
		private object syncObj = new object();
		private int id;
		private bool connected;
		private bool disconnecting;
		private bool disposed;
		private bool sending;
		private bool isServerSide;
		private DateTime lastHB;
		private bool sessionEndRaised;

		private IScope scope;
		private ILogService log;
		private MsgPeer peer;
		private Socket s;
		private ProtocolBuffer recvBuffer;					// Buffer used to receive messages
		private Queue<RequestInfo> queue;                   // Queues outgoing requests until they are sent to the other peer
		private Dictionary<int, RequestInfo> requests;      // A collection of all sent requests that are waiting for a response
		private Dictionary<int, ClientProxy> proxies;		// Dictionary used to store proxies used to make calls on a service exposed by the end point at the other side of the connection
		private Dictionary<Tuple<int, int>, int> evSubs;	// Dictionary used to store event subscriptions
		private Timer hbTimer;
		private IPEndPoint endPoint;
        private volatile bool simulateClientReceiveInvalidMessage;

        public event EventHandler OnEnd = delegate { };

		public IScope Scope { get => scope; }

		internal MsgSession(IScope scope, MsgPeer peer, Socket s, EventHandler sessionEndCallback)
			: this(scope)
		{
			this.peer = peer;
			this.s = s;
			endPoint = s.RemoteEndPoint as IPEndPoint;
			isServerSide = true;
			connected = true;
			lastHB = DateTime.Now;
			hbTimer.Change(15000, Timeout.Infinite);
			OnEnd += sessionEndCallback;
			s.BeginReceive(recvBuffer.buffer, 0, 8000, SocketFlags.None, receiveLoop, null);
		}


		internal MsgSession(IScope scope, MsgPeer peer)
			: this(scope)
		{
			this.peer = peer;
			endPoint = new IPEndPoint(IPAddress.Loopback, 5500);
			isServerSide = false;
			connected = false;
		}


		private MsgSession(IScope scope)
		{
			this.scope = scope;
			(scope as IFactory).RegisterScoped<IMsgStreamService, MsgStreamService>();
			(scope as IFactory).RegisterScoped<IMsgSession>(this);

			log = scope.GetInstance<ILogService>();

			id = GetNextID();
			recvBuffer = new ProtocolBuffer(scope);
			queue = new Queue<RequestInfo>();
			proxies = new Dictionary<int, ClientProxy>();
			requests = new Dictionary<int, RequestInfo>();
			evSubs = new Dictionary<Tuple<int, int>, int>();
			hbTimer = new Timer(SendHB, null, Timeout.Infinite, Timeout.Infinite);
			lastHB = DateTime.Now;
		}


		public void Dispose()
		{
			List<RequestInfo> requestsToAbort = null;

			lock (syncObj)
			{
				if (disposed)
					return;

				disposed = true;

				if (connected)
					Disconnect();

				recvBuffer.Release();
				recvBuffer = null;

				if (queue != null)
				{
					while (queue.Count > 0)
					{
						var rq = queue.Dequeue();
						rq.Dispose();
					}
					queue = null;
				}

				if (requests != null)
				{
					requestsToAbort = new List<RequestInfo>(requests.Values);
					requests.Clear();
					requests = null;
				}

				if (proxies != null)
				{
					proxies.Clear();
					proxies = null;
				}

				if (hbTimer != null)
				{
					hbTimer.Dispose();
					hbTimer = null;
				}

				if (evSubs != null)
				{
					evSubs.Clear();
					evSubs = null;
				}

				if (s != null)
				{
					s.Dispose();
				}

				OnEnd = null;
				scope.Dispose();
			}

			if (requestsToAbort != null && requestsToAbort.Count > 0)
			{
				var ex = new Exception($"Disconnected from the remote end point ({endPoint.ToString()}) while waiting for the response.");
				foreach (RequestInfo rq in requestsToAbort)
				{
					rq.SetError(ex);
				}
			}
		}


		public void Connect(IPEndPoint ep)
		{
			if (ep == null)
				throw new ArgumentNullException(nameof(ep));
			if (isServerSide)
				throw new InvalidOperationException("Cannot call Connect for server side sessions.");

			lock (syncObj)
			{
				if (disposed)
					throw new ObjectDisposedException($"Cannot call {nameof(MsgSession)}.{nameof(Connect)}, object is in the disposed state.");
				if (connected)
					throw new InvalidOperationException("Already connected to an end point.");
				if (disconnecting)
					throw new InvalidOperationException($"Cannot call {nameof(MsgSession)}.{nameof(Connect)}, object is in the disconnecting state.");

				sessionEndRaised = false;
				try
				{
                    recvBuffer.Reset();
					endPoint = ep;
					s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
					s.NoDelay = true;
					s.Connect(endPoint);
					connected = true;
					lastHB = DateTime.Now;
					hbTimer.Change(15000, Timeout.Infinite);
					s.BeginReceive(recvBuffer.buffer, 0, 8000, SocketFlags.None, receiveLoop, null);
					ThreadPool.QueueUserWorkItem(ResubscribeToEvents, null);
				}
				catch(Exception ex)
				{
					if (s != null)
					{
						connected = false;
						s.Dispose();
					}
					hbTimer.Change(Timeout.Infinite, Timeout.Infinite);
					throw ex;
				}
			}
		}


		public void Disconnect()
		{
			lock (syncObj)
			{
				if (!connected || disconnecting || disposed)
					return;

				disconnecting = true;
				s.Shutdown(SocketShutdown.Send);
				hbTimer.Change(Timeout.Infinite, Timeout.Infinite);
			}
		}


		private void SendHB(object state)
		{
			lock (syncObj)
			{
				if (disposed || disconnecting || !connected)
					return;

				try
				{
					if (lastHB.AddMinutes(1) < DateTime.Now)
					{
						RequestInfo hb = new RequestInfo(this);
						hb.output.StartMessage(MsgOpcode.HeartBeat, 0);
						hb.output.EndMessage();
						SendMessage(hb);
					}
					hbTimer.Change(15000, Timeout.Infinite);
				}
				catch (Exception) { }  // Ignore exceptions, simply stop sending Heart Beats
			}
		}


		public bool IsConnected
		{
			get
			{
				lock (syncObj)
					return connected;
			}
		}


		/// <summary>
		/// Gets or sets the end point to which we are connected to
		/// </summary>
		public IPEndPoint EndPoint
		{
			get { return endPoint; }
		}


		// =========================================================================
		// Service publishing
		// =========================================================================

		public T GetService<T>() where T : class
		{
			return peer.GetService<T>();
		}


		public void RegisterService<T>(T serviceInstance) where T : class
		{
			peer.RegisterService<T>(serviceInstance);
		}


		public T GetServiceProxy<T>() where T : class
		{
			return GetServiceProxy(typeof(T)) as T;
		}


		internal object GetServiceProxy(Type serviceContract)
		{
			ClientProxy proxy;
			int contractid = JenkinsHash.Compute(serviceContract.Name);
			if (!proxies.TryGetValue(contractid, out proxy))
			{
				proxy = DynamicCodeGen.CreateClientProxy(scope, serviceContract, contractid);
				proxies[contractid] = proxy;
			}
			return proxy;
		}


		// =========================================================================
		// Receive loop
		// =========================================================================

		private void receiveLoop(IAsyncResult ar)
		{
			ProtocolBuffer msg = null;
			lock (syncObj)
			{
				try
				{
					if (!connected || disposed)
						return;

					int rb;
					rb = s.EndReceive(ar);
					if (rb == 0)
					{
						UnsyncronizedGracefulConnectionShutdown();
					}
					else
					{
						recvBuffer.availableData += rb;

                        //if(simulateClientReceiveInvalidMessage && recvBuffer.availableData >= 16)
                        //{
                        //    var rnd = new Random();
                        //    var rndBytes = new byte[16];
                        //    Buffer.BlockCopy(rndBytes, 0, recvBuffer.buffer, 0, rndBytes.Length);
                        //    simulateClientReceiveInvalidMessage = false;
                        //}

                        while (recvBuffer.TryExtractMessage(out msg))
						{
							lastHB = DateTime.Now;
							ProcessMessage(msg);
						}

						if (recvBuffer.availableData + 8000 > recvBuffer.buffer.Length)
							recvBuffer.EnsureCapacity(recvBuffer.availableData + 8000);

						s.BeginReceive(recvBuffer.buffer, recvBuffer.availableData, 8000, SocketFlags.None, receiveLoop, null);
					}
				}
				catch (Exception ex)
				{
					if (msg != null)
						msg.Release();

					log.LogException($"Connection to {endPoint.ToString()} will be closed due to the following error...", ex);
					UnsynchronizedAbortiveConnectionShutdown();
				}
			}
		}


		private void UnsyncronizedGracefulConnectionShutdown()
		{
			connected = false;
			try
			{
				if (disconnecting)
				{
					s.Dispose();
				}
				else
				{
					disconnecting = true;
					s.Shutdown(SocketShutdown.Send);
					s.Close(100);
					s.Dispose();
				}
			}
			catch { }
			finally
			{
                recvBuffer.Reset();
                AbortAllRequests();
                ThreadPool.QueueUserWorkItem(RaiseOnEnd);
			}
		}


		private void UnsynchronizedAbortiveConnectionShutdown()
		{
			connected = false;
			try  // using try/catch here because there might be a problem with the socket and it might be in an invalid state.
			{
				s.Shutdown(SocketShutdown.Both);
				s.Close();
			}
			catch { }
			finally
			{
                recvBuffer.Reset();
                s.Dispose();
                AbortAllRequests();
				ThreadPool.QueueUserWorkItem(RaiseOnEnd);
			}
		}


		private void AbortAllRequests()
		{
			List<RequestInfo> pendingRequests;

			if (disposed || sessionEndRaised)
				return;

			sessionEndRaised = true;
			hbTimer.Change(Timeout.Infinite, Timeout.Infinite);
			pendingRequests = new List<RequestInfo>(requests.Values);
			requests.Clear();
			queue.Clear();

			ThreadPool.QueueUserWorkItem(FinishAbortAllRequests, pendingRequests);
		}


		private void FinishAbortAllRequests(object state)
		{
			var pendingRequests = state as List<RequestInfo>;
			Exception ex = new Exception($"Disconnected from the remote end point ({endPoint}) while waiting for the response.");
			foreach (RequestInfo rq in pendingRequests)
			{
				rq.SetError(ex);
			}
		}

		private void RaiseOnEnd(object state)
		{
			log.LogMessage($"Connection to {endPoint.ToString()} closed.");
			lock (syncObj)
				disconnecting = false;
			OnEnd?.Invoke(this, EventArgs.Empty);
		}


		private void ProcessMessage(ProtocolBuffer msg)
		{
			try
			{
				switch (msg.opcode)
				{
					case MsgOpcode.EventSubscription:
						HandleEventSubscriptionMessage(msg);
						msg.Release();
						break;
					case MsgOpcode.HeartBeat:
						msg.Release();
						break;
					case MsgOpcode.Event:
						ThreadPool.QueueUserWorkItem(HandleEventMessage, msg);
						break;
					case MsgOpcode.Invoke:
						ThreadPool.QueueUserWorkItem(HandleInvokeMessage, msg);
						break;
					case MsgOpcode.InvokeAsync:
						HandleInvokeAsyncMessage(msg);
						break;
					case MsgOpcode.Response:
						HandleResponseMessage(msg);
						break;
					case MsgOpcode.Exception:
						HandleExceptionMessage(msg);
						break;
					case MsgOpcode.StreamRequest:
						HandleStreamRequestMessage(msg);
						break;
					case MsgOpcode.StreamBlock:
						HandleStreamBlockMessage(msg);
						break;
				}
			}
			catch (Exception ex)
			{
				lock (syncObj)
				{
					if (!disposed)
					{
						log.LogException("Internal Error while processing message, connection will be closed.", ex);
						Disconnect();
					}
				}
			}
		}


		private void HandleEventSubscriptionMessage(ProtocolBuffer msg)
		{
			bool subscribe = msg.GetBoolean();
			int contractid = msg.GetInt32();
			int eventid = msg.GetInt32();
			if (subscribe)
				RegisterEvent(contractid, eventid);
			else
				UnregisterEvent(contractid, eventid);
		}


		internal void RegisterEvent(int contractid, int eventid)
		{
			Tuple<int, int> key = new Tuple<int, int>(contractid, eventid);
			lock (syncObj)
			{
				evSubs[key] = 0;
			}
		}


		internal void UnregisterEvent(int contractid, int eventid)
		{
			Tuple<int, int> key = new Tuple<int, int>(contractid, eventid);
			lock (syncObj)
			{
				evSubs.Remove(key);
			}
		}


		internal bool IsSubscriber(int contractid, int eventid)
		{
			Tuple<int, int> key = new Tuple<int, int>(contractid, eventid);
			lock (syncObj)
			{
				if (evSubs != null)
					return evSubs.ContainsKey(key);
				else
					return false;
			}
		}


		private void HandleEventMessage(object state)
		{
			ProtocolBuffer msg = state as ProtocolBuffer;
			ClientProxy proxy = null;
			int contractid;
			int eventid;
			try
			{
				contractid = msg.GetInt32();
				eventid = msg.GetInt32();

				lock (syncObj)
				{
					if (disposed)
						return;

					proxies.TryGetValue(contractid, out proxy);
				}

				if (proxy != null)
					proxy.OnReceiveEvent(eventid, msg);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
			}
			finally
			{
				msg.Release();
			}
		}


		private void HandleInvokeMessage(object state)
		{
			ServerProxy service;
			ProtocolBuffer message = state as ProtocolBuffer;
			RequestInfo rq = new RequestInfo(this, message);
			try
			{
				int contractid = rq.input.GetInt32();
				int methodid = rq.input.GetInt32();
				service = peer.GetService(contractid);
				service.InvokeMethod(methodid, rq);
				rq.output.EndMessage();
				SendMessage(rq);
			}
			catch (Exception ex)
			{
				try
				{
					rq.output.SetError(ex);
					SendMessage(rq);
				}
				catch
				{
					log.LogException(ex);
					rq.Dispose();
				}
			}
		}


		private void HandleInvokeAsyncMessage(ProtocolBuffer message)
		{
			ServerProxy service;
			RequestInfo rq = new RequestInfo(this, message);
			int contractid = rq.input.GetInt32();
			int methodid = rq.input.GetInt32();
			service = peer.GetService(contractid);
			Task.Factory.StartNew(async () =>
			{
				try
				{
					await service.InvokeMethodAsync(methodid, rq);
				}
				catch(Exception ex)
				{
					HandleAsyncCallException(ex, rq);
				}
			});
		}


		private void HandleAsyncCallException(Exception ex, RequestInfo rq)
		{
			try
			{
				if (ex is AggregateException)
				{
					var aex = ex as AggregateException;
					if (aex.InnerExceptions.Count > 0)
						rq.output.SetError(aex.InnerExceptions[0]);
					else if (aex.InnerException != null)
						rq.output.SetError(aex.InnerException);
					else
						rq.output.SetError(aex);
				}
				else
				{
					rq.output.SetError(ex);
				}
				SendMessage(rq);
			}
			catch (Exception sendex)
			{
				log.LogException(sendex);
				rq.Dispose();
			}
		}


		private void HandleResponseMessage(ProtocolBuffer msg)
		{
			RequestInfo rq;
			try
			{
				lock (syncObj)
				{
					if (requests.TryGetValue(msg.msgid, out rq))
						requests.Remove(msg.msgid);
				}

				if (rq != null)
					rq.SetResponse(msg);
                else
					msg.Release();
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				msg.Release();
			}
		}


		private void HandleExceptionMessage(ProtocolBuffer msg)
		{
			RequestInfo rq;
			try
			{
				lock (syncObj)
				{
					if (requests.TryGetValue(msg.msgid, out rq))
						requests.Remove(msg.msgid);
				}

				if (rq != null)
				{
					string type = msg.GetString();
					string message = msg.GetString();
					string stacktrace = msg.GetString();
					MsgException ex = new MsgException("A request resulted in an error being returned from the server.", type, message, stacktrace);
					rq.SetError(ex);
				}
			}
			catch (Exception ex)
			{
				log.LogException(ex);
			}
			finally
			{
				msg.Release();   // in this case msg is always released, as it is not being assigned to the rq object like in Response message.
			}
		}


		private void HandleStreamRequestMessage(ProtocolBuffer msg)
		{
			try
			{
				var guidBytes = msg.GetArray<byte>();
				var guid = new Guid(guidBytes);
				var streamService = scope.GetInstance<IMsgStreamService>();
				var stream = streamService.GetStream(guid);
				if (stream == null)
					Disconnect();  // Cannot keep connection open as the requested stream is invalid
				else
					Task.Factory.StartNew(async () => await SendStream(guidBytes, stream), TaskCreationOptions.LongRunning);
			}
			finally
			{
				msg.Release();
			}
		}


		private async Task SendStream(byte[] guid, Stream stream)
		{
			try
			{
				bool wait;
				int blockCount = 0;
				int readBytes;
				do
				{
					RequestInfo rq = new RequestInfo(this);

					rq.output.StartMessage(MsgOpcode.StreamBlock, 0);
					rq.output.AddInt32(0);                      // Adds dummy block size
					rq.output.AddArray<byte>(guid);

					readBytes = await stream.ReadAsync(rq.output.buffer, rq.output.position, SerializationBuffer.STREAM_BLOCK_SIZE);

					rq.output.availableData += readBytes;
					rq.output.position += readBytes;
					rq.output.SetInt32(17, readBytes);  // Update block size to the correct value
					rq.output.EndMessage();
					SendMessage(rq);

					blockCount++;
					if (blockCount % 20 == 0)
					{
						do
						{
							lock (syncObj)
							{
								if (disposed || disconnecting || !connected)
									return;
								wait = (queue.Count > 20);
							}

							if (wait)
								await Task.Delay(14);  // smallest delay will be rougly 15 ms anyway. TODO: Determine if 15ms is enough to transfer 20 blocks
						} while (wait);
					}

				} while (readBytes > 0);
			}
			catch (Exception ex)
			{
				log.LogException("Stream transfer was cancelled due to an error.", ex);
			}
			finally
			{
				var streamService = scope.GetInstance<IMsgStreamService>();
				streamService.UnregisterStream(new Guid(guid));
			}
		}


		private void HandleStreamBlockMessage(ProtocolBuffer msg)
		{
			var blockSize = msg.GetInt32();
			var guidBytes = msg.GetArray<byte>();
			var guid = new Guid(guidBytes);
			var streamService = scope.GetInstance<IMsgStreamService>();
			var stream = streamService.GetStream(guid);
			if (stream == null)
			{
				Disconnect();
			}
			else
			{
				msg.availableData = msg.position + blockSize;
				(stream as RemoteStream).HandleStreamBlock(msg);
			}
		}



		// =========================================================================
		// Send loop
		// =========================================================================

		private void sendLoop(IAsyncResult ar)
		{
			var rq = ar.AsyncState as RequestInfo;
			lock (syncObj)
			{
				try
				{
					if (!connected || disconnecting || disposed)
					{
						rq.Unlock();
						rq.Dispose();
						sending = false;
						return;
					}

					int sb = s.EndSend(ar);

					rq.output.position += sb;
					if (rq.output.position < rq.output.availableData)
					{
						s.BeginSend(
							rq.output.buffer,
							rq.output.position,
							rq.output.availableData - rq.output.position,
							SocketFlags.None,
							sendLoop, rq);
					}
					else
					{
						rq.Unlock();
						if (rq.lifetime == RQLifetime.Oneway)
							rq.Dispose();

						sending = queue.Count > 0;
						if (sending)
						{
							rq = queue.Dequeue();
							s.BeginSend(
								rq.output.buffer,
								0,
								rq.output.availableData,
								SocketFlags.None,
								sendLoop, rq);
						}
					}
				}
				catch (Exception ex)
				{
					rq.Unlock();
					if (rq.lifetime == RQLifetime.Oneway)
						rq.Dispose();

					sending = false;
					log.LogException($"Connection to {endPoint.ToString()} will be closed due to the following error...", ex);
					UnsynchronizedAbortiveConnectionShutdown();
				}
			}
		}


		public void SendMessage(RequestInfo rq)
		{
			if (!rq.output.msgCompleted)
				throw new InvalidOperationException("Message being sent is not complete");

			try
			{
				lock (syncObj)
				{
					if (!connected || disconnecting || disposed)
						throw new InvalidOperationException("Unable to send message because the connection is closed or being shutdown.");

					if (queue.Count > 1000)
						throw new Exception("Unable to send message becuase too many messages are queued, the remote endpoint is not responding.");

					rq.Lock();
					rq.output.position = 0;

					rq.output.ValidateMessage();

					if (sending)
					{
						queue.Enqueue(rq);
					}
					else
					{
						sending = true;
						s.BeginSend(rq.output.buffer, 0, rq.output.availableData, SocketFlags.None, sendLoop, rq);
					}
				}
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				Disconnect();
			}
		}


		public void SendRequest(RequestInfo rq)
		{
			lock (syncObj)
			{
				if (!connected || disconnecting || disposed)
					throw new InvalidOperationException("Unable to send message because the connection is closed or being shutdown.");

				requests.Add(rq.msgid, rq);
				SendMessage(rq);
			}
		}


		public void StartStreamTransfer(Guid guid)
		{
			RequestInfo rq = new RequestInfo(this);
			rq.output.StartMessage(MsgOpcode.StreamRequest, 0);
			rq.output.AddArray<byte>(guid.ToByteArray());
			rq.output.EndMessage();
			SendMessage(rq);
		}


		// =========================================================================
		// Client side event subscriptions
		// =========================================================================

		private List<EventSubscriptionInfo> eventSubscriptions = new List<EventSubscriptionInfo>();

		public void ClientSideRegisterEvent(int contractid, int eventid)
		{
			bool sendMessage = false;
			lock (eventSubscriptions)
			{
				int idx = eventSubscriptions.FindIndex(p => p.ContractID == contractid && p.EventID == eventid);
				if (idx < 0)
				{
					EventSubscriptionInfo evInfo = new EventSubscriptionInfo(contractid, eventid);
					eventSubscriptions.Add(evInfo);
					sendMessage = true;
				}
			}
			if (sendMessage && IsConnected)
			{
				RequestInfo rq = new RequestInfo(this);
				rq.output.StartMessage(MsgOpcode.EventSubscription, 0);
				rq.output.AddBoolean(true);
				rq.output.AddInt32(contractid);
				rq.output.AddInt32(eventid);
				rq.output.EndMessage();
				SendMessage(rq);
			}
		}


		public void ClientSideUnregisterEvent(int contractid, int eventid)
		{
			bool sendMessage = false;
			lock (eventSubscriptions)
			{
				sendMessage = eventSubscriptions.RemoveAll(p => p.ContractID == contractid && p.EventID == eventid) > 0;
			}
			if (sendMessage && IsConnected)
			{
				RequestInfo rq = new RequestInfo(this);
				rq.output.StartMessage(MsgOpcode.EventSubscription, 0);
				rq.output.AddBoolean(false);
				rq.output.AddInt32(contractid);
				rq.output.AddInt32(eventid);
				rq.output.EndMessage();
				SendMessage(rq);
			}
		}


		internal void ResubscribeToEvents(object state)
		{
			RequestInfo rq = new RequestInfo(this);
			lock (syncObj)
			{
				foreach (EventSubscriptionInfo ev in eventSubscriptions)
				{
					rq.output.StartMessage(MsgOpcode.EventSubscription, 0);
					rq.output.AddBoolean(true);
					rq.output.AddInt32(ev.ContractID);
					rq.output.AddInt32(ev.EventID);
					rq.output.EndMessage();
				}
			}

			try
			{
				lock (syncObj)
				{
					if (rq.output.availableData > 0 && connected && !disconnecting && !disposed)
						SendMessage(rq);
				}
			}
			catch { } // Ignore communication errors at this point
		}

        internal void SimulateClientReceiveInvalidMessage()
        {
            simulateClientReceiveInvalidMessage = true;
        }
    }
}
