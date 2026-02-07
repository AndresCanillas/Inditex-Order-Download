using System;

namespace Zebra.Sdk.Settings
{
	/// <summary>
	///       Signals that an error occurred retrieving a setting
	///       </summary>
	public class SettingsException : Exception
	{
		/// <summary>
		///       Constructs a <c>SettingsException</c> with <c>Setting not found</c> as the detailed error message.
		///       </summary>
		public SettingsException() : base("Setting not found")
		{
		}

		/// <summary>
		///       Constructs a <c>SettingsException</c> with <c>message</c> as the detailed error message.
		///       </summary>
		/// <param name="message">The error message.</param>
		public SettingsException(string message) : base(message)
		{
		}

		/// <summary>
		///       Constructs a <c>SettingsException</c> with the <c>message</c> as the detailed error message and 
		///       <c>cause</c> as the source of the exception.
		///       </summary>
		/// <param name="message">The error message.</param>
		/// <param name="cause">The cause of the exception</param>
		public SettingsException(string message, Exception cause) : base(message, cause)
		{
		}
	}
}