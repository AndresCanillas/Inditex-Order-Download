using System;
using System.Collections.Generic;

namespace Zebra.Sdk.Comm.Internal
{
	internal class ZebraUrlParser
	{
		public ZebraUrlParser()
		{
		}

		public Dictionary<string, string> GetVariables(string url)
		{
			Dictionary<string, string> strs = new Dictionary<string, string>();
			if (url != null)
			{
				string[] strArrays = url.Trim().Split(new char[] { '?' });
				if ((int)strArrays.Length >= 2)
				{
					string[] strArrays1 = strArrays[1].Split(new char[] { '&' });
					for (int i = 0; i < (int)strArrays1.Length; i++)
					{
						string[] strArrays2 = strArrays1[i].Split(new char[] { '=' });
						if ((int)strArrays2.Length == 2)
						{
							strs.Add(strArrays2[0], strArrays2[1]);
						}
					}
				}
			}
			return strs;
		}
	}
}