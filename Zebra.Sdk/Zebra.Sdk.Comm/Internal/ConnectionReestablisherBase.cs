using System;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Comm.Internal
{
	internal abstract class ConnectionReestablisherBase : ConnectionReestablisher
	{
		protected Connection zebraPrinterConnection;

		protected long thresholdTime;

		protected long startTime;

		internal Action<PrinterReconnectionHandler> reestablishConnection;

		protected ConnectionReestablisherBase(Connection c, long thresholdTime)
		{
			ConnectionReestablisherBase connectionReestablisherBase = this;
			this.reestablishConnection = new Action<PrinterReconnectionHandler>(connectionReestablisherBase.ReestablishConnection);
			this.zebraPrinterConnection = c;
			this.thresholdTime = thresholdTime;
		}

		public abstract void ReestablishConnection(PrinterReconnectionHandler handler);

		protected void TimeoutCheck()
		{
			if ((long)Math.Abs(Environment.TickCount) > this.startTime + this.thresholdTime)
			{
				throw new TimeoutException(string.Format("Task timed out waiting for '{0}' to come back online", this.zebraPrinterConnection));
			}
		}

		protected string WaitForPrinterToComeOnlineViaSgdAndGetFwVer(Connection connection)
		{
			string str;
			this.startTime = (long)Math.Abs(Environment.TickCount);
			while (true)
			{
				try
				{
					connection.Open();
					string str1 = SGD.GET("appl.name", connection);
					if (string.IsNullOrEmpty(str1.Trim()))
					{
						throw new ConnectionException("Printer is not responding");
					}
					str = str1;
					break;
				}
				catch (ConnectionException)
				{
					try
					{
						connection.Close();
					}
					catch (ConnectionException)
					{
					}
				}
				Sleeper.Sleep((long)2500);
				this.TimeoutCheck();
			}
			return str;
		}
	}
}