using System;
using System.Collections.Generic;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Settings.Internal
{
	internal class SettingRangeInteger : SettingRange
	{
		private long min = -9223372036854775808L;

		private long max = 9223372036854775807L;

		public SettingRangeInteger(string rangeString)
		{
			try
			{
				List<string> matches = RegexUtil.GetMatches("^(.+)-(.+)$", rangeString);
				if (matches.Count == 3)
				{
					this.min = long.Parse(matches[1]);
					this.max = long.Parse(matches[2]);
				}
			}
			catch (ArgumentException argumentException1)
			{
				ArgumentException argumentException = argumentException1;
				throw new FormatException(argumentException.Message, argumentException);
			}
			catch (OverflowException overflowException1)
			{
				OverflowException overflowException = overflowException1;
				throw new FormatException(overflowException.Message, overflowException);
			}
		}

		public bool IsInRange(string value)
		{
			bool flag;
			long num = (long)0;
			try
			{
				num = long.Parse(value);
				bool flag1 = true;
				if (num < this.min || num > this.max)
				{
					flag1 = false;
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