using Service.Contracts;
using Services.Core;
using StructureMangoOrderFileColor;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace JsonColor
{
    public static class FieldsHelper
    {
        private static readonly HashSet<string> FastDescriptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "WOMAN", "MAN" };
        private static readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> PrintDescriptionGender =
            new Dictionary<string, Dictionary<string, Dictionary<string, string>>>(StringComparer.OrdinalIgnoreCase)
            {
                 { "KIDS", new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "KIDS", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                            {
                                { "FEMALE", "KIDS GIRL" },
                                { "MALE", "KIDS BOY" },
                            }
                        },
                        { "BABY", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                            {
                                { "FEMALE", "BABY GIRL" },
                                { "MALE", "BABY BOY" },
                            }
                        },
                        { "NEW-BORN", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                            {
                                { "FEMALE", "NEWBORN G." },
                                { "MALE", "NEWBORN B." },
                                { "UNISEX","NEWBORN UNISEX" }
                            }
                        }
                    }
                 },
                 { "TEEN", new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "TEEN", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                            {
                                { "FEMALE", "TEEN GIRLS" },
                                { "MALE", "TEEN BOYS" },
                            }
                        }
                    }
                 },
                 { "HOME UNISEX", new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "ADULT", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                            {

                                { "FEMALE", "HOME" },
                                { "MALE", "HOME" },
                                { "UNISEX","HOME" }
                            }
                        },
                        { "KIDS", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                            {
                                { "FEMALE", "HOME KIDS" },
                                { "MALE", "HOME KIDS" },
                                { "UNISEX","HOME KIDS" }
                            }
                        },
                        { "BABY", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                            {
                                { "FEMALE", "HOME BABY" },
                                { "MALE", "HOME BABY" },
                                { "UNISEX","HOME BABY" }
                            }
                        },
                        { "NEW-BORN", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                            {

                                { "FEMALE", "HOME NEW-BORN" },
                                { "MALE", "HOME NEW-BORN" },
                                { "UNISEX","HOME NEW-BORN" }
                            }
                        }
                    }
                 },
                 { "HOME", new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "ADULT", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                            {
                                { "FEMALE", "HOME" },
                                { "MALE", "HOME" },
                                { "UNISEX","HOME" }
                            }
                        },
                        { "KIDS", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                            {
                                { "FEMALE", "HOME KIDS"},
                                { "MALE", "HOME KIDS"},
                                { "UNISEX","HOME KIDS" }
                            }
                        },
                        { "BABY", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                            {
                                { "FEMALE", "HOME BABY" },
                                { "MALE", "HOME BABY" },
                                { "UNISEX","HOME BABY" }
                            }
                        },
                        { "NEW-BORN", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                            {
                                { "FEMALE", "HOME NEW-BORN" },
                                { "MALE", "HOME NEW-BORN" },
                                { "UNISEX","HOME NEW-BORN" }
                            }
                        }
                    }
                 }
            };
        public static string CreateFieldHead(string fields, int interactionStart, int interactionFinish, char delimeter)
        {
            string respose = "";
            for(int interactionCounter = interactionStart; interactionFinish >= interactionCounter; interactionCounter++)
            {
                respose += fields.Replace(delimeter.ToString(), interactionCounter.ToString() + delimeter);
            }
            return respose;
        }
        public static string CreateFieldComposition(string titles, string fabrics, int interactionTitleStart, int interactionTotleFinish, int interactionFabricStart, int interactionFabricFinish, char delimeter)
        {
            var titleFields = titles.Split(delimeter).ToList();
            string respose = "";
            for(int titleCounter = interactionTitleStart; interactionTotleFinish >= titleCounter; titleCounter++)
            {
                titleFields.ForEach(field => respose += field + titleCounter.ToString() + delimeter);
                respose += CreateFieldFabric(fabrics, titleCounter.ToString(), interactionFabricStart, interactionFabricFinish, delimeter);
            }
            return respose;
        }

        public static string CreateFieldFabric(string fabrics, string titleCounter, int interactionFabricStart, int interactionFabricFinish, char delimeter)
        {

            var fabricFields = fabrics.Split(delimeter).ToList();
            string respose = "";

            for(int fabricCounter = interactionFabricStart; interactionFabricFinish >= fabricCounter; fabricCounter++)
            {
                fabricFields.ForEach(field => respose += field.Replace("Fabric_", "Fabric" + fabricCounter.ToString() + "_") + titleCounter + delimeter);

            }


            return respose;
        }
        public static string GetPrintDescriptionGender(StyleColor styleColor, ILogService log)
        {
            var lineKey = styleColor.Line?.Trim();
            var ageKey = styleColor.Age?.Trim();
            var genderKey = styleColor.Gender?.Trim();

            if(string.IsNullOrEmpty(lineKey))
            {
                log.LogMessage("Warning: StyleColor.Line is null or empty");
                return ClientDefinitions.NotFoundDescription;
            }


            if(FastDescriptions.Contains(lineKey))
            {
                return styleColor.Line;
            }

            if(!PrintDescriptionGender.TryGetValue(lineKey, out var genderDict))
            {
                log.LogMessage($"Warning: PrintDescriptionGender not found for Line: {lineKey}");
                return ClientDefinitions.NotFoundDescription;
            }

            if(!genderDict.TryGetValue(ageKey, out var ageDict))
            {
                log.LogMessage($"Warning: PrintDescriptionGender not found for Line: {lineKey}, Age: {ageKey}");
                return ClientDefinitions.NotFoundDescription;
            }

            if(!ageDict.TryGetValue(genderKey, out var description))
            {
                log.LogMessage($"Warning: PrintDescriptionGender not found for Line: {lineKey}, Age: {ageKey}, Gender: {genderKey}");
                return ClientDefinitions.NotFoundDescription;
            }

            return description;
        }
        public static string CreateDelimeter(int fieldsQty, int interactionQty)
        {
            string respose = "";
            for(int interactionCounter = 1; interactionQty >= interactionCounter; interactionCounter++)
            {
                respose += CreateDelimeterByfieldsQty(fieldsQty);
            }
            return respose;
        }
        public static string CreateDelimeterByfieldsQty(int fieldsQty)
        {
            string respose = "";
            for(int delimeterCounter = 1; fieldsQty >= delimeterCounter; delimeterCounter++)
            {
                respose += ClientDefinitions.delimeter;
            }
            return respose;
        }

        public static void LoadFields(
            ref string line,
            ref string head,
            object orderData,
            bool hasHead,
            ILogService log,
            string[] notIncludeFields,
            string suffix = null)
        {
            if(orderData == null)
                throw new InvalidOperationException($"Error: The objtec {orderData.GetType().Name} can't be empty");

            var properties = orderData.GetType().GetProperties().ToList();


            foreach(var property in properties)
            {
                if(property != null)
                {
                    var fieldName = property.Name;
                    if(!notIncludeFields.Contains(fieldName))
                    {
                        var fieldValue = Rfc4180Writer.QuoteValue((property.GetValue(orderData) ?? "").ToString());
                        switch(property.Name)
                        {
                            case "ProductionDate":
                                try
                                {
                                    //Fuente: https://www.iteramos.com/pregunta/69888/cnet-convertir-fecha-yyyymmdd-a-formato-systemdatetime
                                    fieldValue = DateTime.ParseExact(fieldValue, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AllowInnerWhite).ToString("dd-MM-yyyy");
                                }
                                catch(Exception ex)
                                {
                                    log.LogMessage($"Error: when to try parse ProductionDate: ({ex.Message}) ");
                                }
                                break;
                            case "SupplierCode":
                                try
                                {
                                    fieldValue = int.Parse(fieldValue).ToString();
                                }
                                catch(Exception ex)
                                {
                                    log.LogMessage($"Error: when to try parse SupplierCode: ({ex.Message}) ");
                                }
                                break;
                            case "MangoColorCode":
                                fieldName = orderData.GetType().Name == "StyleColor" ? "MangoColorCode_StyleColor" : property.Name;
                                break;

                        }
                        if(!hasHead) head += string.Concat(fieldName, (suffix == null ? "" : suffix), ClientDefinitions.delimeter);
                        line += string.Concat(fieldValue, ClientDefinitions.delimeter);
                    }
                }
            }
        }
    }
}
