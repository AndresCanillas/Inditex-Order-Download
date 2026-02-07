using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace WebLink.Contracts.Sage
{
	public interface IXmlTransfer
	{
		List<Fld> Fields { get; set; }
		List<Grp> Groups { get; set; }
		List<Tab> Tables { get; set; }
		List<Lst> Lists { get; set; }
		string GetValueInGroup(string fieldName, string groupId);
		void SetValueInGroup(string val, string FieldName, string GroupName);
		string GetValueInTab(string FieldName, string TabName);
		void SetValueInTab(string val, string FieldName, string TabName);
		string GetFieldValue(string FieldName);
		void AddField(Fld field);
		void AddField(string name, string text, string type = "Char");
		void AddField(Fld field, string groupId);
		void SetValueInGroup(Fld field, string groupId);
		Fld GetField(string fieldName);
		Fld GetFieldFromGroup(string fieldName, string groupId);
	}

	[XmlRoot(ElementName = "FLD")]
	public class Fld
	{
		[XmlAttribute(AttributeName = "NAME")]
		public string Name { get; set; }
		[XmlAttribute(AttributeName = "TYPE")]
		public string Type { get; set; }
		[XmlText]
		public string Text { get; set; }
		[XmlAttribute(AttributeName = "MENULAB")]
		public string MenuLab { get; set; }
		[XmlAttribute(AttributeName = "MENULOCAL")]
		public string MenuLocal { get; set; }
		[XmlAttribute(AttributeName = "BYTES")]
		public string Bytes { get; set; }
		[XmlAttribute(AttributeName = "MIMETYPE")]
		public string MimeType { get; set; }

		public Fld()
		{
			Type = "Char";
		}
	}


	[XmlRoot(ElementName = "LST")]
	public class Lst
	{
		[XmlElement(ElementName = "ITM")]
		public List<string> Itm { get; set; }
		[XmlAttribute(AttributeName = "NAME")]
		public string Name { get; set; }
		[XmlAttribute(AttributeName = "SIZE")]
		public string Size { get; set; }
		[XmlAttribute(AttributeName = "TYPE")]
		public string Type { get; set; }

		public Lst()
		{
			Itm = new List<string>();
		}
	}


	[XmlRoot(ElementName = "GRP")]
	public class Grp
	{
		[XmlElement(ElementName = "FLD")]
		public List<Fld> Fields { get; set; }
		[XmlAttribute(AttributeName = "ID")]
		public string Id { get; set; }
		[XmlElement(ElementName = "LST")]
		public List<Lst> List { get; set; }

		public Grp()
		{
			Fields = new List<Fld>();
			List = new List<Lst>();
		}
	}


	[XmlRoot(ElementName = "LIN")]
	public class Lin
	{
		[XmlElement(ElementName = "FLD")]
		public List<Fld> Fields { get; set; }
		[XmlAttribute(AttributeName = "NUM")]
		public string Num { get; set; }

		public Lin()
		{
			Fields = new List<Fld>();
		}
	}

	[XmlRoot(ElementName = "TAB")]
	public class Tab
	{
		[XmlElement(ElementName = "LIN")]
		public List<Lin> Lines { get; set; }
		[XmlAttribute(AttributeName = "DIM")]
		public string Dimension { get; set; }
		[XmlAttribute(AttributeName = "ID")]
		public string Id { get; set; }
		[XmlAttribute(AttributeName = "SIZE")]
		public string Size { get; set; }

		public Tab()
		{
			Lines = new List<Lin>();
		}

	}

	public abstract class AXmlTransfer : IXmlTransfer
	{
		[XmlElement(ElementName = "FLD")]
		public List<Fld> Fields { get; set; }
		[XmlElement(ElementName = "GRP")]
		public List<Grp> Groups { get; set; }
		[XmlElement(ElementName = "TAB")]
		public List<Tab> Tables { get; set; }
		[XmlElement(ElementName = "LST")]
		public List<Lst> Lists { get; set; }

		public string GetFieldValue(string FieldName)
		{
			var fld = Fields.Find(f => f.Name.Equals(FieldName));

			if (fld == null)
			{
				return string.Empty;
			}

			return fld.Text;
		}

		public void AddField(Fld field)
		{

			var fld = Fields.Find(f => f.Name.Equals(field.Name));

			if (fld == null)
			{
				Fields.Add(field);
			}
			else
			{
				fld.Text = field.Text;
			}

		}

		public void AddField(string name, string text, string type = "Char")
		{
			var fld = new Fld() { Name = name, Text = text, Type = type };
			AddField(fld);
		}

		public void AddField(Fld field, string groupId)
        {
			Grp grp = Groups.Find(g => g.Id.Equals(groupId));

			if (grp == null)
			{
				grp = new Grp() { Id = groupId };
				Groups.Add(grp);
			}

			Fld fld = grp.Fields.Find(f => f.Name.Equals(field.Name));

			if (fld == null)
			{
				fld = field;
				grp.Fields.Add(fld);

			}else
			{
				fld.Text = field.Text;
			}

		}

		public string GetValueInGroup(string FieldName, string GroupName)
		{
			var grp = Groups.Find(g => g.Id.Equals(GroupName));

			return grp.Fields.Find(f => f.Name.Equals(FieldName)).Text;
		}

		public void SetValueInGroup(string FieldName, string val, string GroupName)
		{
			Fld fld = new Fld() { Name = FieldName, Text = val };

			AddField(fld, GroupName);
		}

		public void SetValueInGroup(Fld field, string groupId)
		{
			AddField(field, groupId);
		}

		public Fld GetField(string fieldName)
		{
			throw new NotImplementedException();
		}
		public Fld GetFieldFromGroup(string fieldName, string groupId)
		{
			var grp = Groups.Find(f => f.Id.Equals(groupId));

			if (grp == null)
			{
				return null;
			}

			return grp.Fields.FirstOrDefault(f => f.Name.Equals(fieldName));
		}

		public string GetValueInTab(string FieldName, string TabName)
		{
			var tab = Tables.Find(t => t.Lines.Where(l => l.Fields.Where(f => f.Name.Equals(FieldName)) != null) != null);
			var line = tab.Lines.Find(l => l.Fields.Where(f => f.Name.Equals(FieldName)) != null);

			return line.Fields.Find(f => f.Name.Equals(FieldName)).Text;
		}
		public void SetValueInTab(string val, string FieldName, string TabName)
		{
			throw new NotImplementedException();
		}
	}
}
