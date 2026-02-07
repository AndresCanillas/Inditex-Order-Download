using System;
using System.Collections.Generic;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Settings.Internal
{
	internal class SettingRangeString : SettingRange
	{
		private int minLength;

		private int maxLength = 2147483647;

		public SettingRangeString(string rangeString)
		{
			try
			{
				List<string> matches = RegexUtil.GetMatches("^(.+)-(.+)$", rangeString);
				if (matches.Count == 3)
				{
					this.minLength = int.Parse(matches[1]);
					this.maxLength = int.Parse(matches[2]);
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
			bool flag = true;
			int length = value.Length;
			if (length < this.minLength || length > this.maxLength)
			{
				flag = false;
			}
			return flag;
		}
	}
}