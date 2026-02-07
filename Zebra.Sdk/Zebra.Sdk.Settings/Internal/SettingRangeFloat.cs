using System;
using System.Collections.Generic;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Settings.Internal
{
	internal class SettingRangeFloat : SettingRange
	{
		private string min = double.MinValue.ToString();

		private string max = double.MaxValue.ToString();

		public SettingRangeFloat(string rangeString)
		{
			List<string> matches = RegexUtil.GetMatches("^(.+)-(.+)$", rangeString);
			if (matches.Count == 3)
			{
				this.min = matches[1];
				this.max = matches[2];
			}
		}

		public bool IsInRange(string value)
		{
			bool flag;
			decimal num = new decimal(0);
			try
			{
				num = new decimal(Convert.ToDouble(value));
				bool flag1 = true;
				try
				{
					decimal num1 = new decimal(Convert.ToDouble(this.min));
					decimal num2 = new decimal(Convert.ToDouble(this.max));
					if (num.CompareTo(num1) < 0 || num.CompareTo(num2) > 0)
					{
						flag1 = false;
					}
				}
				catch (Exception)
				{
				}
				return flag1;
			}
			catch (Exception)
			{
				flag = false;
			}
			return flag;
		}
	}
}