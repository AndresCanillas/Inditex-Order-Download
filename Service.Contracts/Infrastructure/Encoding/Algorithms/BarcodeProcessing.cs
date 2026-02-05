using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Contracts
{
	public static class BarcodeProcessing
	{
		public static string ProcessTrackingCodeMask(string mask, JObject data, TagEncodingInfo tagInfo)
		{
            if (String.IsNullOrWhiteSpace(mask))
				return "";
			StringBuilder sb = new StringBuilder(500);
			int pos = 0;
			while(pos < mask.Length)
			{
				var c = mask[pos];
				switch (c)
				{
					case '[':
						pos = ProcessDataField(mask, pos, data, sb);
						break;
					case '%':
						pos = ProcessRFIDField(mask, pos, tagInfo, sb);
						break;
					default:
						sb.Append(c);
						break;
				}
				pos++;
			}
			return sb.ToString();
		}

		private static int ProcessDataField(string mask, int pos, JObject data, StringBuilder sb)
		{
			var fieldInfo = GetFieldInfo(mask, ']', pos);
			var value = data[fieldInfo.FieldName];
			if (String.IsNullOrWhiteSpace(fieldInfo.FormatSpecifier))
			{
				sb.Append(value);
			}
			else
			{
				var formatedValue = String.Format("{0:" + fieldInfo.FormatSpecifier + "}", value);
				sb.Append(formatedValue);
			}
			return fieldInfo.EndIndex;
		}

		private static int ProcessRFIDField(string mask, int pos, TagEncodingInfo tagInfo, StringBuilder sb)
		{
			string value;
			var fieldInfo = GetFieldInfo(mask, '%', pos);
			switch (fieldInfo.FieldName.ToLower())
			{
                case "barcode":
                    value = tagInfo.Barcode;
                    break;
				case "epc":
					value = tagInfo.EPC;
					break;
				case "accpwd":
					value = tagInfo.AccessPassword;
					break;
				case "killpwd":
					value = tagInfo.AccessPassword;
					break;
				case "usrmem":
					value = tagInfo.UserMemory;
					break;
				case "serial":
					value = tagInfo.SerialNumber.ToString();
					break;
				default:
					throw new InvalidOperationException($"RFID Field {fieldInfo.FieldName} is not recognized. Valid RFID fields include: epc, accpwd, killpwd, usrmem & serial");
			}
			if (String.IsNullOrWhiteSpace(fieldInfo.FormatSpecifier))
			{
				sb.Append(value);
			}
			else
			{
				var formatedValue = String.Format("{0:" + fieldInfo.FormatSpecifier + "}", value);
				sb.Append(formatedValue);
			}
			return fieldInfo.EndIndex;
		}

		private static QRMaskFieldInfo GetFieldInfo(string mask, char endDelimiter, int pos)
		{
			pos++;
			int endIdx = mask.IndexOf(endDelimiter, pos);
			if (endIdx < 0)
				throw new Exception($"Found unterminated field in QR mask at pos {pos}: {mask}");
            string fieldName;
            string formatSpecifier = null;
            var fieldSpec = mask.Substring(pos, endIdx - pos);
			var formatIdx = fieldSpec.IndexOf(':');
			if (formatIdx > 0)
			{
				fieldName = fieldSpec.Substring(0, formatIdx).Trim();
				formatSpecifier = fieldSpec.Substring(formatIdx + 1);
			}
            else
            {
                fieldName = fieldSpec;
            }
			return new QRMaskFieldInfo()
			{
				FieldName = fieldName,
				FormatSpecifier = formatSpecifier,
				StartIndex = pos,
				EndIndex = endIdx
			};
		}

		class QRMaskFieldInfo
		{
			public string FieldName;
			public string FormatSpecifier;
			public int StartIndex;
			public int EndIndex;
		}
	}
}
