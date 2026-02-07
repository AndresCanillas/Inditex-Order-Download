using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using WebLink.Contracts;
using WebLink.Models;

namespace WebLink.Services
{
	public class CatalogDataRemediation: IAutomatedProcess
	{
		private IServiceProvider sp;
		private volatile bool keepAlive = true;
		private ManualResetEvent waitHandle = new ManualResetEvent(false);
		private Thread t;

		public CatalogDataRemediation(IServiceProvider sp)
		{
			this.sp = sp;
		}

		public TimeSpan GetIdleTime()
		{
			return TimeSpan.MaxValue;
		}

		public void OnLoad()
		{
			t = new Thread(run);
			t.Start();
		}

		public void OnUnload()
		{
			keepAlive = false;
			waitHandle.WaitOne();
		}


		private void run(object st)
		{
			try
			{
				do
				{
					// Process whatever

					Thread.Sleep(1000);
				} while (keepAlive);
			}
			finally
			{
				waitHandle.Set();
			}
		}

		public void OnExecute()
		{
		}
	}
}
