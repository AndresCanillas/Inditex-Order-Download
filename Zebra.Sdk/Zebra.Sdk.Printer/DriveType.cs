using System;

namespace Zebra.Sdk.Printer
{
	/// <summary>
	///       Drive types.
	///       </summary>
	public enum DriveType
	{
		/// <summary>
		///       Onboard flash drive.
		///       </summary>
		FLASH,
		/// <summary>
		///       RAM Drive.
		///       </summary>
		RAM,
		/// <summary>
		///       Removable mass storage drive.
		///       </summary>
		MASS_STORAGE,
		/// <summary>
		///       Unknown drive.
		///       </summary>
		UNKNOWN,
		/// <summary>
		///       Read only drive.
		///       </summary>
		READ_ONLY
	}
}