using System;

namespace Zebra.Sdk.Printer
{
	/// <summary>
	///       This class is used to describe format variable fields.
	///       </summary>
	public class FieldDescriptionData : IComparable<FieldDescriptionData>
	{
		private int fieldNumber;

		private string fieldName;

		/// <summary>
		///       In CPCL, this field is always null.<br />
		///       In ZPL, this field will correspond to the optional name parameter of the ^FN command, or null if the parameter is not present
		///       </summary>
		public string FieldName
		{
			get
			{
				return this.fieldName;
			}
			set
			{
				this.fieldName = value;
			}
		}

		/// <summary>
		///       In CPCL, this number will be the number of the variable field in the format. The fields are numbered starting at 1.<br />
		///       In ZPL, this number will correspond to the ^FN number.
		///       </summary>
		public int FieldNumber
		{
			get
			{
				return this.fieldNumber;
			}
			set
			{
				this.fieldNumber = value;
			}
		}

		/// <summary>
		///       Create a descriptor for a field
		///       </summary>
		/// <param name="fieldNumber">The number of the field.</param>
		/// <param name="fieldName">The name of the field, or null if not present.</param>
		public FieldDescriptionData(int fieldNumber, string fieldName)
		{
			this.fieldNumber = fieldNumber;
			this.fieldName = fieldName;
		}

		/// <summary>Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.</summary>
		/// <param name="obj">An object to compare with this instance. </param>
		/// <returns>A value that indicates the relative order of the objects being compared. The return value has these meanings: Value Meaning Less than zero This instance precedes <paramref name="obj" /> in the sort order. Zero This instance occurs in the same position in the sort order as <paramref name="obj" />. Greater than zero This instance follows <paramref name="obj" /> in the sort order. </returns>
		/// <exception cref="T:System.ArgumentException">
		///   <paramref name="obj" /> is not the same type as this instance. </exception>
		public int CompareTo(FieldDescriptionData obj)
		{
			return this.FieldNumber - obj.FieldNumber;
		}
	}
}