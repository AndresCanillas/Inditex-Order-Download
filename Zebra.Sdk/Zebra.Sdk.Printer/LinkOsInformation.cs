using System;
using System.Collections.Generic;
using Zebra.Sdk.Printer.Discovery;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer
{
	/// <summary>
	///       A container class used to hold Link-OS specific information
	///       </summary>
	public class LinkOsInformation
	{
		private int major = -1;

		private int minor = -1;

		/// <summary>
		///       Gets the Link-OS major version number
		///       </summary>
		public int Major
		{
			get
			{
				return this.major;
			}
		}

		/// <summary>
		///       Gets the Link-OS minor version number
		///       </summary>
		public int Minor
		{
			get
			{
				return this.minor;
			}
		}

		/// <summary>
		///       Creates a container class to hold Link-OS information.
		///       </summary>
		/// <param name="major">the Link-OS major version number</param>
		/// <param name="minor">the Link-OS minor version number</param>
		public LinkOsInformation(int major, int minor)
		{
			this.Init(major, minor);
		}

		/// <summary>
		///       Creates a container class to hold Link-OS information.
		///       </summary>
		/// <param name="linkosVersionString">e.g. ("2.1", "3.0")</param>
		public LinkOsInformation(string linkosVersionString)
		{
			if (string.IsNullOrEmpty(linkosVersionString))
			{
				this.Init(1, 0);
				return;
			}
			string[] strArrays = linkosVersionString.Split(new char[] { '.' });
			if ((int)strArrays.Length != 2)
			{
				this.Init(1, 0);
				return;
			}
			try
			{
				this.Init(int.Parse(strArrays[0]), int.Parse(strArrays[1]));
			}
			catch (Exception)
			{
				this.Init(1, 0);
			}
		}

		/// <summary>
		///       Creates a container class to hold Link-OS information based on the supplied <c>discoveredPrinter</c>.
		///       </summary>
		/// <param name="discoveredPrinter">A discovered printer.</param>
		public LinkOsInformation(DiscoveredPrinter discoveredPrinter)
		{
			try
			{
				Dictionary<string, string> discoveryDataMap = discoveredPrinter.DiscoveryDataMap;
				if (!discoveryDataMap.ContainsKey("LINK_OS_MAJOR_VER") || !discoveryDataMap.ContainsKey("LINK_OS_MINOR_VER"))
				{
					this.Init(-1, -1);
				}
				else
				{
					int intValueForKey = StringUtilities.GetIntValueForKey(discoveryDataMap, "LINK_OS_MAJOR_VER");
					this.Init(intValueForKey, StringUtilities.GetIntValueForKey(discoveryDataMap, "LINK_OS_MINOR_VER"));
				}
			}
			catch (Exception)
			{
				throw new ArgumentException("The DiscoveredPrinter argument does not appear to be a Link-OS printer");
			}
		}

		private void Init(int major, int minor)
		{
			this.major = major;
			this.minor = minor;
		}
	}
}