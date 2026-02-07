using System;

namespace Zebra.Sdk.Weblink
{
	/// <summary>
	///       ENumeration to determine how the Weblink address is set.
	///       </summary>
	public enum WeblinkAddressStrategy
	{
		/// <summary>
		///       Looks at current weblink settings and determine which location to set based on if the URL is set and valid and if 
		///       the printer is connected to the address.
		///       </summary>
		AUTO_SELECT,
		/// <summary>
		///       Overrides any setting in Weblink location 1.
		///       </summary>
		FORCE_CONNECTION_1,
		/// <summary>
		///       Overrides any setting in Weblink location 2.
		///       </summary>
		FORCE_CONNECTION_2
	}
}