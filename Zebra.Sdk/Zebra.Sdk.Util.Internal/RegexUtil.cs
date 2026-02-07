using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Zebra.Sdk.Util.Internal
{
	internal class RegexUtil
	{
		public RegexUtil()
		{
		}

		public static List<string> GetMatches(string regex, string s)
		{
			List<string> strs = new List<string>();
			Match match = (new Regex(regex)).Match(s);
			if (match.Success)
			{
				for (int i = 0; i < match.Groups.Count; i++)
				{
					strs.Add(match.Groups[i].Value);
				}
			}
			return strs;
		}
	}
}