using System;

namespace Zebra.Sdk.Printer.Operations.Internal
{
	internal abstract class PrinterOperation<T>
	{
		protected PrinterOperation()
		{
		}

		public virtual T Execute()
		{
			throw new NotImplementedException();
		}
	}
}