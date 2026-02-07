using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services.Zebra
{
    public class ZPrinterManager : IZPrinterManager
    {
		private IFactory factory;
		private ILocationRepository locationRepo;
		private IPrinterRepository printerRepo;
		private IEventQueue events;
		private ILocalizationService g;
		private ILogService log;

		private ConcurrentDictionary<string, IZPrinter> printers = new ConcurrentDictionary<string, IZPrinter>();
		private ConcurrentDictionary<int, PrinterState> states = new ConcurrentDictionary<int, PrinterState>();

		private Timer timer;
		private string subToken;

		public ZPrinterManager(
			IFactory factory,
			ILocationRepository locationRepo,
			IPrinterRepository printerRepo,
			IEventQueue events,
			ILocalizationService g,
			ILogService log)
		{
			this.factory = factory;
			this.locationRepo = locationRepo;
			this.printerRepo = printerRepo;
			this.events = events;
			this.log = log;
			this.g = g;
			subToken = events.Subscribe<PrinterJobEvent>(HandleEvent);
			timer = new Timer(CheckPrinters, null, (int)TimeSpan.FromSeconds(10).TotalMilliseconds, Timeout.Infinite);
		}

		private async void CheckPrinters(object o)
		{
			try
			{
				using (var ctx = factory.GetInstance<PrintDB>())
				{
					var locations = await locationRepo.GetListAsync(ctx);
					var printers = await printerRepo.GetListAsync(ctx);
					List<CompactPrinterState> data = new List<CompactPrinterState>(printers.Count);
					foreach (var printer in printers)
					{
						var state = GetPrinterState(printer.ID);
						var conn = GetPrinter(printer.DeviceID);
						if (conn != null && conn.Connected)
						{
							state.Online = true;
							if (!conn.IsPrinting())
							{
								try
								{
									var actualState = await conn.GetPrinterState();
									state.Ready = actualState.Ready;
									state.Paused = actualState.Paused;
									state.PaperOut = actualState.PaperOut;
									state.RibbonOut = actualState.RibbonOut;
									state.HeadOpen = actualState.HeadOpen;
									if (state.Ready)
										conn.ResumeWork();
								}
								catch { }
							}
						}
						else
						{
							state.Online = false;
							state.Ready = false;
							state.Paused = false;
							state.PaperOut = false;
							state.RibbonOut = false;
							state.HeadOpen = false;
						}
						data.Add(new CompactPrinterState(state));
					}
					events.Send(new PrinterJobEvent(0, PrinterJobEventType.AllPrinterStatus, data));
				}
			}
			catch (Exception ex)
			{
				log.LogException(ex);
			}
			finally
			{
				timer.Change((int)TimeSpan.FromSeconds(30).TotalMilliseconds, Timeout.Infinite);
			}
		}


		private void HandleEvent(PrinterJobEvent e)
		{
			if(e.Type == PrinterJobEventType.PrinterStatus)
			{
				var data = (e.Data as CompactPrinterState);
				var state = GetPrinterState(data.ID);
				state.Online = true;
				state.Ready = (data.FL & 2) != 0;
				state.Paused = (data.FL & 4) != 0;
				state.PaperOut = (data.FL & 8) != 0;
				state.RibbonOut = (data.FL & 16) != 0;
				state.HeadOpen = (data.FL & 32) != 0;
			}
		}

		private PrinterState GetPrinterState(int id)
		{
			PrinterState state;
			if (!states.TryGetValue(id, out state))
			{
				state = new PrinterState() { ID = id };
				states.TryAdd(id, state);
			}
			return state;
		}


		public List<IZPrinter> GetList()
		{
			var result = new List<IZPrinter>(printers.Values);
			result.Sort((p1, p2) => (int)((p2.MainChannel.ConnectedSince - p1.MainChannel.ConnectedSince).Ticks));
			return result;
		}


		public List<PrinterState> GetPrinterStates()
		{
			return new List<PrinterState>(states.Values);
		}


		public IZPrinter GetPrinter(string deviceid)
		{
			IZPrinter printer;
			if (deviceid != null && printers.TryGetValue(deviceid, out printer))
				return printer;
			return null;
		}


		public IZPrinter GetPrinter(int id) =>
			printers.Values.Where(p => p.ID == id).FirstOrDefault();


		public IZPrinter RegisterPrinter(IWSConnection conn)
		{
			if (conn == null)
				throw new ArgumentNullException(nameof(conn));
			if(String.IsNullOrWhiteSpace(conn.DeviceID))
				throw new InvalidOperationException("The IWSConnection object is not in a valid state.");
			if(conn.ChannelType != ChannelType.Weblink)
				throw new InvalidOperationException("The IWSConnection object is not the main Weblink Channel.");
			var printer = factory.GetInstance<IZPrinter>();
			printer.MainChannel = conn;
			printers.TryAdd(conn.DeviceID, printer);
			events.Send(new PrinterConnectedEvent(printer.DeviceID, printer.ProductName, printer.Firmware));
			return printer;
		}


		public void RegisterChannel(IWSConnection conn)
		{
			IZPrinter printer;
			if (printers.TryGetValue(conn.DeviceID, out printer))
				printer.RegisterChannel(conn);
			else
				conn.Disconnect();
		}

		public void RemovePrinter(string deviceid)
		{
			IZPrinter printer;
            if (deviceid != null)
            {
                if (printers.TryRemove(deviceid, out printer))
                {
                    printer.Dispose();
                }
            }
		}
	}
}
