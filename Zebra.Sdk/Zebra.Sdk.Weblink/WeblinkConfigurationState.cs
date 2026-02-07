using System;

namespace Zebra.Sdk.Weblink
{
	/// <summary>
	///       Enumeration of the weblink configuration task's state.
	///       </summary>
	public enum WeblinkConfigurationState
	{
		/// <summary>
		///       Configuration state indicating the task is creating a connection to the printer.
		///       </summary>
		ConnectToPrinter,
		/// <summary>
		///       Configuration state indicating the task is retrieving the printer's settings.
		///       </summary>
		GetSettings,
		/// <summary>
		///       Configuration state indicating the task is configuring the weblink setting.
		///       </summary>
		ConfigureWeblink,
		/// <summary>
		///       Configuration state indicating the task is restarting the printer.
		///       </summary>
		RestartPrinter,
		/// <summary>
		///       Configuration state indicating the task is waiting for the printer to restart and then reconnect.
		///       </summary>
		ReconnectToPrinter,
		/// <summary>
		///       Configuration state indicating the task is validating the printer's profile manager connection.
		///       </summary>
		VerifyWeblinkConnection
	}
}