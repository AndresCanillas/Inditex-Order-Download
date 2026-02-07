using System;

namespace Zebra.Sdk.Printer
{
	/// <summary>
	///       Options for deleting files when loading profiles to a Zebra printer.
	///       </summary>
	public enum FileDeletionOption
	{
		/// <summary>
		///       Attempts to delete all files from the printer. (Persistent files, hidden files, and System files will not be deleted.)
		///       </summary>
		ALL,
		/// <summary>
		///       Will attempt to delete only the file types which are copied at profile/backup creation time.
		///       </summary>
		CLONEABLE,
		/// <summary>
		///       Will not attempt to delete any files.
		///       </summary>
		NONE
	}
}