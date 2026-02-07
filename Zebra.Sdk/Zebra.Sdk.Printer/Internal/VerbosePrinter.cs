using System;

namespace Zebra.Sdk.Printer.Internal
{
	internal class VerbosePrinter
	{
		private bool shouldPrint;

		public VerbosePrinter(bool verboseFlag)
		{
			this.shouldPrint = verboseFlag;
		}

		public void Write(string msg)
		{
			if (this.shouldPrint)
			{
				Console.Write(msg);
			}
		}

		public void WriteLine(string msg)
		{
			if (this.shouldPrint)
			{
				Console.WriteLine(msg);
			}
		}
	}
}