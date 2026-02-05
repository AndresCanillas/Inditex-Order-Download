using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.Database
{
	public static class CatalogDefinitionExt
	{
		public static CatalogDefinition GetCatalogDefinition(this Type t)
		{
			var catalog = new CatalogDefinition();
			var fields = new List<FieldDefinition>();
			catalog.Name = t.Name;
			var attrs = t.GetCustomAttributes();
			foreach (var attr in attrs)
			{
				if (attr is TargetTableAttribute)
					catalog.Name = (attr as TargetTableAttribute).TableName;
				if (attr is Readonly || attr is ReadOnlyAttribute)
					catalog.IsReadonly = true;
				if (attr is Hidden)
					catalog.IsHidden = true;
			}
			var members = t.GetMembers(BindingFlags.Public | BindingFlags.Instance);
			var fieldId = 0;
			foreach (var m in members)
			{
				var ignore = m.GetCustomAttribute<IgnoreFieldAttribute>();
				if (ignore != null) continue;
				if (m.MemberType == MemberTypes.Property)
				{
					var p = m as PropertyInfo;
					if (p.PropertyType.IsValidCatalogType())
					{
						FieldDefinition fd = p.GetFieldDefinition();
						fd.FieldID = fieldId++;
						fields.Add(fd);
					}
				}
				else if (m.MemberType == MemberTypes.Field)
				{
					var f = m as FieldInfo;
					if (f.FieldType.IsValidCatalogType())
					{
						FieldDefinition fd = f.GetFieldDefinition();
						fd.FieldID = fieldId++;
						fields.Add(fd);
					}
				}
			}
			catalog.Definition = JsonConvert.SerializeObject(fields);
			return catalog;
		}

		public static FieldDefinition GetFieldDefinition(this PropertyInfo propertyInfo)
		{
			return GetFieldDefinition(
				propertyInfo.Name,
				propertyInfo.PropertyType,
				propertyInfo.GetCustomAttributes());
		}

		public static FieldDefinition GetFieldDefinition(this FieldInfo fieldInfo)
		{
			return GetFieldDefinition(
				fieldInfo.Name,
				fieldInfo.FieldType,
				fieldInfo.GetCustomAttributes());
		}

		private static FieldDefinition GetFieldDefinition(string name, Type type, IEnumerable<Attribute> attrs)
		{
			FieldDefinition fd = new FieldDefinition();
			fd.Name = name;
			fd.Type = type.GetCatalogDataType();
			fd.IsSystem = false;
			fd.IsLocked = false;
			if (type.IsNullable())
				fd.CanBeEmpty = true;
			if (name.ToLower() == "id")
			{
				fd.IsKey = true;
				fd.IsIdentity = true;
				fd.IsReadOnly = true;
				fd.IsHidden = true;
				fd.CanBeEmpty = false;
				fd.IsSystem = true;
				fd.IsLocked = true;
			}
			foreach (var attr in attrs)
			{
				var attrName = attr.GetType().Name;
				if (attrName.StartsWith("PK"))
				{
					fd.IsKey = true;
					fd.IsHidden = true;
				}
				if (attrName.StartsWith("Identity"))
				{
					fd.IsIdentity = true;
					fd.IsReadOnly = true;
					fd.IsHidden = true;
				}
				if (attrName.StartsWith("SystemField"))
					fd.IsSystem = true;
				if (attrName.StartsWith("Locked"))
					fd.IsLocked = true;
				if (attrName.StartsWith("Readonly"))
					fd.IsReadOnly = true;
				if (attrName.StartsWith("Caption"))
					fd.Captions = CreateDefaultCaption((string)Reflex.GetMember(attr, "Text"));
				if (attrName.StartsWith("Description"))
					fd.Description = (string)Reflex.GetMember(attr, "Text");
				if (attrName.StartsWith("MaxLength"))
					fd.Length = (int)Reflex.GetMember(attr, "Length");
				else if (attrName.StartsWith("MaxLen"))
					fd.Length = (int)Reflex.GetMember(attr, "Value");
				if (attrName.StartsWith("Nullable"))
					fd.CanBeEmpty = true;
				if (attrName.StartsWith("Required"))
					fd.CanBeEmpty = false;
				if (attrName.StartsWith("Range"))
				{
					fd.MinValue = (int)Reflex.GetMember(attr, "MinValue");
					fd.MaxValue = (int)Reflex.GetMember(attr, "MaxValue");
				}
				if (attrName.StartsWith("DateRange"))
				{
					fd.MinDate = (DateTime)Reflex.GetMember(attr, "MinDate");
					fd.MaxDate = (DateTime)Reflex.GetMember(attr, "MaxDate");
				}
				if (attrName.StartsWith("SizeRange"))
				{
					fd.MaxWidth = (int)Reflex.GetMember(attr, "MaxWidth");
					fd.MaxHeight = (int)Reflex.GetMember(attr, "MaxHeight");
				}
				if (attrName.StartsWith("ValidChars"))
					fd.ValidChars = (string)Reflex.GetMember(attr, "Value");
				if (attrName.StartsWith("RegEx"))
					fd.Regex = (string)Reflex.GetMember(attr, "Value");
				if (attrName.StartsWith("Unique"))
					fd.IsUnique = true;
				if (attrName.StartsWith("Readonly"))
					fd.IsReadOnly = true;
				if (attrName.StartsWith("Hidden"))
					fd.IsHidden = true;
				if (attrName.StartsWith("MainDisplay"))
					fd.IsMainDisplay = true;
			}
			if (fd.IsUnique) fd.CanBeEmpty = false;
			return fd;
		}

		private static string CreateDefaultCaption(string caption)
		{
			var result = new List<CaptionDef>();
			result.Add(new CaptionDef() { Language = "en-US", Text = caption });
			return JsonConvert.SerializeObject(result);
		}

		public static bool IsValidCatalogType(this Type t)
		{
			return (t == typeof(int) || t == typeof(long) ||
				t == typeof(float) || t == typeof(double) ||
				t == typeof(Decimal) || t == typeof(bool) ||
				t == typeof(DateTime) || t == typeof(string) ||
				t == typeof(byte[]) || t.IsEnum);
		}

		public static ColumnType GetCatalogDataType(this Type t)
		{
			if (t == typeof(int) || t.IsEnum)
				return ColumnType.Int;
			if (t == typeof(long))
				return ColumnType.Long;
			if (t == typeof(float) || t == typeof(double) || t == typeof(Decimal))
				return ColumnType.Decimal;
			if (t == typeof(bool))
				return ColumnType.Bool;
			if (t == typeof(DateTime))
				return ColumnType.Date;
			if (t == typeof(string))
				return ColumnType.String;
			throw new Exception($"Column of type {t.Name} is not valid");
		}

		public static bool IsNullable(this Type t)
		{
			return Nullable.GetUnderlyingType(t) != null;
		}
	}
}
