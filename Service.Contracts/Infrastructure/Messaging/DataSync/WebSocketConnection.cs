using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Service.Contracts.Messaging
{
	public class WebSocketConnection : IDisposable
	{
		private readonly object syncObj = new object();
		private bool connected;
		private readonly ConcurrentQueue<SocketMsgInfo> tosend = new ConcurrentQueue<SocketMsgInfo>();
		private ClientWebSocket socket;


		public event EventHandler Connected;
		public event EventHandler Disconnected;
		public event EventHandler<SocketMsgInfo> MessageReceived;
		public event EventHandler<SocketMsgInfo> MessageSent;
		public event EventHandler<string> ErrorRaised;


		public string Url { get; set; }


		public bool IsConnected { get => connected; }


		public void Dispose()
		{
			Disconnect().Wait();
		}


		public async Task Connect()
		{
			bool alreadyConnected = false;
			lock (syncObj) if (connected) alreadyConnected = true;
			if (alreadyConnected) return;
			socket = new ClientWebSocket();
			await socket.ConnectAsync(new Uri(Url), CancellationToken.None);
			lock (syncObj) connected = true;
			StartReceiveLoop();
			StartSendLoop();
			Connected?.Invoke(this, EventArgs.Empty);
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
				RaiseDisconnected();
			}
		}


		public void Send(string message)
		{
			lock (syncObj)
			{
				if (!connected)
					throw new Exception("Not connected");
			}
			tosend.Enqueue(new SocketMsgInfo(message));
		}


		public void Send(byte[] data)
		{
			lock (syncObj)
			{
				if (!connected)
					throw new Exception("Not connected");
			}
			tosend.Enqueue(new SocketMsgInfo(data));
		}


		private async void StartReceiveLoop()
		{
			var pos = 0;
			var buffer = new byte[1024 * 10];
			try
			{
				while (socket.State == WebSocketState.Open)
				{
					var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer, pos, buffer.Length - pos), CancellationToken.None);
					if (socket.State == WebSocketState.Open)
					{
						pos += result.Count;
						if (pos <= buffer.Length)
						{
							if (result.EndOfMessage)
							{
								if (result.MessageType == WebSocketMessageType.Text)
								{
									var message = Encoding.UTF8.GetString(buffer, 0, pos);
									pos = 0;
									MessageReceived?.Invoke(this, new SocketMsgInfo(message));
								}
								else if (result.MessageType == WebSocketMessageType.Binary)
								{
									var data = new byte[pos];
									Buffer.BlockCopy(buffer, 0, data, 0, pos);
									pos = 0;
									MessageReceived?.Invoke(this, new SocketMsgInfo(data));
								}
							}
						}
						else
						{
							await socket.CloseAsync(WebSocketCloseStatus.MessageTooBig, "501", CancellationToken.None);
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
				ErrorRaised?.Invoke(this, ex.Message);
			}
			finally
			{
				RaiseDisconnected();
			}
		}


		private void RaiseDisconnected()
		{
			var raiseEvent = false;
			lock (syncObj)
			{
				if (connected)
				{
					connected = false;
					raiseEvent = true;
					socket.Dispose();
				}
			}
			while (tosend.Count > 0)
				tosend.TryDequeue(out _);
			if (raiseEvent) Disconnected?.Invoke(this, EventArgs.Empty);
		}


		private async void StartSendLoop()
		{
			try
			{
				do
				{
					var msg = await WaitForMessage();
					if (msg != null && socket.State == WebSocketState.Open)
					{
						if (msg.IsBinary)
							await socket.SendAsync(new ArraySegment<byte>(msg.Data), WebSocketMessageType.Binary, true, CancellationToken.None);
						else
							await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(msg.Message)), WebSocketMessageType.Text, true, CancellationToken.None);
						MessageSent?.Invoke(this, msg);
					}
				} while (socket.State == WebSocketState.Open);
			}
			catch (Exception ex)
			{
				ErrorRaised?.Invoke(this, ex.Message);
				try
				{
					await socket.CloseAsync(WebSocketCloseStatus.InternalServerError, "500", CancellationToken.None);
				}
				catch { }
			}
			finally
			{
				RaiseDisconnected();
			}
		}


		private async Task<SocketMsgInfo> WaitForMessage()
		{
			SocketMsgInfo element;
			while (socket.State == WebSocketState.Open)
			{
				if (tosend.Count == 0)
					await Task.Delay(100).ConfigureAwait(false);
				if (tosend.TryDequeue(out element))
					return element;
			}
			return null;
		}
	}


	public class SocketMsgInfo
	{
		public string Message;
		public byte[] Data;
		public bool IsBinary;

		public SocketMsgInfo(string message)
		{
			Message = message;
			IsBinary = false;
		}

		public SocketMsgInfo(byte[] data)
		{
			Data = data;
			IsBinary = true;
		}

		public string FormatBinary()
		{
			if (Data == null) return "(EMPTY)";
			StringBuilder sb = new StringBuilder(Data.Length * 3);
			for (int idx = 0; idx < Data.Length; idx++)
			{
				var b = Data[idx];
				if (idx > 0 && idx % 16 == 0) sb.Append("\r\n");
				sb.Append($"{b.ToString("X2")} ");
			}
			return sb.ToString();
		}

		public string FormatSATO()
		{
			if (Data == null) return "(EMPTY)";
			StringBuilder sb = new StringBuilder(Data.Length * 3);
			for (int idx = 0; idx < Data.Length; idx++)
			{
				var b = Data[idx];
				if (b < 0x1B)
				{
					sb.Append($" 0x{b.ToString("X2")} ");
				}
				else if (b == 0x1B)
				{
					idx++;
					sb.Append($"\r\n<{(char)Data[idx]}>");
				}
				else
				{
					sb.Append((char)b);
				}
			}
			return sb.ToString();
		}
	}
}
