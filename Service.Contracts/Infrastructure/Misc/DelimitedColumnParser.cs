using System;
using System.Collections.Generic;
using System.Reflection;

namespace Service.Contracts
{
	public class DelimitedColumnParser
	{
		public char Delimiter { get; set; } = ',';
		public char QuotationChar { get; set; } = '"';

		private int idx;
		private int tokenStartIdx;
		private int tokenEndIdx;
		private string text;

		public T Bind<T>(string line) where T: class, new()
		{
			if(String.IsNullOrWhiteSpace(line))
				return default(T);

			var columns = ParseLine(line);
			var idx = 0;
			var o = new T();

			var members = typeof(T).GetMembers(BindingFlags.Public | BindingFlags.Instance);
			foreach(var m in members)
			{
				if(m.MemberType == MemberTypes.Property)
				{
					var p = (PropertyInfo)m;
					ValidateType(p.PropertyType);
					p.SetValue(o, Convert.ChangeType(columns[idx], p.PropertyType));
					idx++;
				}
				else if(m.MemberType == MemberTypes.Field)
				{
					var f = (FieldInfo)m;
					ValidateType(f.FieldType);
					f.SetValue(o, Convert.ChangeType(columns[idx], f.FieldType));
					idx++;
				}
			}
			return o;
		}

		private void ValidateType(Type propertyType)
		{
			if(!(propertyType == typeof(char) || propertyType == typeof(int) || propertyType == typeof(long) ||
				propertyType == typeof(string) || propertyType == typeof(DateTime) ||
				propertyType == typeof(decimal) || propertyType == typeof(double) || propertyType == typeof(float)))
			throw new NotImplementedException($"Support for type {propertyType.Name} is not implemented.");
		}

		public List<string> ParseLine(string line)
		{
			idx = 0;
			tokenStartIdx = 0;
			tokenEndIdx = 0;
			text = line;
			var result = new List<string>();
			while (idx < text.Length)
				result.Add(NextColumn());
			return result;
		}

		public string NextColumn()
		{
			tokenStartIdx = idx;
			tokenEndIdx = idx;
			while (tokenEndIdx < text.Length)
			{
				if(text[tokenEndIdx] == QuotationChar)
				{
					if (tokenEndIdx + 1 < text.Length)
					{
						tokenStartIdx++;
						tokenEndIdx = FindQuotationEnd(tokenEndIdx + 1);
						return GetToken();
					}
					else return null;
				}
				else if(text[tokenEndIdx] == Delimiter)
				{
					return GetToken();
				}
				else
				{
					tokenEndIdx++;
				}
			}
			idx = text.Length;
			return text.Substring(tokenStartIdx);
		}

		private int FindQuotationEnd(int startIndex)
		{
			int closingQuoteIdx = 0;
			do
			{
				closingQuoteIdx = text.IndexOf(QuotationChar, startIndex);
				if (closingQuoteIdx < 0) return text.Length;

				if (closingQuoteIdx + 1 > text.Length && text[closingQuoteIdx + 1] == QuotationChar)
					startIndex = closingQuoteIdx + 2;
				else
					return closingQuoteIdx;
			} while (startIndex < text.Length);
			return text.Length;
		}

		private void SkipToDelimiter(int startIndex)
		{
			idx = text.IndexOf(Delimiter, startIndex);
			if (idx >= 0)
				idx++;
			else
				idx = text.Length;
		}

		private string GetToken()
		{
			SkipToDelimiter(tokenEndIdx);
			int tokenLength = tokenEndIdx - tokenStartIdx;
			if (tokenLength > 0)
				return text.Substring(tokenStartIdx, tokenLength);
			else
				return null;
		}
	}
}
