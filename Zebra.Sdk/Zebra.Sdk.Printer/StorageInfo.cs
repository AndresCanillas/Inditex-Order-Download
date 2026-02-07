using System;

namespace Zebra.Sdk.Printer
{
	/// <summary>
	///       A container class which holds information about various printer drives.
	///       </summary>
	public class StorageInfo
	{
		/// <summary>
		///       The drive's alphabetical identifier.
		///       </summary>
		public char driveLetter;

		/// <summary>
		///       The type of drive. (e.g. flash, RAM)
		///       </summary>
		public DriveType driveType;

		/// <summary>
		///       The number of bytes remaining on the drive.
		///       </summary>
		public long bytesFree;

		/// <summary>
		///       Bool defining whether or not files persist across printer reboots.
		///       </summary>
		public bool isPersistent;

		/// <summary>
		///       Creates an empty <c>StorageInfo</c> container
		///       </summary>
		public StorageInfo()
		{
		}
	}
}