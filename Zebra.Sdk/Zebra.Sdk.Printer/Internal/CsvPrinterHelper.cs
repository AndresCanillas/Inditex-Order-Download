using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Zebra.Sdk.Printer;

namespace Zebra.Sdk.Printer.Internal
{
	internal class CsvPrinterHelper
	{
		public CsvPrinterHelper()
		{
		}

		public static int[] ExtractFdsByColumnHeading(FieldDescriptionData[] originalFds, string[] columns, int[] quantityColumn)
		{
			Dictionary<string, int> strs = new Dictionary<string, int>();
			int num = 0;
			string[] strArrays = columns;
			for (int i = 0; i < (int)strArrays.Length; i++)
			{
				string str = strArrays[i];
				if (!string.IsNullOrEmpty(str))
				{
					if (!strs.ContainsKey(str))
					{
						strs.Add(str, num);
					}
					else
					{
						strs[str] = num;
					}
					if (str.ToLower().Equals("quantity"))
					{
						quantityColumn[0] = num;
					}
					num++;
				}
			}
			int[] item = new int[(int)originalFds.Length];
			for (int j = 0; j < (int)originalFds.Length; j++)
			{
				if (!strs.ContainsKey(originalFds[j].FieldName))
				{
					throw new UseDefaultMappingException("Column headings do not match data...cannot sort.");
				}
				item[j] = strs[originalFds[j].FieldName];
			}
			return item;
		}

		public static Dictionary<string, string> ParseSingleLineFormat(string[] thisLineOfItems)
		{
			Dictionary<string, string> strs = new Dictionary<string, string>();
			Regex regex = new Regex("(.+)\"(.+)\"");
			string[] strArrays = thisLineOfItems;
			for (int i = 0; i < (int)strArrays.Length; i++)
			{
				IEnumerator enumerator = regex.Matches(strArrays[i]).GetEnumerator();
				if (enumerator.MoveNext() && ((Match)enumerator.Current).Groups.Count >= 2)
				{
					strs.Add(((Match)enumerator.Current).Groups[2].Value, ((Match)enumerator.Current).Groups[1].Value);
				}
			}
			return strs;
		}
	}
}