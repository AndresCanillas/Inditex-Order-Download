using System;
using System.Threading.Tasks;

namespace Zebra.Sdk.Util.Internal
{
	internal class Sleeper
	{
		private static Sleeper sleeper;

		protected Sleeper()
		{
		}

		private static Sleeper GetInstance()
		{
			if (Sleeper.sleeper == null)
			{
				Sleeper.sleeper = new Sleeper();
			}
			return Sleeper.sleeper;
		}

		protected virtual void PerformSleep(long millis)
		{
			try
			{
				Task.Delay((int)millis).Wait();
			}
			catch (Exception)
			{
			}
		}

		public static void Sleep(long millis)
		{
			Sleeper.GetInstance().PerformSleep(millis);
		}
	}
}