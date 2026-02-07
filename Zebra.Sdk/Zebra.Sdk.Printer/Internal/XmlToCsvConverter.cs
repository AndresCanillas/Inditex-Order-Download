using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Zebra.Sdk.Printer.Internal
{
	internal class XmlToCsvConverter
	{
		private string defaultQuantity = string.Empty;

		private string workingVariable = string.Empty;

		private string nodeValue = string.Empty;

		private List<string> columns = new List<string>();

		private List<Dictionary<string, string>> labels = new List<Dictionary<string, string>>();

		private Dictionary<string, string> nameValuePairs;

		public XmlToCsvConverter()
		{
		}

		public static byte[] Convert(byte[] incomingXml)
		{
			byte[] array;
			XmlToCsvConverter xmlToCsvConverter = new XmlToCsvConverter();
			using (Stream memoryStream = new MemoryStream(incomingXml))
			{
				xmlToCsvConverter.ParseDocument(memoryStream, xmlToCsvConverter);
			}
			using (BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream()))
			{
				xmlToCsvConverter.EndDocument(binaryWriter, xmlToCsvConverter);
				array = ((MemoryStream)binaryWriter.BaseStream).ToArray();
			}
			return array;
		}

		public static Stream Convert(Stream sourceDataStream)
		{
			XmlToCsvConverter xmlToCsvConverter = new XmlToCsvConverter();
			xmlToCsvConverter.ParseDocument(sourceDataStream, xmlToCsvConverter);
			BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream());
			xmlToCsvConverter.EndDocument(binaryWriter, xmlToCsvConverter);
			return binaryWriter.BaseStream;
		}

		private void EndDocument(BinaryWriter bw, XmlToCsvConverter converter)
		{
			try
			{
				for (int i = 0; i < converter.columns.Count; i++)
				{
					if (i != 0)
					{
						bw.Write(Encoding.UTF8.GetBytes(","));
					}
					bw.Write(Encoding.UTF8.GetBytes(converter.columns[i]));
				}
				bw.Write(Encoding.UTF8.GetBytes("\n"));
				foreach (Dictionary<string, string> label in converter.labels)
				{
					for (int j = 0; j < converter.columns.Count; j++)
					{
						if (j != 0)
						{
							bw.Write(Encoding.UTF8.GetBytes(","));
						}
						string item = converter.columns[j];
						string str = (!label.ContainsKey(item) ? "" : label[item]);
						if (str.Contains(","))
						{
							str = string.Concat("\"", str, "\"");
						}
						bw.Write(Encoding.UTF8.GetBytes(str));
					}
					bw.Write(Encoding.UTF8.GetBytes("\n"));
				}
			}
			catch (Exception)
			{
			}
		}

		private void ParseDocument(Stream incomingXml, XmlToCsvConverter converter)
		{
			using (StreamReader streamReader = new StreamReader(incomingXml, true))
			{
				using (XmlReader xmlReader = XmlReader.Create(streamReader))
				{
					while (xmlReader.Read())
					{
						XmlNodeType nodeType = xmlReader.NodeType;
						if (nodeType != XmlNodeType.Element)
						{
							if (nodeType == XmlNodeType.Text)
							{
								this.nodeValue = xmlReader.Value;
							}
							else if (nodeType == XmlNodeType.EndElement)
							{
								if (converter.nameValuePairs == null)
								{
									continue;
								}
								if (Regex.IsMatch(xmlReader.Name, "\\bvariable\\b", RegexOptions.IgnoreCase))
								{
									if (!converter.nameValuePairs.ContainsKey(converter.workingVariable))
									{
										converter.nameValuePairs.Add(converter.workingVariable, this.nodeValue.Trim());
									}
									else
									{
										converter.nameValuePairs[converter.workingVariable] = this.nodeValue.Trim();
									}
								}
								else if (Regex.IsMatch(xmlReader.Name, "\\blabel\\b", RegexOptions.IgnoreCase))
								{
									converter.labels.Add(converter.nameValuePairs);
								}
								this.nodeValue = string.Empty;
							}
						}
						else if (Regex.IsMatch(xmlReader.Name, "\\blabel\\b", RegexOptions.IgnoreCase))
						{
							converter.nameValuePairs = new Dictionary<string, string>();
							if (string.IsNullOrEmpty(converter.defaultQuantity))
							{
								continue;
							}
							converter.nameValuePairs.Add("QUANTITY", converter.defaultQuantity);
						}
						else if (!Regex.IsMatch(xmlReader.Name, "\\bvariable\\b", RegexOptions.IgnoreCase))
						{
							if (!Regex.IsMatch(xmlReader.Name, "\\bfile\\b", RegexOptions.IgnoreCase) || !xmlReader.HasAttributes)
							{
								continue;
							}
							string attribute = xmlReader.GetAttribute("_QUANTITY");
							if (string.IsNullOrEmpty(attribute))
							{
								continue;
							}
							converter.defaultQuantity = attribute;
							converter.columns.Add("QUANTITY");
						}
						else
						{
							if (!xmlReader.HasAttributes)
							{
								continue;
							}
							converter.workingVariable = xmlReader.GetAttribute("name");
							if (converter.columns.Contains(converter.workingVariable))
							{
								continue;
							}
							converter.columns.Add(converter.workingVariable);
						}
					}
				}
			}
		}
	}
}