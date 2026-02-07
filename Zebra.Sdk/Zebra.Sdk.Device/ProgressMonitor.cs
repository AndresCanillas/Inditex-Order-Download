using System;

namespace Zebra.Sdk.Device
{
	/// <summary>
	///       Handler to monitor long-running file operations.
	///       </summary>
	public abstract class ProgressMonitor
	{
		internal Action<int, int> updateProgress;

		/// <summary>
		///       Default constructor.
		///       </summary>
		public ProgressMonitor()
		{
			ProgressMonitor progressMonitor = this;
			this.updateProgress = new Action<int, int>(progressMonitor.UpdateProgress);
		}

		/// <summary>
		///       Callback to notify the user as to the progress of the how many bytes have been sent.
		///       <see cref="M:Zebra.Sdk.Device.FileUtil.SendFileContents(System.String,Zebra.Sdk.Device.ProgressMonitor)" /></summary>
		/// <param name="bytesWritten">Bytes currently written</param>
		/// <param name="totalBytes">Total bytes to send</param>
		public abstract void UpdateProgress(int bytesWritten, int totalBytes);
	}
}