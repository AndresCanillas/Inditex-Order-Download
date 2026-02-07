using System;
using System.IO;

namespace Zebra.Sdk.Printer
{
	/// <summary>
	///       Defines functions used for creating and applying profiles to a Zebra printer.
	///       </summary>
	public interface ProfileUtil
	{
		/// <summary>
		///       Save a backup of your printer's settings, alerts, and files for later restoration.
		///       </summary>
		/// <param name="pathToOutputFile">Path on your local machine where you want to save the backup. 
		///       (e.g. /home/user/profile.zprofile). The extension must be.zprofile; if it is not, the method will 
		///       change it to .zprofile for you.</param>
		/// <exception cref="T:System.IO.IOException">If the output file could not be created.</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">Could not interpret the response from the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		void CreateBackup(string pathToOutputFile);

		/// <summary>
		///       Create a profile of your printer's settings, alerts, and files for cloning to other printers.
		///       </summary>
		/// <param name="pathToOutputFile">Path on your local machine where you want to save the profile. 
		///       (e.g. /home/user/profile.zprofile). The extension must be.zprofile; if it is not, the method will 
		///       change it to .zprofile for you.</param>
		/// <exception cref="T:System.IO.IOException">If the output file could not be created.</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">Could not interpret the response from the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		void CreateProfile(string pathToOutputFile);

		/// <summary>
		///       Create a profile of your printer's settings, alerts, and files for cloning to other printers.
		///       </summary>
		/// <param name="profileDestinationStream">The destination stream where you want to write the profile.</param>
		/// <exception cref="T:System.IO.IOException">If the output file could not be created.</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">Could not interpret the response from the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		void CreateProfile(Stream profileDestinationStream);

		/// <summary>
		///       Takes settings, alerts, and files from a backup, and applies them to a printer.
		///       </summary>
		/// <param name="pathToBackup">Path to the profile to load. (e.g. /home/user/profile.zprofile)</param>
		/// <exception cref="T:System.IO.IOException">If the profile does not exist or could not be read.</exception>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		void LoadBackup(string pathToBackup);

		/// <summary>
		///       Takes settings, alerts, and files from a backup, and applies them to a printer.
		///       </summary>
		/// <param name="pathToBackup">Path to the profile to load. (e.g. /home/user/profile.zprofile)</param>
		/// <param name="isVerbose">Increases the amount of detail presented to the user when loading firmware from the profile</param>
		/// <exception cref="T:System.IO.IOException">If the profile does not exist or could not be read.</exception>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		void LoadBackup(string pathToBackup, bool isVerbose);

		/// <summary>
		///       Takes settings, alerts, and files from a profile, and applies them to a printer.
		///       </summary>
		/// <param name="pathToProfile">Path to the profile to load. (e.g. /home/user/profile.zprofile)</param>
		/// <exception cref="T:System.IO.IOException">If the profile does not exist or could not be read.</exception>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		void LoadProfile(string pathToProfile);

		/// <summary>
		///       Takes settings, alerts, and files from a profile, and applies them to a printer.
		///       </summary>
		/// <param name="pathToProfile">Path to the profile to load. (e.g. /home/user/profile.zprofile)</param>
		/// <param name="filesToDelete">An enum describing which files to delete.</param>
		/// <param name="isVerbose">Increases the amount of detail presented to the user when loading firmware from the profile</param>
		/// <exception cref="T:System.IO.IOException">If the profile does not exist or could not be read.</exception>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		void LoadProfile(string pathToProfile, FileDeletionOption filesToDelete, bool isVerbose);
	}
}