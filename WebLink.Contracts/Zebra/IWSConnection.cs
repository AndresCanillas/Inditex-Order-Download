using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace WebLink.Contracts
{
	public interface IWSConnection
	{
		event EventHandler<ReceiveEventArgs> OnReceive;
		event EventHandler<SendEventArgs> OnSend;
		event EventHandler OnInitialized;
		event EventHandler OnDisconnect;
		int InternalID { get; }
		bool IsConnected { get; }
		string DeviceID { get; }
		Dictionary<string, string> DeviceProperties { get; }
		DateTime ConnectedSince { get; }
		ChannelType ChannelType { get; }
		IEnumerable<string> ReceivedMessages { get; }
		IEnumerable<string> SentMessages { get; }
		void ClearReceivedMessages();
		void ClearSentMessages();
		string SentMessagesAsText { get; }
		string ReceivedMessagesAsText { get; }
		Task ProcessConnection(WebSocket socket, string ip);
		Task<string> SendCommand(IZCommand command);
		void Disconnect();
	}

	public enum ChannelType
	{
		Unknown,
		Weblink,
		Raw,
		Config
	}

	public class ReceiveEventArgs
	{
		public string Message { get; set; }
	}


	public class SendEventArgs
	{
		public string Message { get; set; }
	}
}
