using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Runtime;
using System.Text;

namespace WebLink.Services.Automated
{
	public class ForceCollection : IAutomatedProcess
	{
		private TimeSpan idleTime = TimeSpan.FromMinutes(5);

		public TimeSpan GetIdleTime()
		{
			return idleTime;
		}

		public void OnLoad()
		{
		}

		public void OnExecute()
		{
			GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
			GC.Collect(2, GCCollectionMode.Forced, true, true);
		}

		public void OnUnload()
		{
		}
	}
}
