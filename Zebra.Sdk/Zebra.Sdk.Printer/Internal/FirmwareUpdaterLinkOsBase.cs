using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Comm.Internal;
using Zebra.Sdk.Device;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Printer.Discovery;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Internal
{
	internal class FirmwareUpdaterLinkOsBase : FirmwareUpdaterLinkOs
	{
		private static int MIN_TIMEOUT_MS;

		private string firmwareFilePath;

		protected Connection zebraPrinterConnection;

		protected virtual string ConnectionAddress
		{
			get
			{
				if (this.zebraPrinterConnection is TcpConnection)
				{
					return ((TcpConnection)this.zebraPrinterConnection).Address;
				}
				if (!(this.zebraPrinterConnection is MultichannelTcpConnection))
				{
					return "";
				}
				return ((TcpConnection)((MultichannelTcpConnection)this.zebraPrinterConnection).PrintingChannel).Address;
			}
		}

		protected virtual string ConnectionPortNumber
		{
			get
			{
				return ((TcpConnection)this.zebraPrinterConnection).PortNumber;
			}
		}

		protected virtual string ConnectionString
		{
			get
			{
				return string.Format("TCP:{0}:{1}", this.ConnectionAddress, this.ConnectionPortNumber);
			}
		}

		protected virtual bool IsTcpConnection
		{
			get
			{
				return this.zebraPrinterConnection is TcpConnection;
			}
		}

		static FirmwareUpdaterLinkOsBase()
		{
			FirmwareUpdaterLinkOsBase.MIN_TIMEOUT_MS = 600000;
		}

		public FirmwareUpdaterLinkOsBase(ZebraPrinterLinkOs zebraPrinter)
		{
			this.zebraPrinterConnection = zebraPrinter.Connection;
		}

		protected virtual void DoFwDownload(string firmwareFilePath, long timeout, FirmwareUpdateHandler handler)
		{
			ConnectionReestablisher connectionReestablisher = this.zebraPrinterConnection.GetConnectionReestablisher((long)180000);
			long num = (long)Math.Abs(Environment.TickCount) + (timeout < (long)FirmwareUpdaterLinkOsBase.MIN_TIMEOUT_MS ? (long)FirmwareUpdaterLinkOsBase.MIN_TIMEOUT_MS : timeout);
			bool flag = ReflectionUtil.IsDriverConnection(this.zebraPrinterConnection);
			bool flag1 = ReflectionUtil.IsUsbDirectConnection(this.zebraPrinterConnection);
			this.firmwareFilePath = firmwareFilePath;
			if (this.IsTcpConnection)
			{
				this.DownloadFwViaThreadAndWaitForFailure(num, handler);
			}
			else if (this.zebraPrinterConnection is MultichannelTcpConnection)
			{
				ZebraPrinterFactory.GetInstance(this.zebraPrinterConnection).SendFileContents(firmwareFilePath, new FirmwareUpdaterLinkOsBase.FirmwareUpdateProgressMonitor()
				{
					updateProgress = (int bytesWritten, int totalBytes) => handler.progressUpdate(bytesWritten, totalBytes)
				});
			}
			else if (!(this.zebraPrinterConnection is MultichannelConnection))
			{
				ZebraPrinterFactory.GetInstance(this.zebraPrinterConnection).SendFileContents(firmwareFilePath, new FirmwareUpdaterLinkOsBase.FirmwareUpdateProgressMonitor()
				{
					updateProgress = (int bytesWritten, int totalBytes) => handler.progressUpdate(bytesWritten, totalBytes)
				});
			}
			else
			{
				ZebraPrinterFactory.GetInstance(this.zebraPrinterConnection).SendFileContents(firmwareFilePath, new FirmwareUpdaterLinkOsBase.FirmwareUpdateProgressMonitor()
				{
					updateProgress = (int bytesWritten, int totalBytes) => handler.progressUpdate(bytesWritten, totalBytes)
				});
			}
			handler.firmwareDownloadComplete();
			if (!(flag | flag1))
			{
				Sleeper.Sleep((long)30000);
				this.WaitForPrinterToGoOffline(num);
				this.zebraPrinterConnection.Close();
			}
			else
			{
				this.zebraPrinterConnection.Close();
				Sleeper.Sleep((long)90000);
			}
			connectionReestablisher.ReestablishConnection(handler);
		}

		private void DownloadFwViaThreadAndWaitForFailure(long thresholdTime, FirmwareUpdateHandlerBase handler)
		{
			Connection connection = ConnectionBuilderInternal.Build(this.ConnectionString);
			FirmwareDownloadRunnable firmwareDownloadRunnable = new FirmwareDownloadRunnable(this.zebraPrinterConnection, this.firmwareFilePath, handler);
			Task task = Task.Run(() => firmwareDownloadRunnable.Run());
			connection.Open();
			while ((long)Math.Abs(Environment.TickCount) < thresholdTime && (task.Status == TaskStatus.Running || task.Status == TaskStatus.WaitingToRun))
			{
				Sleeper.Sleep((long)2500);
				connection.Write(Encoding.UTF8.GetBytes(SGDUtilities.DecorateWithGetCommand(SGDUtilities.HOST_STATUS)));
				if (connection.Read() == null)
				{
					continue;
				}
				connection.Write(Encoding.UTF8.GetBytes(SGDUtilities.DecorateWithGetCommand(SGDUtilities.DEVICE_RESET)));
				connection.Close();
				throw new ZebraIllegalArgumentException("Firmware not accepted by printer, rebooting printer.  Please verify firmware is valid.");
			}
			connection.Close();
			if (firmwareDownloadRunnable.ExceptionOccured)
			{
				throw new ConnectionException(firmwareDownloadRunnable.ExceptionMessage);
			}
		}

		private string ExtractFirmwareVersion(string firmwareFilePath)
		{
			string str = "";
			using (FileStream fileStream = new FileStream(firmwareFilePath, FileMode.Open))
			{
				str = FirmwareUtil.ExtractFirmwareVersion(fileStream);
			}
			return str;
		}

		private bool IsOnlySettingsChannelOpen(MultichannelConnection multiChannelConnection)
		{
			if (!multiChannelConnection.StatusChannel.Connected)
			{
				return false;
			}
			return !multiChannelConnection.PrintingChannel.Connected;
		}

		private void ThrowExceptionStatusOnly()
		{
			MultichannelConnection multichannelConnection = this.zebraPrinterConnection as MultichannelConnection;
			MultichannelConnection multichannelConnection1 = multichannelConnection;
			if (multichannelConnection != null)
			{
				if (this.IsOnlySettingsChannelOpen(multichannelConnection1))
				{
					throw new ConnectionException("Cannot upgrade firmware with only the status channel open");
				}
			}
			else if (this.zebraPrinterConnection is StatusConnection)
			{
				throw new ConnectionException("Cannot upgrade firmware on the status channel");
			}
		}

		private void TimeoutCheck(long thresholdTime)
		{
			if ((long)Math.Abs(Environment.TickCount) > thresholdTime)
			{
				throw new TimeoutException(string.Concat("Firmware downloader timed out waiting for '", this.zebraPrinterConnection, "' to come back online"));
			}
		}

		public void UpdateFirmware(string firmwareFilePath, FirmwareUpdateHandler handler)
		{
			this.UpdateFirmware(firmwareFilePath, (long)FirmwareUpdaterLinkOsBase.MIN_TIMEOUT_MS, handler, false);
		}

		public void UpdateFirmware(string firmwareFilePath, long timeout, FirmwareUpdateHandler handler)
		{
			this.UpdateFirmware(firmwareFilePath, timeout, handler, false);
		}

		private void UpdateFirmware(string firmwareFilePath, long timeout, FirmwareUpdateHandler handler, bool downloadUnconditionaly)
		{
			if (!File.Exists(firmwareFilePath))
			{
				throw new FileNotFoundException(string.Concat(firmwareFilePath, " does not exist."));
			}
			this.ThrowExceptionStatusOnly();
			string str = this.ExtractFirmwareVersion(firmwareFilePath);
			if (downloadUnconditionaly || FirmwareUtil.FirmwareVersionsDontMatch(str, this.zebraPrinterConnection))
			{
				this.DoFwDownload(firmwareFilePath, timeout, handler);
				return;
			}
			handler.firmwareDownloadComplete();
			handler.printerOnline(ZebraPrinterFactory.GetLinkOsPrinter(this.zebraPrinterConnection), FirmwareUtil.GetFWVersionFromPrinterConnection(this.zebraPrinterConnection).Trim());
		}

		public void UpdateFirmwareUnconditionally(string firmwareFilePath, FirmwareUpdateHandler handler)
		{
			this.UpdateFirmware(firmwareFilePath, (long)FirmwareUpdaterLinkOsBase.MIN_TIMEOUT_MS, handler, true);
		}

		public void UpdateFirmwareUnconditionally(string firmwareFilePath, long timeout, FirmwareUpdateHandler handler)
		{
			this.UpdateFirmware(firmwareFilePath, timeout, handler, true);
		}

		protected virtual void WaitForPrinterToGoOffline(long thresholdTime)
		{
			while (true)
			{
				Sleeper.Sleep((long)5000);
				try
				{
					byte[] numArray = this.zebraPrinterConnection.SendAndWaitForResponse(Encoding.UTF8.GetBytes(SGDUtilities.DecorateWithGetCommand(SGDUtilities.APPL_NAME)), 5000, 5000, null);
					if (numArray == null || numArray.Length == 0)
					{
						break;
					}
				}
				catch (ConnectionException)
				{
					break;
				}
				this.TimeoutCheck(thresholdTime);
			}
		}

		private class FirmwareUpdateProgressMonitor : ProgressMonitor
		{
			public FirmwareUpdateProgressMonitor()
			{
			}

			public override void UpdateProgress(int bytesWritten, int totalBytes)
			{
				throw new NotImplementedException();
			}
		}

		private class SinglePrinterDiscoveryHandler : DiscoveryHandler
		{
			public bool isFinished;

			public DiscoveredPrinter printer;

			public SinglePrinterDiscoveryHandler()
			{
			}

			public void DiscoveryError(string message)
			{
			}

			public void DiscoveryFinished()
			{
				this.isFinished = true;
			}

			public void FoundPrinter(DiscoveredPrinter printer)
			{
				this.printer = printer;
				this.isFinished = true;
			}
		}
	}
}