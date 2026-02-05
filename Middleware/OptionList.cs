using Newtonsoft.Json;
using Service.Contracts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Print.Middleware
{
	public class OptionList
	{
		private List<OptionListElement> list = new List<OptionListElement>();

		public OptionList() { }

		public static OptionList FromEnumerable(IEnumerable enumerable, string valueMember, string nameMember, object selectedValue = null, string allOption = null)
		{
			bool selected;
			string value, name;
			OptionList result = new OptionList();
			if (enumerable == null) return result;
			if (!string.IsNullOrWhiteSpace(allOption))
			{
				result.list.Add(new OptionListElement()
				{
					Value = "0",
					Name = allOption,
					Selected = true
				});
			}
			foreach (var obj in enumerable)
			{
				value = Reflex.GetMember(obj, valueMember).ToString();
				name = Reflex.GetMember(obj, nameMember).ToString();
				if (selectedValue == null)
				{
					if (allOption == null)
						selectedValue = value;
					else
						selectedValue = "";
				}
				selected = (value == selectedValue.ToString());
				result.list.Add(new OptionListElement()
				{
					Value = value,
					Name = name,
					Selected = selected
				});
			}
			return result;
		}

		public static OptionList FromEnum<T>(bool includeAnyOption = false) where T : Enum
		{
			OptionList result = new OptionList();
			string value, name;
			var values = Enum.GetValues(typeof(T));

			if (includeAnyOption)
				result.list.Add(new OptionListElement() { Value = null, Name = "Any" });

			foreach (var elm in values)
			{
				value = ((int)elm).ToString();
				name = Enum.GetName(typeof(T), elm);
				result.list.Add(new OptionListElement() { Value = value, Name = name });
			}

			if (result.list.Count > 0)
				result.list[0].Selected = true;

			return result;
		}

		public static OptionList FromEnum<T>(int selectedValue, bool includeAnyOption = false) where T : Enum
		{
			OptionList result = new OptionList();
			int enumValue;
			string value, name;
			bool selected;

			if (includeAnyOption)
				result.list.Add(new OptionListElement() { Value = null, Name = "Any" });

			var values = Enum.GetValues(typeof(T));
			foreach (var elm in values)
			{
				enumValue = (int)elm;
				value = enumValue.ToString();
				name = Enum.GetName(typeof(T), elm);
				selected = (enumValue == selectedValue);
				result.list.Add(new OptionListElement()
				{
					Value = value,
					Name = name,
					Selected = selected
				});
			}

			if(result.list.Count > 0 && result.list.FirstOrDefault(p=>p.Selected) == null)
				result.list[0].Selected = true;

			return result;
		}


		public override string ToString()
		{
			return JsonConvert.SerializeObject(list);
		}
	}

	public class OptionListElement
	{
		public string Value;
		public string Name;
		public bool Selected;
	}

	public class Map
	{
		public static string FromEnumerable(IEnumerable enumerable, string valueMember, string nameMember, bool includeNullEntry = false)
		{
			string value, name;
			StringBuilder sb = new StringBuilder(500);
			int count = 0;
			sb.Append("{");
			if (includeNullEntry)
			{
				count++;
				sb.Append($"\"\": \"None\",");
			}
			foreach (var obj in enumerable)
			{
				count++;
				value = Reflex.GetMember(obj, valueMember).ToString();
				name = Reflex.GetMember(obj, nameMember).ToString();
				sb.Append($"\"{value}\": \"{name}\",");
			}
			if (sb.Length > 0 && count > 0)
				sb.Remove(sb.Length - 1, 1);
			sb.Append("}");
			return sb.ToString();
		}

		public static string FromEnum<T>() where T : Enum
		{
			string value, name;
			StringBuilder sb = new StringBuilder(500);
			var values = Enum.GetValues(typeof(T));
			sb.Append("{");
			foreach (var elm in values)
			{
				value = ((int)elm).ToString();
				name = Enum.GetName(typeof(T), elm);
				sb.Append($"\"{value}\": \"{name}\",");
			}
			if (sb.Length > 0)
				sb.Remove(sb.Length - 1, 1);
			sb.Append("}");
			return sb.ToString();
		}
	}
}
