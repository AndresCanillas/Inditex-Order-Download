using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts
{
	public static class StringExtensions
	{
		public static T ToEnum<T>(this string value)
		{
			return (T)Enum.Parse(typeof(T), value);
		}

		public static string Merge<T>(this IEnumerable<T> list, string separator)
		{
			if (list == null)
				throw new ArgumentNullException(nameof(list));
			if (separator == null)
				throw new ArgumentNullException(nameof(separator));
			StringBuilder sb = new StringBuilder(100);
			foreach (var e in list)
			{
				sb.Append(e.ToString()).Append(separator);
			}
			if (sb.Length > separator.Length)
				sb.Remove(sb.Length - separator.Length, separator.Length);
			return sb.ToString();
		}

		public static string Merge<T>(this IEnumerable<T> list, string separator, Func<T, string> func)
		{
			if (list == null)
				throw new ArgumentNullException(nameof(list));
			if (separator == null)
				throw new ArgumentNullException(nameof(separator));
			if (func == null)
				throw new ArgumentNullException(nameof(func));
			StringBuilder sb = new StringBuilder(100);
			foreach (var e in list)
			{
				sb.Append(func(e)).Append(separator);
			}
			if (sb.Length > separator.Length)
				sb.Remove(sb.Length - separator.Length, separator.Length);
			return sb.ToString();
		}

		public static List<T> Split<T>(this string str, char separator, Func<string, T> func)
		{
			if (func == null)
				throw new ArgumentNullException(nameof(func));
			var list = new List<T>();
			if (str == null) return list;
			string[] tokens = str.Split(new char[] { separator }, StringSplitOptions.RemoveEmptyEntries);
			foreach (var t in tokens)
				list.Add(func(t));
			return list;
		}

		public static bool IsValidIdentifier(this string str)
		{
			if (String.IsNullOrWhiteSpace(str)) return false;
			if (str.Length > 64) return false;
			if ("0123456789".IndexOf(str[0]) >= 0) return false;
			return str.IsValidCharacters("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_");
		}

		private static bool IsValidCharacters(this string str, string validChars)
		{
			foreach(char c in str)
			{
				if (validChars.IndexOf(c) < 0) return false;
			}
			return true;
		}


		/// <summary>
		/// Ensures the string can be interpreted as a Decimal integer number.
		/// </summary>
		public static bool IsNumeric(this string str)
		{
			string ValidChars = "0123456789";
			if (str.Length > 0 && str.Length <= 18)
			{
				for (int i = 0; i < str.Length; i++)
				{
					if (ValidChars.IndexOf(str[i]) < 0) return false;
				}
				return true;
			}
			return false;
		}
	}
}
