using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Service.Contracts.Database
{
    partial class DynamicDB
    {
        public bool ValidateFields(List<FieldDefinition> catalogFieldList, List<string> languages)
		{
            foreach (var data in catalogFieldList)
			{
				//validate Name and Type Not Empties
				if (string.IsNullOrEmpty(data.Name) || string.IsNullOrEmpty(data.Type.ToString()))
				{
					throw new Exception("Name cannot be empty.");
				}

                //Validate only allowed characters on Name field
                IsValidName(data.Name);

                //Validate Captions
                if (!ValidateCaptions(data.Captions, languages))
				{
					throw new Exception("Invalid captions.");
				}

				//Validate existence of data Type
				if (!ValidateDataType(data.Type))
				{
					throw new Exception("Invalid data type.");
				}

				//DataType validations
				if (!CatalogDataTypeValidations(data.CatalogID, data))
				{
					throw new Exception("Value dos not meet data type restrictions.");
				}
			}

			return false;
		}

        //Validate Name: validate if the current field has a valid name
        public void IsValidName(string value)
        {
            var regExPattern = @"^[a-zA-Z][a-zA-Z0-9\\_]*$";
            Regex Validator = new Regex(regExPattern);
            if (!Validator.IsMatch(value)) 
                throw new Exception("The specified Name has some invalid characters");
        }

        public bool ValidateLength(int? value, int minValue, int maxValue)
		{
			if (!value.HasValue)
				return true;

            return !(value < minValue || value > maxValue);
		}

		//Validate captions: Not empty, not duplicated or not existent languages
		public bool ValidateCaptions(string captions, List<string> languages)
		{
			if (string.IsNullOrWhiteSpace(captions)) return true;
            var captionsList = JsonConvert.DeserializeObject<List<CaptionDef>>(captions);

			var hasDuplicatedLanguages = captionsList.GroupBy(x => x.Language).Any(e => e.Count() > 1);
			var hasEmptyValues = captionsList.Any(x => (!languages.Contains(x.Language)) || string.IsNullOrEmpty(x.Language) || string.IsNullOrEmpty(x.Text));

            return !(hasDuplicatedLanguages || hasEmptyValues);
		}

        //validate just supported datatypes
        public bool ValidateDataType(ColumnType dataType)
		{
			var isValidDataType = Enum.IsDefined(typeof(ColumnType), dataType);

            return isValidDataType;
		}

        //Each DataType has own validations
        public bool CatalogDataTypeValidations(int? catalogId, FieldDefinition data)
		{
			switch (data.Type)
			{
				case ColumnType.Reference:
                case ColumnType.Set:
					{
                        var catalog = GetCatalog(catalogId.Value);
                        if ((data.CatalogID.HasValue && Equals(data.CatalogID, 0)) || Equals(catalog, null))
                            return false;
                        break;
					}
				case ColumnType.Int:
					{
						if (!ValidateLength(data.Length, 1, 12))
							return false;
						break;
					}
				case ColumnType.Long:
					{
						if (!ValidateLength(data.Length, 1, 16))
							return false;
						break;
					}
				case ColumnType.Decimal:
					{
						if (!ValidateLength(data.Length, 1, 20))
							return false;
						break;
					}
				case ColumnType.Bool:
                case ColumnType.Date:
                default://string
                    {
                        if (!ValidateLength(data.Length, 1, 9999))
                            return false;
                        break;
                    }
            }

			return true;
		}
	}
}
