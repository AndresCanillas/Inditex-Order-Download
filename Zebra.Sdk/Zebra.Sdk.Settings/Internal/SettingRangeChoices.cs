using System;
using System.Collections.Generic;

namespace Zebra.Sdk.Settings.Internal
{
	internal class SettingRangeChoices : SettingRange
	{
		private List<string> availableChoices;

		public SettingRangeChoices(string range)
		{
			this.availableChoices = new List<string>();
			string[] strArrays = range.Split(new char[] { ',' });
			for (int i = 0; i < (int)strArrays.Length; i++)
			{
				string str = strArrays[i];
				this.availableChoices.Add(str.Trim());
			}
		}

		public bool IsInRange(string value)
		{
			return this.availableChoices.Contains(value);
		}
	}
}