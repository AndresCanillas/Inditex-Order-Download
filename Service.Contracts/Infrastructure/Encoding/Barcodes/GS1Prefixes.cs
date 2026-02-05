using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Service.Contracts
{
	public static class GS1Prefixes
	{
		private static readonly List<List<GS1CompanyPrefix>> prefixList;
		private static readonly Dictionary<int, int> partitions = new Dictionary<int, int>()
		{
			{12, 0 },
			{11, 1 },
			{10, 2 },
			{9, 3 },
			{8, 4 },
			{7, 5 },
			{6, 6 },
		};

		static GS1Prefixes()
		{
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var gs1PrefixesFileName = "GS1Prefixes.json";

            var entries = JsonConvert.DeserializeObject<List<GS1CompanyPrefix>>(File.ReadAllText(Path.Combine(path, gs1PrefixesFileName)));

            prefixList = new List<List<GS1CompanyPrefix>>
			{
				entries.Where(p => p.prefix.Length == 1).ToList(),
				entries.Where(p => p.prefix.Length == 2).ToList(),
				entries.Where(p => p.prefix.Length == 3).ToList(),
				entries.Where(p => p.prefix.Length == 4).ToList(),
				entries.Where(p => p.prefix.Length == 5).ToList(),
				entries.Where(p => p.prefix.Length == 6).ToList(),
				entries.Where(p => p.prefix.Length == 7).ToList(),
				entries.Where(p => p.prefix.Length == 8).ToList(),
				entries.Where(p => p.prefix.Length == 9).ToList(),
				entries.Where(p => p.prefix.Length == 10).ToList(),
				entries.Where(p => p.prefix.Length == 11).ToList(),
				entries.Where(p => p.prefix.Length == 12).ToList()
			};
		}

		public static int GetPartition(string ean13, List<GS1CompanyPrefix> prefixOverrides = null)
		{
			var gcp = FindGCP(ean13, prefixOverrides);
			if (!partitions.ContainsKey(gcp.gcpLength))
				throw new InvalidOperationException($"Cannot determine partition value for EAN13 {ean13}");

			return partitions[gcp.gcpLength];
		}

		private static GS1CompanyPrefix FindGCP(string ean13, List<GS1CompanyPrefix> prefixOverrides = null)
		{
			if (prefixOverrides != null && FindPrefixOverride(ean13, prefixOverrides, out var result))
				return result;

			int len = 1;
			do
			{
				var prefix = ean13.Substring(0, len);
				var gcp = prefixList[len - 1].FirstOrDefault(p => p.prefix == prefix);
				if (gcp != null)
					return gcp;
				len++;
			} while (len <= prefixList.Count);
			throw new InvalidOperationException($"Cannot determine GS1CompanyPrefix for EAN13 {ean13}");
		}

		private static bool FindPrefixOverride(string ean13, List<GS1CompanyPrefix> prefixOverrides, out GS1CompanyPrefix result)
		{
			foreach(var prefix in prefixOverrides)
			{
				if(ean13.StartsWith(prefix.prefix))
				{
					result = prefix;
					return true;
				}
			}
			result = null;
			return false;
		}
	}

	public class GS1CompanyPrefix
	{
		public string prefix { get; set; }
		public int gcpLength { get; set; }
	}
}
