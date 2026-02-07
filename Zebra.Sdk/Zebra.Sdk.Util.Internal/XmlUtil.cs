using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Zebra.Sdk.Device;

namespace Zebra.Sdk.Util.Internal
{
	internal class XmlUtil
	{
		public XmlUtil()
		{
		}

		public static List<XmlNode> FindImmediateChildByName(XmlNode parent, string key)
		{
			List<XmlNode> xmlNodes = new List<XmlNode>();
			if (parent == null || key == null)
			{
				return null;
			}
			XmlNodeList childNodes = parent.ChildNodes;
			for (int i = 0; i < childNodes.Count; i++)
			{
				XmlNode xmlNodes1 = childNodes.Item(i);
				if (xmlNodes1.Name.Equals(key))
				{
					xmlNodes.Add(xmlNodes1);
				}
			}
			return xmlNodes;
		}

		public static List<XmlNode> GetAllNodesByPath(XmlDocument doc, string path)
		{
			int j;
			if (path == null || path.Length == 0)
			{
				return null;
			}
			string[] strArrays = path.Split(new char[] { '/' });
			XmlNodeList elementsByTagName = doc.GetElementsByTagName(strArrays[(int)strArrays.Length - 1]);
			List<XmlNode> xmlNodes = new List<XmlNode>();
			for (int i = 0; i < elementsByTagName.Count; i++)
			{
				XmlNode parentNode = elementsByTagName.Item(i);
				for (j = (int)strArrays.Length - 1; j >= 0 && parentNode.Name.Equals(strArrays[j]); j--)
				{
					parentNode = parentNode.ParentNode;
				}
				if (j == -1)
				{
					xmlNodes.Add(elementsByTagName.Item(i));
				}
			}
			return xmlNodes;
		}

		public static void GetAllSubNodesByName(List<XmlNode> nodeList, XmlNode n, string key)
		{
			XmlNodeList childNodes = n.ChildNodes;
			for (int i = 0; i < childNodes.Count; i++)
			{
				XmlNode xmlNodes = childNodes.Item(i);
				if (xmlNodes.Name.Equals(key))
				{
					nodeList.Add(xmlNodes);
				}
				XmlUtil.GetAllSubNodesByName(nodeList, xmlNodes, key);
			}
		}

		public static XmlNode GetDataAtNamedNode(Stream resultStream, string nodeName, string optionalErrorId)
		{
			XmlDocument xmlDocument = new XmlDocument();
			using (Stream stream = resultStream)
			{
				if (stream.Position > (long)0)
				{
					stream.Position = (long)0;
				}
				using (XmlReader xmlReader = XmlReader.Create(stream))
				{
					xmlDocument.Load(xmlReader);
				}
			}
			XmlNodeList childNodes = xmlDocument.ChildNodes;
			XmlNode firstSubNodeByName = XmlUtil.GetFirstSubNodeByName(xmlDocument, nodeName);
			if (firstSubNodeByName == null)
			{
				string str = "Malformed response from printer";
				if (optionalErrorId != null && optionalErrorId.Length > 0)
				{
					str = string.Concat(str, " for ", optionalErrorId);
				}
				str = string.Concat(str, ".");
				throw new ZebraIllegalArgumentException(str);
			}
			return firstSubNodeByName;
		}

		public static XmlNode GetDataAtNamedNode(Stream resultStream, string nodeName)
		{
			return XmlUtil.GetDataAtNamedNode(resultStream, nodeName, null);
		}

		public static XmlNode GetFirstNodeByPath(XmlDocument doc, string path)
		{
			List<XmlNode> allNodesByPath = XmlUtil.GetAllNodesByPath(doc, path);
			if (allNodesByPath == null || allNodesByPath.Count <= 0)
			{
				return null;
			}
			return allNodesByPath[0];
		}

		public static XmlNode GetFirstSubNodeByName(XmlNode node, string name)
		{
			XmlNodeList childNodes = node.ChildNodes;
			for (int i = 0; i < childNodes.Count; i++)
			{
				XmlNode xmlNodes = childNodes.Item(i);
				if (xmlNodes.Name.Equals(name))
				{
					return xmlNodes;
				}
				XmlNode firstSubNodeByName = XmlUtil.GetFirstSubNodeByName(xmlNodes, name);
				if (firstSubNodeByName != null)
				{
					return firstSubNodeByName;
				}
			}
			return null;
		}

		public static string GetTextContent(XmlNode n, string defaultValue)
		{
			if (n != null)
			{
				XmlNode firstChild = n.FirstChild;
				if (firstChild != null)
				{
					string value = firstChild.Value;
					XmlNode nextSibling = firstChild.NextSibling;
					XmlNode xmlNodes = nextSibling;
					if (nextSibling != null)
					{
						value = xmlNodes.Value;
					}
					if (value != null)
					{
						return value;
					}
				}
			}
			return defaultValue;
		}

		internal bool IsValidXml(string data)
		{
			bool flag = false;
			try
			{
				using (MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
				{
					using (StreamReader streamReader = new StreamReader(memoryStream, true))
					{
						XDocument.Load(streamReader);
						flag = true;
					}
				}
			}
			catch
			{
			}
			return flag;
		}

		public static string XmlToString(XmlNode root)
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (root != null)
			{
				if (root.NodeType != XmlNodeType.Text)
				{
					if (root.NodeType == XmlNodeType.Document)
					{
						stringBuilder.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n");
					}
					else
					{
						stringBuilder.Append("<").Append(root.Name).Append(">");
						if (root.HasChildNodes && root.FirstChild.NodeType != XmlNodeType.Text)
						{
							stringBuilder.Append("\r\n");
						}
					}
					XmlNodeList childNodes = root.ChildNodes;
					for (int i = 0; i < childNodes.Count; i++)
					{
						XmlNode xmlNodes = childNodes.Item(i);
						stringBuilder.Append(XmlUtil.XmlToString(xmlNodes));
					}
					if (root.NodeType != XmlNodeType.Document)
					{
						stringBuilder.Append("</").Append(root.Name).Append(">\r\n");
					}
				}
				else
				{
					stringBuilder.Append(root.Value);
				}
			}
			return stringBuilder.ToString();
		}
	}
}