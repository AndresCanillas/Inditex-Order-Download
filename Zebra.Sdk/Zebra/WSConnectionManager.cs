using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using WebLink.Contracts;
using WebLink.Services;
using WebLink.Services.Zebra.Commands;
using Service.Contracts;
using Services.Core;

namespace WebLink.Services.Zebra
{
	public class WSConnectionManager : IWSConnectionManager
	{
		private IFactory factory;
		private ILogService log;
        private IZPrinterManager printerManager;

        private ConcurrentDictionary<int, IWSConnection> connections = new ConcurrentDictionary<int, IWSConnection>();

		public WSConnectionManager(
			IFactory factory,
			ILogService log,
            IZPrinterManager printerManager,
			IApplicationLifetime appLifeTime)
		{
            this.log = log;
            this.factory = factory;
			this.printerManager = printerManager;
			appLifeTime.ApplicationStopping.Register(() => Dispose());
		}


		public void Dispose()
		{
			try
			{
				List<IWSConnection> list = new List<IWSConnection>(connections.Values);
				foreach (var conn in list)
				{
					conn.Disconnect();
				}
			}
			catch (Exception ex)
			{
				log.LogException(ex);
			}
		}


		public async Task Accept(WebSocket socket, string ip)
		{
            var conn = factory.GetInstance<IWSConnection>();
			if (!connections.TryAdd(conn.InternalID, conn))
				throw new Exception("Could not add printer to internal collection.");
			conn.OnInitialized += Conn_OnInitialized;
			conn.OnDisconnect += Conn_OnDisconnect;
			await conn.ProcessConnection(socket, ip);
		}


		private void Conn_OnInitialized(object sender, EventArgs e)
		{
			try
			{
				IWSConnection conn = sender as IWSConnection;
				var printer = printerManager.GetPrinter(conn.DeviceID);
				if (printer == null)
				{
					if (conn.ChannelType == ChannelType.Weblink)
					{
						printer = printerManager.RegisterPrinter(conn);
					}
					else
					{
						Console.WriteLine("Received a connection from an unregistered printer.");
						conn.Disconnect();
						return;
					}
				}
                if (conn.ChannelType != ChannelType.Weblink)
                {
                    log.LogMessage($"Received connection of type: {conn.ChannelType}");
                    printer.RegisterChannel(conn);
                }
                else
                    printer.MainChannel.SendCommand(new OpenRawChannel());
            }
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.WriteLine(ex.StackTrace);
			}
		}


		private void Conn_OnDisconnect(object sender, EventArgs e)
		{
			try
			{
				IWSConnection conn = sender as IWSConnection;
                connections.TryRemove(conn.InternalID, out IWSConnection _);
				printerManager.RemovePrinter(conn.DeviceID);
			}
			catch(Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.WriteLine(ex.StackTrace);
			}
		}


		public List<IWSConnection> GetList()
		{
			var result = new List<IWSConnection>();
			foreach (var p in connections.Values)
				result.Add(p);
			result.Sort((p1, p2) => (int)((p2.ConnectedSince - p1.ConnectedSince).Ticks));
			return result;
		}


		public IWSConnection GetConnection(int connectionid) =>
			connections.Values.Where(conn => conn.InternalID == connectionid).FirstOrDefault();
	}
}
