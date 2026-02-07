using System;
using System.Collections.Generic;

namespace Zebra.Sdk.Printer.Discovery.Internal
{
	internal class ZbiState : EnumAttributes
	{
		public static ZbiState DISABLED;

		public static ZbiState STOPPED;

		public static ZbiState RUNNING;

		private static List<ZbiState> possibleStates;

		static ZbiState()
		{
			ZbiState.DISABLED = new ZbiState(0, "Disabled");
			ZbiState.STOPPED = new ZbiState(1, "Stopped");
			ZbiState.RUNNING = new ZbiState(2, "Running");
			ZbiState.possibleStates = new List<ZbiState>()
			{
				ZbiState.DISABLED,
				ZbiState.STOPPED,
				ZbiState.RUNNING
			};
		}

		private ZbiState(int value, string description) : base(value, description)
		{
		}

		public static ZbiState IntToEnum(int value)
		{
			ZbiState sTOPPED = ZbiState.STOPPED;
			foreach (ZbiState possibleState in ZbiState.possibleStates)
			{
				if (possibleState.Value != value)
				{
					continue;
				}
				sTOPPED = possibleState;
				return sTOPPED;
			}
			return sTOPPED;
		}
	}
}