using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Service.Contracts.Database
{
    partial class DynamicDB
    {
        public void ValidateData(CatalogDefinition catalog, JObject o)
		{
            foreach (var field in catalog.Fields)
            {
				if (o.ContainsKey(field.Name))
				{
					if (field.Type == ColumnType.Set || field.Type == ColumnType.Reference) continue;
					var fieldData = GetValue(o, field.Name, field.Type);
					ValidateEmptyValue(field, fieldData);
					ValidateCharacterLength(field, fieldData, o);
					DataTypeValidations(field, fieldData);
				}
            }
		}

		public object GetValue(JObject root, string key, ColumnType type)
		{
			if (root[key].Type == JTokenType.Null)
				return GetDefaultValue(type);
			try
			{
				switch (type)
				{
					case ColumnType.Bool:
						return root.GetValue<bool>(key);
					case ColumnType.Int:
						return root.GetValue<int>(key);
					case ColumnType.Long:
						return root.GetValue<long>(key);
					case ColumnType.String:
						return root.GetValue<string>(key);
					case ColumnType.Decimal:
						return root.GetValue<double>(key);
					case ColumnType.Date:
						return root.GetValue<DateTime>(key);
					case ColumnType.Reference:
						return root.GetValue<int>(key);
					case ColumnType.Set:
					default:
						throw new Exception($"Cannot get the value of {key} becuase it is of type {type}");
				}
			}
			catch (Exception ex)
			{
				throw new Exception($"Error while getting column value. Column: {key}, Type: {type}", ex);
			}
		}


		private void DataTypeValidations(FieldDefinition field, object fieldData)
		{
			switch (field.Type)
			{
				case ColumnType.Int:
					IntValidations(field, fieldData);
					break;
				case ColumnType.Long:
					LongValidations(field, fieldData);
					break;
				case ColumnType.Decimal:
					DecimalValidations(field, fieldData);
					break;
				case ColumnType.Bool:
					BooleanValidations(field, fieldData);
					break;
				case ColumnType.Date:
					DateValidations(field, fieldData);
					break;
				case ColumnType.String:
					StringValidations(field, fieldData);
					break;
				case ColumnType.Reference:
					ReferenceValidations(field, fieldData);
					break;
			}
		}


		private void ValidateEmptyValue(FieldDefinition field, object fieldData)
		{
			if (field.CanBeEmpty) return;
			if (fieldData == null)
				throw new Exception($"Field {field.Name} cannot be null.");
			var str = fieldData.ToString();
			if (str.Length == 0)
				throw new Exception($"Field {field.Name} cannot be empty.");
		}


		private void ValidateCharacterLength(FieldDefinition field, object fieldData, JObject entity)
		{
			if (field.Length.HasValue && fieldData != null)
			{
				var str = fieldData.ToString();
				if (str.Length > field.Length.Value)
					throw new Exception($"The length of field {field.Name}, the length '{str.Length}' is greather than defined in the database ({field.Length})");
			}
		}


		public void IntValidations(FieldDefinition field, object fieldData)
		{
			if (fieldData == null) return;
			int intVal;
			if (fieldData.GetType() == typeof(int))
				intVal = (int)fieldData;
			else if (!int.TryParse(fieldData.ToString(), out intVal))
				throw new Exception($"Field {field.Name} with value '{intVal}' does not have a valid Int32 value.");
			if (intVal < field.MinValue || intVal > field.MaxValue)
				throw new Exception($"Field {field.Name} with value '{intVal}' is not in the allowed Range: ({field.MinValue} - {field.MaxValue})");
		}


		public void LongValidations(FieldDefinition field, object fieldData)
		{
			if (fieldData == null) return;
			long longVal;
			if (fieldData.GetType() == typeof(long))
				longVal = (long)fieldData;
			else if (!long.TryParse(fieldData.ToString(), out longVal))
				throw new Exception($"Field {field.Name} with value '{longVal}' does not have a valid Int64 value.");
			if (longVal < field.MinValue || longVal > field.MaxValue)
				throw new Exception($"Field {field.Name} with value '{longVal}' is not in the allowed Range: ({field.MinValue} - {field.MaxValue})");
		}


		public void DecimalValidations(FieldDefinition field, object fieldData)
		{
			if (fieldData == null) return;
			double doubleVal;
			if (fieldData.GetType() == typeof(double))
				doubleVal = (double)fieldData;
			else if (!double.TryParse(fieldData.ToString(), out doubleVal))
				throw new Exception($"Field {field.Name} with value '{doubleVal}' does not have a valid Double value.");
			if (doubleVal < field.MinValue || doubleVal > field.MaxValue)
				throw new Exception($"{field.Name}  is not in the allowed Range: ({field.MinValue} - {field.MaxValue})");
		}


		private void BooleanValidations(FieldDefinition field, object fieldData)
		{
			if (fieldData != null)
			{
				if (fieldData.GetType() != typeof(bool))
					throw new Exception($"Field {field.Name} must be a valid boolean value.");
			}
		}


		private void DateValidations(FieldDefinition field, object fieldData)
		{
			if (fieldData != null)
			{
				DateTime dateValue;
				if (fieldData.GetType() != typeof(DateTime))
					dateValue = (DateTime)fieldData;
				else
				{
					if (!DateTime.TryParse(fieldData.ToString(), out dateValue))
						throw new Exception($"Field {field.Name} with value '{dateValue.ToString("yyyy-MM-dd hh:mm:ss")}' must be a valid date.");
				}
				if (field.MaxDate.HasValue && dateValue > field.MaxDate.Value)
					throw new Exception($"Field {field.Name} with value '{dateValue.ToString("yyyy-MM-dd hh:mm:ss")}' is out of the date range specified in the database.");
				if (field.MinDate.HasValue && dateValue < field.MinDate.Value)
					throw new Exception($"Field {field.Name} with value '{dateValue.ToString("yyyy-MM-dd hh:mm:ss")}' is out of the date range specified in the database.");
			}
		}


		private void StringValidations(FieldDefinition field, object fieldData)
		{
			if(fieldData != null)
			{
				if (fieldData.GetType() != typeof(string))
					throw new Exception($"Field {field.Name} does not contain a valid string.");
				var str = (string)fieldData;
				if (field.ValidChars != null && !ValidateCharacters(str, field.ValidChars))
					throw new Exception($"Field {field.Name} contains some invalid character(s).");
				if(field.Regex != null && !ValidateRegularExpression(str, field.Regex))
					throw new Exception($"Field {field.Name} must match the Regular Expression specified in the database.");
			}
		}


		private void ReferenceValidations(FieldDefinition field, object fieldData)
		{
			if (fieldData != null)
			{
				if (fieldData.GetType() != typeof(int))
					throw new Exception($"Field {field.Name} does not contain a valid {field.Type} ID.");
			}
		}


		public bool ValidateCharacters(string value, string validChars)
        {
            if (string.IsNullOrEmpty(validChars))
                return true;

            return value.All(c => validChars.Contains(c));
        }


        public bool ValidateRegularExpression(string value, string regularExpression)
        {
            if (Equals(regularExpression, null))
                return true;

            Regex Validator = new Regex(regularExpression);
            return Validator.IsMatch(value);
        }


		public static object GetDefaultValue(ColumnType type)
		{
			switch (type)
			{
				case ColumnType.Bool:
					return false;
				case ColumnType.Int:
					return 0;
				case ColumnType.Long:
					return 0L;
				case ColumnType.String:
					return null;
				case ColumnType.Decimal:
					return 0d;
				case ColumnType.Date:
					return DateTime.Now;
				case ColumnType.Reference:
					return 0;
				case ColumnType.Set:
				default:
					throw new Exception($"Cannot get the default value of a field of type {type}");
			}
		}
	}
}
