using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Zebra.Sdk.Device;

namespace Zebra.Sdk.Util.Internal
{
	internal class StringUtilities
	{
		public readonly static string CRLF;

		public readonly static string LF;

		static StringUtilities()
		{
			StringUtilities.CRLF = "\r\n";
			StringUtilities.LF = "\n";
		}

		public StringUtilities()
		{
		}

		public static byte[] ByteArrayPadToPlaces(int howManyPlaces, byte[] byteArrayToPad)
		{
			if ((int)byteArrayToPad.Length >= howManyPlaces)
			{
				return byteArrayToPad;
			}
			int num = howManyPlaces - (int)byteArrayToPad.Length;
			byte[] numArray = new byte[howManyPlaces];
			int num1 = 0;
			for (int i = num; i < howManyPlaces; i++)
			{
				int num2 = num1;
				num1 = num2 + 1;
				numArray[i] = byteArrayToPad[num2];
			}
			return numArray;
		}

		public static string ByteArrayToHexString(byte[] byteArray)
		{
			return BitConverter.ToString(byteArray).Replace("-", "").ToLower();
		}

		public static string ConvertDoubleToString(double value)
		{
			return value.ToString();
		}

		public static Dictionary<string, string> ConvertKeyValueJsonToMap(byte[] jsonUtf8Bytes)
		{
			return StringUtilities.ConvertKeyValueJsonToMap(Encoding.UTF8.GetString(jsonUtf8Bytes, 0, (int)jsonUtf8Bytes.Length));
		}

		public static Dictionary<string, string> ConvertKeyValueJsonToMap(string jsonString)
		{
			Dictionary<string, string> strs;
			try
			{
				Dictionary<string, object> obj = JObject.Parse(jsonString).ToObject<Dictionary<string, object>>();
				Dictionary<string, string> strs1 = new Dictionary<string, string>();
				foreach (KeyValuePair<string, object> keyValuePair in obj)
				{
					if (keyValuePair.Value is JObject)
					{
						strs1.Add(keyValuePair.Key, keyValuePair.Value.ToString());
					}
					else if (!(keyValuePair.Value is string))
					{
						if (keyValuePair.Value != null)
						{
							continue;
						}
						strs1.Add(keyValuePair.Key, null);
					}
					else
					{
						strs1.Add(keyValuePair.Key, (string)keyValuePair.Value);
					}
				}
				strs = strs1;
			}
			catch (JsonReaderException jsonReaderException1)
			{
				JsonReaderException jsonReaderException = jsonReaderException1;
				throw new ArgumentException(jsonReaderException.Message, jsonReaderException);
			}
			return strs;
		}

		public static double ConvertStringToDouble(string value)
		{
			double num = 0;
			try
			{
				if (!double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out num))
				{
					num = double.Parse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
				}
			}
			catch (FormatException formatException)
			{
				throw new ZebraIllegalArgumentException(formatException.Message);
			}
			return num;
		}

		public static string ConvertTo16dot3(string fileNameOnPrinter)
		{
			return StringUtilities.ConvertToXdot3(fileNameOnPrinter, 16);
		}

		public static string ConvertTo8dot3(string fileNameOnPrinter)
		{
			return StringUtilities.ConvertToXdot3(fileNameOnPrinter, 8);
		}

		private static string ConvertToXdot3(string fileNameOnPrinter, int maxFileNameLength)
		{
			int num = fileNameOnPrinter.LastIndexOf('.');
			string str = "";
			string str1 = "";
			if (num == -1)
			{
				str1 = fileNameOnPrinter.Substring(0, (fileNameOnPrinter.Length < maxFileNameLength ? fileNameOnPrinter.Length : maxFileNameLength));
			}
			else
			{
				string str2 = fileNameOnPrinter.Substring(0, (num > maxFileNameLength ? maxFileNameLength : num));
				str = fileNameOnPrinter.Substring(num);
				if (str.Length > 4)
				{
					str = str.Substring(0, 4);
				}
				str1 = string.Concat(str2, str);
			}
			return str1;
		}

		public static int CountSubstringOccurences(string stringToSearch, string substring)
		{
			int length = 0;
			int num = 0;
			while (length >= 0)
			{
				length = stringToSearch.IndexOf(substring, length);
				if (length < 0)
				{
					continue;
				}
				num++;
				length += substring.Length;
			}
			return num;
		}

		public static bool DoesPrefixExistInArray(string[] prefixes, string value)
		{
			if (value == null || prefixes == null || value.Length == 0 || prefixes.Length == 0)
			{
				return false;
			}
			bool flag = false;
			int num = 0;
			while (num < (int)prefixes.Length)
			{
				if (prefixes[num] == null || prefixes[num].Length == 0 || !value.ToUpper().StartsWith(prefixes[num].ToUpper()))
				{
					num++;
				}
				else
				{
					flag = true;
					break;
				}
			}
			return flag;
		}

		public static int GetIntValueForKey(Dictionary<string, string> map, string key)
		{
			return int.Parse(StringUtilities.GetStringValueForKey(map, key));
		}

		public static string GetStringValueForKey(Dictionary<string, string> map, string key)
		{
			if (map == null)
			{
				throw new ArgumentNullException("map");
			}
			if (map.ContainsKey(key))
			{
				string item = map[key];
				if (item != null && item.Length > 0)
				{
					return item;
				}
			}
			throw new ArgumentException();
		}

		public static byte[] HexToByteArray(string hexString)
		{
			int num;
			if (hexString.Length % 2 != 0)
			{
				throw new ZebraIllegalArgumentException("Hex string must have an even number of digits");
			}
			byte[] numArray = new byte[hexString.Length / 2];
			int num1 = 0;
			int num2 = 0;
			while (num1 < hexString.Length)
			{
				int num3 = -1;
				if (!(int.TryParse(hexString[num1].ToString(), NumberStyles.HexNumber, null, out num) & int.TryParse(hexString[num1 + 1].ToString(), NumberStyles.HexNumber, null, out num3)))
				{
					throw new ZebraIllegalArgumentException("Input contains data that is not a hex digit");
				}
				numArray[num2] = (byte)((num << 4) + num3);
				num1 += 2;
				num2++;
			}
			return numArray;
		}

		public static int IndexOf(string inputString, string[] searchPatterns, int start)
		{
			int length = inputString.Length;
			for (int i = 0; i < (int)searchPatterns.Length; i++)
			{
				int num = inputString.IndexOf(searchPatterns[i], start);
				if (num >= 0 && num < length)
				{
					length = num;
				}
			}
			if (length == inputString.Length)
			{
				length = -1;
			}
			return length;
		}

		public static string Join(string[] strings, string delimiter)
		{
			string str = "";
			if (strings == null || delimiter == null)
			{
				return "";
			}
			string[] strArrays = strings;
			for (int i = 0; i < (int)strArrays.Length; i++)
			{
				str = string.Concat(str, strArrays[i], delimiter);
			}
			if (!str.Equals(""))
			{
				str = str.Substring(0, str.Length - delimiter.Length);
			}
			return str;
		}

		public static string PadWithChar(string initialString, char padding, int lengthWithPadding, bool padInFront)
		{
			StringBuilder stringBuilder = new StringBuilder(initialString);
			int num = lengthWithPadding - initialString.Length;
			for (int i = 0; i < num; i++)
			{
				if (!padInFront)
				{
					stringBuilder.Append(padding);
				}
				else
				{
					stringBuilder.Insert(0, padding);
				}
			}
			return stringBuilder.ToString();
		}

		public static string Repeat(string s, int num)
		{
			if (s == null)
			{
				return null;
			}
			StringBuilder stringBuilder = new StringBuilder(s.Length * num);
			for (int i = 0; i < num; i++)
			{
				stringBuilder.Append(s);
			}
			return stringBuilder.ToString();
		}

		public static string[] Split(string input, string delimiter)
		{
			List<string> strs = new List<string>();
			int length = 0;
			int num = 0;
			while (num >= 0)
			{
				num = input.IndexOf(delimiter, length);
				if (num < 0)
				{
					if (length > input.Length - 1)
					{
						continue;
					}
					strs.Add(input.Substring(length));
				}
				else
				{
					strs.Add(input.Substring(length, num - length));
					length = num + delimiter.Length;
				}
			}
			return strs.ToArray();
		}

		public static string StringPadToPlaces(int howManyPlaces, string filler, string stringToPad, bool padOnEnd)
		{
			if (stringToPad.Length >= howManyPlaces)
			{
				return stringToPad;
			}
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < howManyPlaces - stringToPad.Length; i++)
			{
				stringBuilder.Append(filler);
			}
			if (!padOnEnd)
			{
				stringBuilder.Append(stringToPad);
			}
			else
			{
				stringBuilder = stringBuilder.Insert(0, stringToPad, stringToPad.Length);
			}
			return stringBuilder.ToString();
		}

		public static string StringPadToPlaces(int howManyPlaces, char filler, string stringToPad, bool padOnEnd)
		{
			if (stringToPad.Length >= howManyPlaces)
			{
				return stringToPad;
			}
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < howManyPlaces - stringToPad.Length; i++)
			{
				stringBuilder.Append(filler);
			}
			if (!padOnEnd)
			{
				stringBuilder.Append(stringToPad);
			}
			else
			{
				stringBuilder = stringBuilder.Insert(0, stringToPad, 1);
			}
			return stringBuilder.ToString();
		}

		public static string StringPadToPlaces(int howManyPlaces, string filler, string stringToPad)
		{
			if (stringToPad.Length >= howManyPlaces)
			{
				return stringToPad;
			}
			string str = "";
			for (int i = 0; i < howManyPlaces - stringToPad.Length; i++)
			{
				str = string.Concat(str, filler);
			}
			str = string.Concat(str, stringToPad);
			return str;
		}

		public static long StringToLong(string stringWithLeadingIntegers)
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < stringWithLeadingIntegers.Length; i++)
			{
				try
				{
					char chr = stringWithLeadingIntegers[i];
					stringBuilder.Append(int.Parse(chr.ToString() ?? ""));
				}
				catch (Exception)
				{
					break;
				}
			}
			return Convert.ToInt64(stringBuilder.ToString());
		}

		public static string StripQuotes(string str)
		{
			if (str == null)
			{
				return null;
			}
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < str.Length; i++)
			{
				char chr = str[i];
				if (chr != '\"')
				{
					stringBuilder.Append(chr);
				}
			}
			return stringBuilder.ToString();
		}

		public static List<string> ToList(string[] array)
		{
			List<string> strs = new List<string>();
			for (int i = 0; array != null && i < (int)array.Length; i++)
			{
				strs.Add(array[i]);
			}
			return strs;
		}

		public static List<string> ToList(string[][] twoDimensionalStringArray)
		{
			List<string> strs = new List<string>();
			string[][] strArrays = twoDimensionalStringArray;
			for (int i = 0; i < (int)strArrays.Length; i++)
			{
				string[] strArrays1 = strArrays[i];
				for (int j = 0; j < (int)strArrays1.Length; j++)
				{
					strs.Add(strArrays1[j]);
				}
			}
			return strs;
		}
	}
}