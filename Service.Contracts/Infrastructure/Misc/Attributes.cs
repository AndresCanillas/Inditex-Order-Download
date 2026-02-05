using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Service.Contracts
{
	// Allows to provide a friendly name for a system or service. The value will be used as part of the metadata.
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property)]
	public class FriendlyName : Attribute
	{
		public string Text;

		public FriendlyName(string friendlyName)
		{
			Text = friendlyName;
		}
	}

	// Allows to provide a description associated to the UI control that will represent this field. The description
	// will be shown as a tooltip in the UI. When used to decorate a service or a system class, then the value is
	// used as part of the metadata.
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
	public class Description : Attribute
	{
		public string Text;

		public Description(string description)
		{
			Text = description;
		}
	}

	// Allows to provide the text of the label associated to the UI control that will represent this field.
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class Caption : Attribute
	{
		public string Text;

		public Caption(string friendlyName)
		{
			Text = friendlyName;
		}
	}

	// Indicates that this field is meant to be displayed in the UI but in a disabled state.
	// A read only field cannot be edited. NOTE: There is no way of turning this on/off on
	// the UI side.
	// If applied to a class (table definition), then this should be interpreted such that the
	// catalog information can be seen by the users, but not edited interactively. Options such as
	// Adding new records, editing records, saving, deleting, as well as inline edition should
	// all be disabled when the entire catalog is marked as readonly.
	// Is important to understand that these attributes are only informational, in the end the application is
	// responsible for honoring or ignoring this settings.
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property)]
	public class Readonly : Attribute
	{
	}

	// Indicates that this field is not meant to be show in the UI.
	// IMPORTANT:
	// There is no way of turning this on/off on the UI side, the UI will completely skip this field and not render it.
	// If applied to a class (table definition), then this is interpreted as the catalog not showing in the catalogs UI.
	// Is important to understand that these attributes are only informational, in the end the application is
	// responsible for honoring or ignoring this settings.
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property)]
	public class Hidden: Attribute
	{
	}

	// Indicates that a field is required ans should not be left empty. Has no effect
	// when applied to complex types (fields whose type is another class). When applied
	// to components then a component needs to be selected. In the case of lists, then
	// at least ONE row needs to be supplied. 
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class Required : Attribute
	{
	}


	// Indicates that the value of a field should only accept one of the specified options.
	// options represents an array of values that will be displayed as a combobox in the UI.
	// Example:
	//
	// [FixedOptions("0=Option A|1=Option B|2=Option C")]
	//
	//
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class FixedOptions : Attribute
	{
		public string Options;

		public FixedOptions(string options)
		{
			Options = options;
		}

		public override string ToString()
		{
			return $"{{\"options\": \"{Options}\"}}";
		}

		public static FixedOptions FromEnum(Type enumType)
		{
			string value, name;
			StringBuilder sb = new StringBuilder(1000);
			var enumValues = Enum.GetValues(enumType);
			foreach (var elm in enumValues)
			{
				value = ((int)elm).ToString();
				name = Enum.GetName(enumType, elm);
				sb.Append($"{value}={name}|");
			}
			if (sb.Length > 0)
				sb.Remove(sb.Length - 1, 1);
			return new FixedOptions(sb.ToString());
		}

        internal static FixedOptions FromComponents(IEnumerable<Type> enumerable)
        {
            string value, name;
            StringBuilder sb = new StringBuilder(1000);
            sb.Append($"=|");
            foreach(var elm in enumerable)
                sb.Append($"{elm.Name}={elm.Name}|");
            sb.Length--;
            return new FixedOptions(sb.ToString());
        }
    }


	// Indicates that a field should not accept more than the specified number of characters.
	// If applied to a List, then it restricts how many rows can be added to the list.
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class MinLen : Attribute
	{
		public int Value;

		public MinLen(int value)
		{
			Value = value;
		}

		public override string ToString()
		{
			return $"'value':'{Value}'";
		}
	}

	// Indicates that a field should not accept more than the specified number of characters.
	// If applied to a List, then it restricts how many rows can be added to the list.
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class MaxLen : Attribute
	{
		public int Value;

		public MaxLen(int value)
		{
			Value = value;
		}

		public override string ToString()
		{
			return $"'value':'{Value}'";
		}
	}


	// Indicates that the field will only accept values between the provided values (inclusive range).
	// NOTE: This attribute is honored exclusively for numeric data types (byte, int, long, float, double & decimal)
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class Range : Attribute
	{
		public int MinValue;
		public int MaxValue;

		public Range(int min, int max)
		{
			MinValue = min;
			MaxValue = max;
		}

		public override string ToString()
		{
			return $"'Range':[{MinValue.ToString()}, {MaxValue.ToString()}]";
		}
	}

	// Indicates that the field will only accept dates between the provided values (inclusive range). Dates must be supplied in the following format: yyyy/MM/dd
	// NOTE: This attribute is honored exclusively for date fields
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class DateRange : Attribute
	{
		public DateTime MinDate;
		public DateTime MaxDate;

		public DateRange(string minDate, string maxDate)
		{
			MinDate = DateTime.ParseExact(minDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);
			MaxDate = DateTime.ParseExact(maxDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);
		}

		public override string ToString()
		{
			return $"'Date Range':[{MinDate.ToString("yyyy/MM/dd")}, {MaxDate.ToString("yyyy/MM/dd")}]";
		}
	}


	// Indicates that the field will only accept images with the maximum size specified.
	// NOTE: This attribute is honored exclusively for Image fields (byte[])
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class SizeRange : Attribute
	{
		public int MaxWidth;
		public int MaxHeight;

		public SizeRange(int width, int height)
		{
			MaxWidth = width;
			MaxHeight = height;
		}

		public override string ToString()
		{
			return $"'Size Range':[{MaxWidth.ToString()}, {MaxHeight.ToString()}]";
		}
	}


	// Indicates that a field should only accept characters in the specified set of characters.
	// NOTES: This attribute is honored only for string fields, control characters are automatically allowed.
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class ValidChars : Attribute
	{
		public string Value;

		public ValidChars(string value)
		{
			Value = value;
		}

		public override string ToString()
		{
			return $"'value':'{Value}'";
		}
	}


	// Indicates that a field should only accept values that match the specified regular expression.
	// NOTES: This attribute is honored only for string fields, control characters are automatically allowed.
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class RegEx : Attribute
	{
		public string Value;

		public RegEx(string value)
		{
			Value = value;
		}

		public override string ToString()
		{
			return $"'value':'{Value}'";
		}
	}

	// Indicates that the field should be treated as a password.
	// NOTE: This automatically implies data is encrypted at rest even if [EncryptedAtRest] is not specified
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class Password : Attribute
	{
	}

	// Indicates that the value of the decorated field should be filtered out (not returned in any response) when
	// data filtering has been enabled on the current request. 
	// NOTE: Data filtering can be enabled for each request as part of the authorization process.
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class Filtered : Attribute
	{
	}

	// Indicates that the data associated to an entire dto (class or interface), a field or property 
	// should be encrypted before persisting it. This is honored exclusively if the field is string or if
	// decorating an entire dto (class or interface)
	[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property)]
	public class EncryptAtRest : Attribute
	{
	}



	/// <summary>
	/// Attribute used to indicate that a property or field should be ignored when executing sql statements that insert or update data.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method)]
	public class IgnoreFieldAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of this class
		/// </summary>
		public IgnoreFieldAttribute() { }
	}


	/// <summary>
	/// Attribute used to indicate that a property or field should be ignored when executing sql statements that read data.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method)]
	public class LazyLoadAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of this class
		/// </summary>
		public LazyLoadAttribute() { }
	}

	/// <summary>
	/// Attribute used to specify the name of the target table. Used when the System.Type name is diferent than the name of the table.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class TargetTableAttribute : Attribute
	{
		/// <summary>
		/// The name of the target table
		/// </summary>
		public string TableName;

		/// <summary>
		/// Initializes a new instance of this class
		/// </summary>
		/// <param name="tablename">The name of the target table</param>
		public TargetTableAttribute(string tablename)
		{
			TableName = tablename;
		}
	}


	/// <summary>
	/// Attribute used to specify a foreign key relationship
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class FKAttribute : Attribute
	{
		public Type FKEntity;
		public string LeftColumns;
		public string RightColumns;

		/// <summary>
		/// Initializes a new instance of this class
		/// </summary>
		/// <param name="entity">The System.Type of the object used as entity</param>
		/// <param name="leftcolumns">The name of the column (this table column)</param>
		/// <param name="rightcolumns">The name of the referenced column (external table column)</param>
		public FKAttribute(Type entity, string leftcolumns, string rightcolumns)
		{
			if (entity == null || leftcolumns == null || rightcolumns == null ||
				leftcolumns.Length == 0 || rightcolumns.Length == 0)
				throw new ArgumentException();
			FKEntity = entity;
			LeftColumns = leftcolumns;
			RightColumns = rightcolumns;
		}
	}


	/// <summary>
	/// Attribute used to specify the name of the target table column (Used when a field does not have the same name than the column)
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class TargetColumnAttribute : Attribute
	{
		/// <summary>
		/// The name of the column in the table
		/// </summary>
		public string ColumnName;

		/// <summary>
		/// Initializes a new instance of this class
		/// </summary>
		/// <param name="columnname">The name of the target column</param>
		public TargetColumnAttribute(string columnname)
		{
			ColumnName = columnname;
		}
	}


	/// <summary>
	/// Attribute used to define a primary key, all fields decorated with this attribute are considered part of the primary key.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class PKAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of this class
		/// </summary>
		public PKAttribute() { }
	}


	/// <summary>
	/// Attribute used to identify which field is used as the identity field.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class IdentityAttribute : Attribute
	{
		/// <summary>
		/// Used only in Oracle where identity values are extracted from a sequence.
		/// </summary>
		public string SequenceName;

		/// <summary>
		/// Initializes a new instance of this class
		/// </summary>
		public IdentityAttribute() { }

		/// <summary>
		/// Use this constructor if the database is Oracle since identity columns must use a sequence.
		/// </summary>
		/// <param name="sequenceName">Name of the sequence to be used to get the value of the identity column, used in Oracle systems only.</param>
		public IdentityAttribute(string sequenceName)
		{
			SequenceName = sequenceName;
		}
	}


	/// <summary>
	/// Attribute used to identify which field(s) should have unique values. A unique index should exist for each of these.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class UniqueAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of this class
		/// </summary>
		public UniqueAttribute() { }
	}

	/// <summary>
	/// Attribute used to identify which field(s) a non-unique index.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class IndexedAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of this class
		/// </summary>
		public IndexedAttribute() { }
	}


	/// <summary>
	/// Attribute used to identify which field(s) should have unique values.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class MainDisplayAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of this class
		/// </summary>
		public MainDisplayAttribute() { }
	}

	/// <summary>
	/// Attribute used to identify a field as nullable. Allows the system to know that it needs to checks for nulls before manipulating the field.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class NullableAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of this class.
		/// </summary>
		public NullableAttribute() { }
	}


	/// <summary>
	/// Attribute used to identify which field is used as the identity field.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class SystemFieldAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of this class
		/// </summary>
		public SystemFieldAttribute() { }
	}

	/// <summary>
	/// Attribute used to identify which field is used as the identity field.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class LockedAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of this class
		/// </summary>
		public LockedAttribute() { }
	}
}
