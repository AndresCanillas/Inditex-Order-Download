using System;

namespace Zebra.Sdk.Weblink
{
	/// <summary>
	///       Callback for updating the status of a Weblink configuration task.
	///       </summary>
	public abstract class WeblinkConfigurationStateUpdater
	{
		protected WeblinkConfigurationStateUpdater()
		{
		}

		/// <summary>
		///       Provides a custom message for the current weblink configuration state.
		///       </summary>
		/// <param name="message">Custom message.</param>
		public virtual void ProgressUpdate(string message)
		{
		}

		/// <summary>
		///       Sets the new state of the Weblink Configurator defined by <c>WeblinkConfigurationState</c>.
		///       </summary>
		/// <param name="newState">The updated WeblinkConfiguration state.</param>
		public abstract void UpdateState(WeblinkConfigurationState newState);
	}
}