using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Service.Contracts;

namespace Service.Contracts
{
	public interface ILocalizationService
	{
		string this[string key] { get; }
		string this[string key, params object[] args] { get; }
	}

	public class LanguageConfig
	{
		public LanguageInfo Default { get; set; }
		public LanguageInfo[] Languages { get; set; }
	}

	public class LanguageInfo
	{
		public string Name { get; set; }
		public string APICode { get; set; }
		public string Culture { get; set; }
	}

	public class LocalizationService : ILocalizationService
	{
		private Dictionary<string, Dictionary<string, string>> indexes;

		public LocalizationService(IAppInfo info, IAppConfig config)
		{
			var langs = config.Bind<LanguageConfig>("Languages");
			indexes = new Dictionary<string, Dictionary<string, string>>();
			if (langs != null)
			{
				foreach (var lang in langs.Languages)
				{
					var file = Path.Combine(info.AssemblyDir, $"Resources.{lang.Culture}.json");
					AddLanguage(file, lang);
				}
			}
		}

		private void AddLanguage(string file, LanguageInfo lang)
		{
			if (File.Exists(file))
			{
				var index = new Dictionary<string, string>(1000);
				var strings = JsonConvert.DeserializeObject<Strings>(File.ReadAllText(file));
				foreach (var elm in strings.Resources)
					index[elm.Key] = elm.Value;
				indexes.Add(lang.Culture, index);
			}
		}

		public string this[string key]
		{
			get
			{
				Dictionary<string, string> index;
				if (indexes.TryGetValue(CultureInfo.CurrentUICulture.Name, out index))
				{
					string value;
					if (index.TryGetValue(key, out value))
						return value;
				}
				return key;
			}
		}


		public string this[string key, params object[] args]
		{
			get
			{
				Dictionary<string, string> index;
				if (indexes.TryGetValue(CultureInfo.CurrentUICulture.Name, out index))
				{
                    var argsKeys = new List<string>();

                    foreach(var arg in args)
                    {
                        if (index.TryGetValue(arg.ToString(), out string translatedArg))
                            argsKeys.Add(translatedArg);
                        else
                            argsKeys.Add(arg.ToString());
                    }

                    string value;
                    if (index.TryGetValue(key, out value))
                        return String.Format(value, argsKeys.ToArray());

                }
				return String.Format(key, args);
			}
		}


		class Strings
		{
			public List<StringPair> Resources = new List<StringPair>();
		}

#pragma warning disable CS0649
		// CS0649 - This is used exclusively when loading from json files therefore code never assigns these values
		class StringPair
		{
			public string Key;
			public string Value;
		}
#pragma warning restore CS0649
	}
}
