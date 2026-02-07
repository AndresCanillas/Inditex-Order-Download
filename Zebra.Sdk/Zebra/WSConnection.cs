using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebLink.Contracts;
using WebLink.Services;
using WebLink.Services.Zebra.Commands;
using Zebra.Sdk.Printer.Discovery.Internal;

namespace WebLink.Services.Zebra
{
	public class WSConnection : IWSConnection
	{
        private ILogService log;
		private WebSocket socket;
		private ConcurrentQueue<WSQueueElement> outq = new ConcurrentQueue<WSQueueElement>();
		private volatile bool connected;
		private List<string> received = new List<string>();
		private List<string> sent = new List<string>();
        private object syncObj = new object();
        private volatile IZCommand currentCommand;


		public event EventHandler<ReceiveEventArgs> OnReceive;
		public event EventHandler<SendEventArgs> OnSend;
		public event EventHandler OnInitialized;
		public event EventHandler OnDisconnect;


		public WSConnection(ILogService logService, IMemorySequence sequence)
		{
            log = logService;
            InternalID = sequence.NextID();
        }


        public int InternalID { get; set; }


		public Dictionary<string, string> DeviceProperties { get; set; }


		public bool IsConnected
		{
			get
			{
				return connected;
			}
		}


		public string DeviceID { get; set; }


		public DateTime ConnectedSince { get; set; }


		public ChannelType ChannelType { get; set; }


		public IEnumerable<string> ReceivedMessages
        {
            get
            {
                lock (received)
                    return new List<string>(received);
            }
        }


        public string ReceivedMessagesAsText
        {
            get
            {
                StringBuilder sb = new StringBuilder(1000);
                lock (received)
                {
                    foreach (string s in received)
                        sb.AppendLine(s);
                }
                return sb.ToString();
            }
        }


        public void ClearReceivedMessages()
        {
            lock (received)
                received.Clear();
        }


        public IEnumerable<string> SentMessages
        {
            get
            {
                lock (sent)
                    return new List<string>(sent);
            }
        }


        public string SentMessagesAsText
        {
            get
            {
                StringBuilder sb = new StringBuilder(1000);
                lock (sent)
                {
                    foreach (string s in sent)
                        sb.AppendLine(s);
                }
                return sb.ToString();
            }
        }


        public void ClearSentMessages()
        {
            lock (sent)
                sent.Clear();
        }



        public async Task<string> SendCommand(IZCommand command)
        {
            outq.Enqueue(new WSQueueElement(command));
			await command.WaitForTransmission();
            if (command.IsOneWay)
            {
                return null;
            }
            else
            {
                return await command.WaitForResponse();
            }
        }


		public async void Disconnect()
		{
			try
			{
				if (connected && (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived))
					await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
				connected = false;
			}
			catch { }
			finally
			{
                log.LogMessage($"(ID: {InternalID}): WS Connection disconnected.");
				OnDisconnect?.Invoke(this, EventArgs.Empty);
                socket.Dispose();
            }
        }
		

		public async Task ProcessConnection(WebSocket socket, string ip)
		{
            this.socket = socket;
            connected = true;
            ConnectedSince = DateTime.UtcNow;
            ChannelType = ChannelType.Unknown;
            var buffer = new byte[1024 * 4];
			try
			{
                log.LogMessage($"(ID: {InternalID}): Waiting for initial channel message. {ip}");
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
				string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
				RaiseOnReceive(message);
				InitializeChannel(message);
                log.LogMessage($"(ID: {InternalID}): Channel initialized as {ChannelType}");
                StartReceiveLoop();
                while (socket.State == WebSocketState.Open && result.CloseStatus == null)
				{
					var element = await WaitForCommand();
                    if (element != null)
                    {
                        if (!element.Command.IsOneWay)
                        {
                            lock (syncObj)
                                currentCommand = element.Command;
                        }
						try
						{
							await InternalSendCommand(element);
						}
						catch(Exception ex)
						{
							if (currentCommand != null)
								currentCommand.SetError(ex);
							else
								log.LogException(ex);
						}
                    }
				}
			}
			catch (Exception ex)
			{
                log.LogException($"Error in ProcessConnection for IP ({ip}).", ex);
			}
			Disconnect();
		}

        private async void StartReceiveLoop()
        {
            var buffer = new byte[1024 * 4];
            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    var response = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    RaiseOnReceive(response);
                    lock (syncObj)
                    {
                        if (currentCommand != null)
                        {
                            currentCommand.SetResponse(response);
                            currentCommand = null;
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                log.LogException("Exception while receiving from connection. Will close the channel. DeviceID " + DeviceID, ex);
            }
        }

        private async Task InternalSendCommand(WSQueueElement element)
		{
			byte[] buffer = element.CommandBody;
			await socket.SendAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), WebSocketMessageType.Binary, true, CancellationToken.None);
			var message = Encoding.UTF8.GetString(buffer);
            element.Command.SetTransmission();
			RaiseOnSend(message);
		}


		private void InitializeChannel(string message)
		{
			if (String.IsNullOrWhiteSpace(message))
			{
				Disconnect();
				return;
			}
			JObject o = JObject.Parse(message);
			if (o.ContainsKey("discovery_b64"))
			{
				var discoveryPacket = o.Value<string>("discovery_b64");
                if (discoveryPacket.Contains(":"))
                {
                    discoveryPacket = discoveryPacket.Substring(0, discoveryPacket.IndexOf(':'));
                }
                var packet = Convert.FromBase64String(discoveryPacket);
				DeviceProperties = DiscoveredPrinterNetworkFactory.GetDiscoveredPrinterNetwork(packet).DiscoveryDataMap;
				DeviceID = DeviceProperties["SERIAL_NUMBER"];
				ChannelType = ChannelType.Weblink;
				log.LogMessage("Device ID: {0}", DeviceID);
			}
			else if (o.ContainsKey("channel_name"))
			{
				DeviceID = o.Value<string>("unique_id");
				var channelName = o.Value<string>("channel_name");
				if (channelName.Contains(".raw."))
					ChannelType = ChannelType.Raw;
				else if (channelName.Contains(".config."))
					ChannelType = ChannelType.Config;
				log.LogMessage("Device ID: {0}", DeviceID);
			}
			if(ChannelType != ChannelType.Unknown)
			{
				OnInitialized?.Invoke(this, EventArgs.Empty);
			}
			else
			{
				Disconnect();
			}
		}


		private async Task<WSQueueElement> WaitForCommand()
		{
			WSQueueElement element;
			while (connected)
			{
				while (connected && outq.Count == 0 && socket.State == WebSocketState.Open)
					await Task.Delay(100).ConfigureAwait(false);
                if(socket.State != WebSocketState.Open)
                {
                    Disconnect();
                    return null;
                }
				if (connected && outq.TryDequeue(out element))
					return element;
			}
			return null;
		}


		private void RaiseOnReceive(string message)
		{
            lock (received)
            {
                received.Add(message);
                while (received.Count > 50)
                    received.RemoveAt(0);
            }
			ThreadPool.QueueUserWorkItem(InvokeOnReceive, message);
		}

		private void InvokeOnReceive(object state)
		{
			try
			{
				string message = (string)state;
				OnReceive?.Invoke(this, new ReceiveEventArgs() { Message = message });
			}
			catch(Exception ex)
			{
				log.LogException(ex);
			}
		}

		private void RaiseOnSend(string message)
		{
            lock (sent)
            {
                sent.Add(message);
                while (sent.Count > 50)
                    sent.RemoveAt(0);
            }
			OnSend?.Invoke(this, new SendEventArgs() { Message = message });
		}
	}

	class WSQueueElement
	{
		public IZCommand Command;
		public byte[] CommandBody;

		public WSQueueElement(IZCommand command)
		{
			Command = command;
			CommandBody = command.ToByteArray();
		}
	}
}
