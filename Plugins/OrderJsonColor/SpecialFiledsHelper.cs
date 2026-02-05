using Service.Contracts;
using Services.Core;
using StructureMangoOrderFileColor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace JsonColor
{
    public static class SpecialFiledsHelper
    {
       
        public static void LoadFieldsSizeRange(
           ref string line,
           ref string head,
           Sizerange sizeRange,
           bool hasHead,
           ILogService log,
           string[] notIncludeFields,
           string suffix = null)
        {
            if(sizeRange == null)
                throw new InvalidOperationException($"Error: The objtec {sizeRange.GetType().Name} can't be empty");

            var properties = sizeRange.GetType().GetProperties().ToList();

            foreach(var property in properties)
            {
                if(property != null)
                {
                    var fieldName = property.Name == "SizeName" ? "SizeRangeName" : property.Name;
                    if(!notIncludeFields.Contains(fieldName))
                    {
                        var sizes = "";
                        if(!string.IsNullOrEmpty((property.GetValue(sizeRange) ?? "").ToString()))
                        {
                            var rawValue = Rfc4180Writer.QuoteValue((property.GetValue(sizeRange) ?? "").ToString());
                            var parts = rawValue.Split('/');

                            // Validamos si todos los valores son números
                            if(fieldName == "SizeRangeName")
                                sizes = SortSizes(parts);
                            else

                                sizes = parts
                                    .OrderBy(x => x)
                                    .Merge("/");

                        }

                        if(!hasHead) head += string.Concat(fieldName, (suffix == null ? "" : suffix), ClientDefinitions.delimeter);
                        line += string.Concat(sizes, ClientDefinitions.delimeter);
                    }

                }
            }

        }
        public static void LoadCareInstructions(
           ref string line,
           ref string head,
           StyleColor styleColor,
           bool hasHead,
           string oderId,
           ILogService log,
           string[] notIncludeFields,
           bool includeCareInstructions)
        {
            int delimeterQty = ClientDefinitions.careinstructionFields.Count(x => ClientDefinitions.delimeter == x);
            int careInstructionsQty = styleColor.CareInstructions == null ? 0 : styleColor.CareInstructions.Count();
            if(careInstructionsQty != 0 && styleColor.CareInstructions != null && includeCareInstructions)
            {
                if(careInstructionsQty > ClientDefinitions.careinstructionsLimitQty)
                    throw new InvalidOperationException($"Error: CareInstructions can't be above 15 and the order ({oderId}) has {careInstructionsQty}");

                var indexCareInstruction = 1;
                foreach(var careInstruction in styleColor.CareInstructions)
                {
                    FieldsHelper.LoadFields(ref line, ref head, careInstruction, hasHead, log, notIncludeFields, indexCareInstruction++.ToString());

                }
                if(careInstructionsQty < ClientDefinitions.careinstructionsLimitQty)
                {
                    line += FieldsHelper.CreateDelimeter(delimeterQty, ClientDefinitions.careinstructionsLimitQty - careInstructionsQty);
                    if(!hasHead) head += FieldsHelper.CreateFieldHead(ClientDefinitions.careinstructionFields, careInstructionsQty + 1,
                        ClientDefinitions.careinstructionsLimitQty, ClientDefinitions.delimeter);
                }

            }
            else
            {
                line += FieldsHelper.CreateDelimeter(delimeterQty, ClientDefinitions.careinstructionsLimitQty);
                if(!hasHead) head += FieldsHelper.CreateFieldHead(ClientDefinitions.careinstructionFields, 1, ClientDefinitions.careinstructionsLimitQty,
                    ClientDefinitions.delimeter);
            }
        }
        public static void LoadComposition(
           ref string line,
           ref string head,
           StyleColor styleColor,
           bool hasHead,
           string OrderId,
           ILogService log,
           string[] notIncludeFields,
           bool includeComposition)
        {

            int delimeterCompositionQty = ClientDefinitions.compositionFields.Count(x => x == ClientDefinitions.delimeter);
            int compositionsQty = styleColor.Composition == null ? 0 : styleColor.Composition.Count();
            if(compositionsQty != 0 && styleColor.Composition != null && includeComposition)
            {
                if(compositionsQty > ClientDefinitions.titlesLimitQty)
                    throw new InvalidOperationException($"Error: Comoposition can't be above {ClientDefinitions.titlesLimitQty} " +
                        $"and the order ({OrderId}) has {compositionsQty}");


                var indexComposition = 1;
                foreach(var composition in styleColor.Composition)
                {
                    var suffixTitle = indexComposition++.ToString();
                    FieldsHelper.LoadFields(ref line, ref head, composition, hasHead, log, notIncludeFields, suffixTitle);


                    int delimeterFabricQty = ClientDefinitions.fabricFields.Count(x => ClientDefinitions.delimeter == x);
                    int fabricQty = composition.Fabric.Count();

                    if(fabricQty > ClientDefinitions.fabricLimitQty)
                        throw new InvalidOperationException($"Error: fabric can't be above {ClientDefinitions.fabricLimitQty} " +
                            $"and the order ({OrderId}) has {fabricQty}");

                    var indexFabric = 1;
                    foreach(var fabric in composition.Fabric)
                    {
                        var suffixFabric = string.Concat("_Fabric", indexFabric++, "_Title", suffixTitle);
                        FieldsHelper.LoadFields(ref line, ref head, fabric, hasHead, log, notIncludeFields, suffixFabric);
                    }
                    if(fabricQty < ClientDefinitions.fabricLimitQty)
                    {
                        line += FieldsHelper.CreateDelimeter(delimeterFabricQty + 1, ClientDefinitions.fabricLimitQty - fabricQty);
                        if(!hasHead) head += FieldsHelper.CreateFieldFabric(ClientDefinitions.fabricFields, suffixTitle, fabricQty + 1,
                            ClientDefinitions.fabricLimitQty, ClientDefinitions.delimeter);
                    }

                }
                if(compositionsQty < ClientDefinitions.titlesLimitQty)
                {

                    line += FieldsHelper.CreateDelimeter(delimeterCompositionQty, (ClientDefinitions.titlesLimitQty * ((4 * ClientDefinitions.fabricLimitQty) + 2))
                        - (compositionsQty * ((4 * ClientDefinitions.fabricLimitQty) + 2)));
                    if(!hasHead) head += FieldsHelper.CreateFieldComposition(ClientDefinitions.compositionFields, ClientDefinitions.fabricFields,
                        compositionsQty + 1, ClientDefinitions.titlesLimitQty, 1, ClientDefinitions.fabricLimitQty, ClientDefinitions.delimeter);
                }
            }
            else
            {
                line += FieldsHelper.CreateDelimeter(delimeterCompositionQty, ClientDefinitions.titlesLimitQty * ((4 * ClientDefinitions.fabricLimitQty) + 2));
                if(!hasHead) head += FieldsHelper.CreateFieldComposition(ClientDefinitions.compositionFields, ClientDefinitions.fabricFields, 1, ClientDefinitions.titlesLimitQty, 1, ClientDefinitions.fabricLimitQty, ClientDefinitions.delimeter);
            }
        }
        public static void LoadGarmentMeasurements(
           ref string line,
           ref string head,
           List<Garmentmeasurement> measurements,
           bool hasHead,
           string orderID,
           ILogService log,
           string[] notIncludeFields)
        {
            int delimeterQty = ClientDefinitions.measurementsFields.Count(x => ClientDefinitions.delimeter == x);
            int measurementsQty = measurements == null ? 0 : measurements.Count();
            if(measurementsQty != 0 && measurements != null)
            {
                if(measurementsQty > ClientDefinitions.measurementsLimitQty)
                    throw new InvalidOperationException($"Error: GarmentMeasurements can't be above 5 and the order ({orderID}) has {measurementsQty}");

                var indexmeasurements = 1;
                foreach(var careInstruction in measurements)
                {
                    FieldsHelper.LoadFields(ref line, ref head, careInstruction, hasHead, log, notIncludeFields, indexmeasurements++.ToString());

                }
                if(measurementsQty < ClientDefinitions.measurementsLimitQty)
                {
                    line += FieldsHelper.CreateDelimeter(delimeterQty, ClientDefinitions.measurementsLimitQty - measurementsQty);
                    if(!hasHead) head += FieldsHelper.CreateFieldHead(ClientDefinitions.measurementsFields, measurementsQty + 1, ClientDefinitions.measurementsLimitQty, ClientDefinitions.delimeter);
                }

            }
            else
            {
                line += FieldsHelper.CreateDelimeter(delimeterQty, ClientDefinitions.measurementsLimitQty);
                if(!hasHead) head += FieldsHelper.CreateFieldHead(ClientDefinitions.measurementsFields, 1, ClientDefinitions.measurementsLimitQty, ClientDefinitions.delimeter);
            }
        }
        public static void LoadSizeDescriptions(
           ref string line,
           ref string head,
           List<Sizedescription> descriptions,
           bool hasHead,
           string orderID,
           ILogService log,
           string[] notIncludeFields)
        {
            int delimeterQty = ClientDefinitions.sizeDescriptionFields.Count(x => ClientDefinitions.delimeter == x);
            int descQty = descriptions == null ? 0 : descriptions.Count();
            if(descQty != 0 && descriptions != null)
            {
                if(descQty > ClientDefinitions.sizeDescriptionLimitQty)
                    throw new InvalidOperationException($"Error: SizeDescriptions can't be above {ClientDefinitions.sizeDescriptionLimitQty} and the order ({orderID}) has {descQty}");
                var index = 1;
                foreach(var desc in descriptions)
                {
                    FieldsHelper.LoadFields(ref line, ref head, desc, hasHead, log, notIncludeFields, index++.ToString());
                }
                if(descQty < ClientDefinitions.sizeDescriptionLimitQty)
                {
                    line += FieldsHelper.CreateDelimeter(delimeterQty, ClientDefinitions.sizeDescriptionLimitQty - descQty);
                    if(!hasHead) head += FieldsHelper.CreateFieldHead(ClientDefinitions.sizeDescriptionFields, descQty + 1, ClientDefinitions.sizeDescriptionLimitQty, ClientDefinitions.delimeter);
                }
            }
            else
            {
                line += FieldsHelper.CreateDelimeter(delimeterQty, ClientDefinitions.sizeDescriptionLimitQty);
                if(!hasHead) head += FieldsHelper.CreateFieldHead(ClientDefinitions.sizeDescriptionFields, 1, ClientDefinitions.sizeDescriptionLimitQty, ClientDefinitions.delimeter);
            }
        }

        public static void LoadPvps(
           ref string line,
           ref string head,
           List<PVP> pvps,
           bool hasHead,
           string orderID,
           ILogService log,
           string[] notIncludeFields)
        {
            int fieldsQty = 9;
            int pvpQty = pvps == null ? 0 : pvps.Count;
            if(pvpQty != 0 && pvps != null)
            {
                if(pvpQty > ClientDefinitions.pvpLimitQty)
                    throw new InvalidOperationException($"Error: PVP can't be above {ClientDefinitions.pvpLimitQty} and the order ({orderID}) has {pvpQty}");
                int index = 1;
                foreach(var pvp in pvps)
                {
                    string price = "";
                    string origin = "";
                    var props = pvp.GetType().GetProperties();
                    foreach(var pr in props)
                    {
                        if(pr.Name.StartsWith("PVP") && pr.GetValue(pvp) != null)
                        {
                            price = (pr.GetValue(pvp) ?? "").ToString();
                            origin = pr.Name.Replace("PVP_", "").Replace("PVP", "");
                            break;
                        }
                    }
                    string currency = (pvp.GetType().GetProperty("Currency")?.GetValue(pvp) ?? "").ToString();
                    string m1 = "", mp1 = "", mc1 = "";
                    string m2 = "", mp2 = "", mc2 = "";
                    var measuresObj = pvp.GetType().GetProperty("Measures")?.GetValue(pvp) as System.Collections.IEnumerable;
                    if(measuresObj != null)
                    {
                        var measures = measuresObj.Cast<dynamic>().ToList();
                        if(measures.Count > 0)
                        {
                            m1 = (measures[0].GetType().GetProperty("Measure")?.GetValue(measures[0]) ?? "").ToString();
                            mp1 = (measures[0].GetType().GetProperty("PVP")?.GetValue(measures[0]) ?? "").ToString();
                            mc1 = (measures[0].GetType().GetProperty("Currency")?.GetValue(measures[0]) ?? "").ToString();
                        }
                        if(measures.Count > 1)
                        {
                            m2 = (measures[1].GetType().GetProperty("Measure")?.GetValue(measures[1]) ?? "").ToString();
                            mp2 = (measures[1].GetType().GetProperty("PVP")?.GetValue(measures[1]) ?? "").ToString();
                            mc2 = (measures[1].GetType().GetProperty("Currency")?.GetValue(measures[1]) ?? "").ToString();
                        }
                    }
                    if(!hasHead)
                    {
                        head += $"PVP{index}{ClientDefinitions.delimeter}Origin{index}{ClientDefinitions.delimeter}Currency{index}{ClientDefinitions.delimeter}" +
                                $"Measure{index}{ClientDefinitions.delimeter}MeasurePVP{index}{ClientDefinitions.delimeter}MeasureCurrency{index}{ClientDefinitions.delimeter}" +
                                $"Measure{index}_2{ClientDefinitions.delimeter}MeasurePVP{index}_2{ClientDefinitions.delimeter}MeasureCurrency{index}_2{ClientDefinitions.delimeter}";
                    }
                    line += string.Concat(price, ClientDefinitions.delimeter, origin, ClientDefinitions.delimeter, currency, ClientDefinitions.delimeter,
                        m1, ClientDefinitions.delimeter, mp1, ClientDefinitions.delimeter, mc1, ClientDefinitions.delimeter,
                        m2, ClientDefinitions.delimeter, mp2, ClientDefinitions.delimeter, mc2, ClientDefinitions.delimeter);
                    index++;
                }
                if(pvpQty < ClientDefinitions.pvpLimitQty)
                {
                    int rest = ClientDefinitions.pvpLimitQty - pvpQty;
                    line += FieldsHelper.CreateDelimeter(fieldsQty, rest);
                    if(!hasHead)
                    {
                        for(int i = pvpQty + 1; i <= ClientDefinitions.pvpLimitQty; i++)
                        {
                            head += $"PVP{i}{ClientDefinitions.delimeter}Origin{i}{ClientDefinitions.delimeter}Currency{i}{ClientDefinitions.delimeter}" +
                                    $"Measure{i}{ClientDefinitions.delimeter}MeasurePVP{i}{ClientDefinitions.delimeter}MeasureCurrency{i}{ClientDefinitions.delimeter}" +
                                    $"Measure{i}_2{ClientDefinitions.delimeter}MeasurePVP{i}_2{ClientDefinitions.delimeter}MeasureCurrency{i}_2{ClientDefinitions.delimeter}";
                        }
                    }
                }
            }
            else
            {
                line += FieldsHelper.CreateDelimeter(fieldsQty, ClientDefinitions.pvpLimitQty);
                if(!hasHead)
                {
                    for(int i = 1; i <= ClientDefinitions.pvpLimitQty; i++)
                    {
                        head += $"PVP{i}{ClientDefinitions.delimeter}Origin{i}{ClientDefinitions.delimeter}Currency{i}{ClientDefinitions.delimeter}" +
                                $"Measure{i}{ClientDefinitions.delimeter}MeasurePVP{i}{ClientDefinitions.delimeter}MeasureCurrency{i}{ClientDefinitions.delimeter}" +
                                $"Measure{i}_2{ClientDefinitions.delimeter}MeasurePVP{i}_2{ClientDefinitions.delimeter}MeasureCurrency{i}_2{ClientDefinitions.delimeter}";
                    }
                }
            }
        }

        public static void LoadSizeRangeAll(
           ref string line,
           ref string head,
           List<Itemdata> items,
           bool hasHead,
           ILogService log,
           string sizeId,
           string[] notIncludeFields)
        {
            if(notIncludeFields.Contains(ClientDefinitions.sizeRangeAllField))
                return;

            var values = new List<string>();

            if(items != null)
            {
                foreach(var item in items)
                {
                    if(item?.SizeDescriptions == null) continue;

                    foreach(var desc in item.SizeDescriptions)
                    {
                        if(desc?.SizeId == sizeId && !string.IsNullOrEmpty(desc.SizeDescription))
                            values.Add(desc.SizeDescription);
                    }
                }
            }

            var sizes = string.Empty;

            if(values.Count > 0)
            {
                sizes = values.Merge("/");
            }

            if(!hasHead) head += string.Concat(ClientDefinitions.sizeRangeAllField, ClientDefinitions.delimeter);
            line += string.Concat(sizes, ClientDefinitions.delimeter);
        }
        
        public static void LoadPrintDescriptionGender(
           ref string line,
           ref string head,
           StyleColor styleColor,
           bool hasHead,
           ILogService log)
        {
            if(styleColor is null) throw new ArgumentNullException(nameof(styleColor));
#if DEBUG
#else
            if(log is null) throw new ArgumentNullException(nameof(log));
#endif
            

            var printDescription = FieldsHelper.GetPrintDescriptionGender(styleColor, log);
            
                if(!hasHead)
                {
                    head += string.Concat(ClientDefinitions.printDescriptioGenderField, ClientDefinitions.delimeter);
                }

                line += string.Concat(printDescription, ClientDefinitions.delimeter);
        }

        public static void LoadSizeWithColumCountries(
           ref string line,
           ref string head,
           Itemdata item,
           bool hasHead,
           ILogService log,
           string[] notIncludeFields)
        {

            var countryIds = new List<string>() { "CN", "ES", "GB", "IT", "MX", "US" };


            if(item != null)
            {
                var values = new Dictionary<string, string>();
                if(item?.SizeDescriptions == null)
                {
                    foreach(var country in countryIds)
                    {
                        if(!hasHead) head += string.Concat(country, ClientDefinitions.delimeter);
                        line += string.Concat(string.Empty, ClientDefinitions.delimeter);
                    }
                    return;
                }

                foreach(var desc in item.SizeDescriptions)
                {
                    if(countryIds.Contains(desc.Country))
                        values.Add(desc.Country, desc.SizeDescription);
                }

                foreach(var country in countryIds)
                {
                    if(values.TryGetValue(country, out var descrip))
                    {
                        if(!hasHead) head += string.Concat(country, ClientDefinitions.delimeter);
                        line += string.Concat(descrip, ClientDefinitions.delimeter);
                    }
                    else
                    {
                        if(!hasHead) head += string.Concat(country, ClientDefinitions.delimeter);
                        line += string.Concat(string.Empty, ClientDefinitions.delimeter);
                    }

                }
            }
        }

        private static string SortSizes(IEnumerable<string> values)
        {
            var list = values?.Where(x => !string.IsNullOrEmpty(x)).ToList() ?? new List<string>();
            if(list.Count == 0)
                return string.Empty;

            bool allAreNumbers = list.All(x => Regex.IsMatch(x, @"^\d+(-\d+)?$"));

            if(allAreNumbers)
                return SortSizeNumbers(list);

            return SortSizeAlphas(list);

        }

        private static string SortSizeNumbers(IEnumerable<string> values)
        {
            if(values == null)
                return string.Empty;

            var list = values.ToArray();
            if(list.All(v => Regex.IsMatch(v, @"^\d+(-\d+)?$")))
            {
                return list
                    .OrderBy(v => int.Parse(Regex.Match(v, @"^\d+").Value))
                    .Merge("/");
            }

            return list.OrderBy(v => v).Merge("/");
        }

        private static string SortSizeAlphas(IEnumerable<string> parts)
        {

            string[] alphaOrder = { "XXS", "XS", "S", "M", "L", "XL", "XXL" };

            return parts
                .OrderBy(x =>
                {
                    int index = Array.IndexOf(alphaOrder, x);
                    return index >= 0 ? index : int.MaxValue;
                })
                .Merge("/");
        }

        public static string LoadSizeIdByLabelId(string labelId)
        {
            var labelsWithRule = LoadJsonConfig.GetLabelsWithSizeDescriptionRule();

            foreach(var label in labelsWithRule)
            {
                if(labelId.Contains(label.LabelId))
                    return label.SizeId;
            }

            return "001";
        }
    }
}
