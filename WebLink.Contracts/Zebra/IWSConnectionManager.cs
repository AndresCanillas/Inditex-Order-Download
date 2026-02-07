using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace WebLink.Contracts
{
	public interface IWSConnectionManager : IDisposable
	{
		Task Accept(WebSocket socket, string ip);
		List<IWSConnection> GetList();
		IWSConnection GetConnection(int connectionid);
	}
}
