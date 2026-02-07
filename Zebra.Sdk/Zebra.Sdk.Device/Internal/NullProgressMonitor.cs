using System;
using Zebra.Sdk.Device;

namespace Zebra.Sdk.Device.Internal
{
	internal class NullProgressMonitor : ProgressMonitor
	{
		public NullProgressMonitor()
		{
		}

		public override void UpdateProgress(int bytesWritten, int totalBytes)
		{
		}
	}
}