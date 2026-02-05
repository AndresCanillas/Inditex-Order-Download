using Service.Contracts.Database;
using Services.Core;
using StructureMangoOrderFileColor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace JsonColor
{
    public static class JsonToTextConverter
    {
        private static int ProjectID = 0;
        private static IConnectionManager ConnMng;
        public static string LoadData(MangoOrderData orderData, ILogService log = null, IConnectionManager connMng = null, int projectID = 0)
        {
            string filedata = "";
            bool hasHead = false;
            ProjectID = projectID;
            ConnMng = connMng;

            foreach(var styleColor in orderData.StyleColor)
            {

                string dataLabel = "";
                string line = "";
                string head = "";
                var prepackData = new Prepackdata()
                {
                    MangoPrePackCode = "",
                    PrepackBarCode = "",
                    PrepackColor = "",
                    PrepackQty = "",
                    PrepackTotalQty = ""

                };
                if(orderData.StyleColor.Count() == 0 || orderData.StyleColor == null)
                    throw new InvalidOperationException($"Error: and the order ({orderData.LabelOrder.LabelOrderId}) the StyleColor is empty ");


                filedata += CreateLineByLabel(styleColor, orderData, ref hasHead, ref dataLabel, ref line, ref head, prepackData, log, connMng);
                if(ClientDefinitions.isOnlyColor)
                    return filedata;
            }
            return filedata;
        }

        private static string CreateLineByLabel(StyleColor styleColor, MangoOrderData
            orderData, ref bool hasHead, ref string dataLabel,
            ref string line, ref string head, Prepackdata prepackData, ILogService log, Service.Contracts.Database.IConnectionManager connMng)
        {
            if(styleColor.LabelData.Count() == 0 || styleColor.LabelData == null)
                throw new InvalidOperationException($"Error: and the order ({orderData.LabelOrder.LabelOrderId}) the label is empty");

            foreach(var label in styleColor.LabelData)
            {


                if(styleColor.ItemData.Count() == 0 || styleColor.ItemData == null)
                    throw new InvalidOperationException($"Error: and the order ({orderData.LabelOrder.LabelOrderId}) ");

                var originalLabelId = label.LabelID;

                foreach(var item in styleColor.ItemData)
                {

                    GetlabelHelper.CheckLabePiggyback(label, item, styleColor.Pvps, orderData.LabelOrder?.TypePO, originalLabelId);

                    GetlabelHelper.CheckLabelType(label, orderData, originalLabelId);

                    CreateLabelData(ref hasHead,
                        ref dataLabel,
                        out line,
                        out head,
                        orderData,
                        label,
                        item,
                        prepackData,
                        styleColor,
                        log,
                        GetlabelHelper.
                            GetLabelCompositionInfo(
                            ProjectID,
                            label.LabelID,
                            ConnMng, log
                    ));

                    CreateSetCompos(ref hasHead,
                        ref dataLabel,
                        ref line,
                        ref head,
                        orderData,
                        label,
                        prepackData,
                        styleColor,
                        log,
                        item);

                    CreateSpecialRuleLables(ref hasHead,
                        ref dataLabel,
                        ref line,
                        ref head,
                        orderData,
                        label,
                        prepackData,
                        styleColor,
                        log,
                        item);
                    label.LabelID = originalLabelId;
                }
                label.LabelID = originalLabelId;
            }
            if(styleColor.LabelData.Exists(l => l.LabelID.Contains("ADHEDIST")))
            {
                CreateSetPackLabel(
                    ref hasHead,
                    ref dataLabel,
                    ref line,
                    ref head,
                    orderData,
                    new Labeldata
                    {
                        LabelID = "ADHEDIST",
                        Variable = styleColor.LabelData.FirstOrDefault() == null ? "no found" : styleColor.LabelData.First().Variable,
                        Vendor = styleColor.LabelData.FirstOrDefault() == null ? "no found" : styleColor.LabelData.First().Vendor
                    },
                    prepackData,
                    styleColor,
                    log);
            }
            if(orderData.PrepackData != null)
            {
                CreateLabelData(ref hasHead,
                            ref dataLabel,
                            out line,
                            out head,
                            orderData,
                            new Labeldata
                            {
                                LabelID = "CB000MSZ",
                                Variable = styleColor.LabelData.FirstOrDefault() == null ? "no found" : styleColor.LabelData.First().Variable,
                                Vendor = styleColor.LabelData.FirstOrDefault() == null ? "no found" : styleColor.LabelData.First().Vendor
                            },
                            crearItemDataEmpty(),
                            prepackData,
                            styleColor,
                            log,
                            new IsCompoDto { IncludeComposition = false, IncludeCareInstructions = false, LabelId = "CB000MSZ" }
                            );
            }

            return dataLabel;
        }


        private static void CreateSetCompos(ref bool hasHead, ref string dataLabel, ref string line,
            ref string head, MangoOrderData orderData, Labeldata label, Prepackdata prepackdata, StyleColor styleColor
            , ILogService log, Itemdata item)
        {

            var labelIds = GetlabelHelper.GetSetComposLabel(label.LabelID, styleColor.Line);

            if(labelIds == null) return;




            foreach(var labelId in labelIds.Split(',').ToList())
            {
                if(!string.IsNullOrEmpty(labelId))
                {

                    CreateLabelData(ref hasHead,
                     ref dataLabel,
                     out line,
                     out head,
                     orderData,
                     new Labeldata
                     {
                         LabelID = labelId,
                         Variable = label.Variable,
                         Vendor = label.Vendor
                     },
                     item,
                     prepackdata,
                     styleColor,
                     log,
                     GetlabelHelper.
                       GetLabelCompositionInfo(
                           ProjectID,
                           labelId,
                           ConnMng, log
                       ));
                }

            }
        }
        private static void CreateSpecialRuleLables(ref bool hasHead, ref string dataLabel, ref string line,
            ref string head, MangoOrderData orderData, Labeldata label, Prepackdata prepackdata, StyleColor styleColor
            , ILogService log, Itemdata item)
        {

            var labelIds = GetlabelHelper.GetSpecialRuleLabel(label.LabelID, orderData, item);


            if(labelIds == null) return;

            foreach(var labelId in labelIds.Split(',').ToList())
            {
                if(!string.IsNullOrEmpty(labelId))
                {
                    CreateLabelData(ref hasHead,
                     ref dataLabel,
                     out line,
                     out head,
                     orderData,
                     new Labeldata
                     {
                         LabelID = labelId,
                         Variable = label.Variable,
                         Vendor = label.Vendor
                     },
                     item,
                     prepackdata,
                     styleColor,
                     log,
                     GetlabelHelper.
                       GetLabelCompositionInfo(
                           ProjectID,
                           labelId,
                           ConnMng, log
                       ));
                }

            }
        }
        private static void CreateSetPackLabel(ref bool hasHead, ref string dataLabel, ref string line,
           ref string head, MangoOrderData orderData, Labeldata label, Prepackdata prepackdata, StyleColor styleColor, ILogService log)
        {
            foreach(var item in styleColor.ItemData)
            {

                var labelPack = GetlabelHelper.GetPackLabel(item.SizePack.SizePackQty, styleColor.Line, styleColor.ProductTypeCodeLegacy);
                if(!string.IsNullOrEmpty(labelPack))
                {
                    foreach(var labelId in labelPack.Split(',').ToList())
                    {
                        if(!string.IsNullOrEmpty(labelId))
                        {
                            CreateLabelData(ref hasHead,
                             ref dataLabel,
                             out line,
                             out head,
                             orderData,
                             new Labeldata
                             {
                                 LabelID = labelId,
                                 Variable = label.Variable,
                                 Vendor = label.Vendor
                             },
                             item,
                             prepackdata,
                             styleColor,
                             log,
                             GetlabelHelper.
                               GetLabelCompositionInfo(
                                   ProjectID,
                                   labelId,
                                   ConnMng, log
                               ));
                        }
                    }
                }

            }
        }
        public static string CreateHeader(PropertyInfo[] properties)
        {
            string header = "";
            if(!(properties is null))
            {
                for(int i = 0; i < properties.Length; i++)
                {
                    var Layer1 = string.Concat(properties[i].Name,

                    CreateHeader(properties[i].GetType().GetProperties()), ";");
                }
            }
            return header;

        }

        private static Itemdata crearItemDataEmpty()
        {
            return
             new Itemdata()
             {
                 COLOR = "",
                 CURRENCY_ES = "",
                 CURRENCY_EU = "",
                 CURRENCY_IN = "",
                 FillingWeight = "",
                 Phase_IN = "",

                 PVP_ES = "",
                 PVP_EU = "",
                 PVP_IN = "",
                 SizeNameCN = "",
                 SizeNameDE = "",
                 SizeNameES = "",
                 SizeNameFR = "",
                 SizeNameIT = "",
                 SizeNameMX = "",
                 SizeNameUK = "",
                 SizeNameUS = "",
                 EAN13 = "",
                 stocksegment = "",
                 itemQty = "",
                 MangoColorCode = "",
                 MangoSAPSizeCode = "",
                 MangoSizeCode = "",
                 Material = "",
                 SizeName = "",
                 SizePack = new Sizepack()
                 {
                     SizeBarCode = "",
                     SizePackQty = "",
                     SizePackType = "",
                     TotalSizePackQty = "",

                 },
                 GarmentMeasurements = new List<Garmentmeasurement>()
                  {
                     new Garmentmeasurement(){
                     Description = "",
                     Dim = "",
                     Measurement = "" }
                 },
             };
        }

        private static void CreateLabelData(ref bool hasHead, ref string dataLabel, out string line,
            out string head, MangoOrderData orderData, Labeldata label, Itemdata item,
            Prepackdata prepackdata, StyleColor styleColor, ILogService log, IsCompoDto includeComposition)
        {
            line = "";
            head = "";
            var itemQty = item.itemQty;
            var EAN13 = item.EAN13;
            var sizeBarCode = item.SizePack.SizeBarCode;
            var mangoColorCode = item.MangoColorCode;

            var packLabels = new string[] { "CB000151", "CB000KS1" };
            if(packLabels.Contains(label.LabelID))
            {
                item.itemQty = item.SizePack.TotalSizePackQty;
                item.EAN13 = item.SizePack.SizeBarCode;

            }

            if(label.LabelID == "CB000MSZ")
            {
                var pack = orderData.PrepackData.FirstOrDefault(p => p.PrepackColor == styleColor.MangoColorCode);
                if(pack == null) return;

                item.itemQty = pack.PrepackTotalQty;
                item.EAN13 = pack.PrepackBarCode;
                item.SizePack.SizeBarCode = pack.PrepackBarCode;
                item.MangoColorCode = pack.PrepackColor;
            }

            if(!label.LabelID.Contains(includeComposition.LabelId))
                throw new InvalidOperationException($"Error: The labels asigned to validate composition is not the same {includeComposition.LabelId} vs {label.LabelID}");
                
            


            if(!label.LabelID.Contains("ADHEDIST"))
            {

                string[] notIncludeFields = { "GarmentMeasurements", "SizePack", "Fabric", "SizeDescriptions" };

                FieldsHelper.LoadFields(ref line, ref head, label, hasHead, log, notIncludeFields);
                FieldsHelper.LoadFields(ref line, ref head, orderData.LabelOrder, hasHead, log, notIncludeFields);
                FieldsHelper.LoadFields(ref line, ref head, orderData.Supplier, hasHead, log, notIncludeFields);

                //Para ajustar el orden de los campos al mapeo actual se mantiene el orden de los campos que existian antes y los nuevo se agreagan al final.
                FieldsHelper.LoadFields(ref line, ref head, styleColor, hasHead, log,
                    new string[]{ "ItemData", "LabelData", "Composition", "CareInstructions", "SizeRange",
                    "StyleID", "MangoColorCode", "Color", "GenericMaterial","Destination","Origin", "Pvps", "MinLegalData", "StyleColorSet"});

                FieldsHelper.LoadFields(ref line, ref head, styleColor.Destination, hasHead, log, notIncludeFields);
                FieldsHelper.LoadFields(ref line, ref head, styleColor.Origin, hasHead, log, notIncludeFields);
                FieldsHelper.LoadFields(ref line, ref head, item, hasHead, log, notIncludeFields);
                FieldsHelper.LoadFields(ref line, ref head, item.SizePack, hasHead, log, notIncludeFields);
                SpecialFiledsHelper.LoadGarmentMeasurements(ref line, ref head,
                     item.GarmentMeasurements, hasHead, orderData.LabelOrder.LabelOrderId, log, notIncludeFields);
                SpecialFiledsHelper.LoadComposition(ref line, ref head, styleColor, hasHead, orderData.LabelOrder.LabelOrderId, log, notIncludeFields, includeComposition.IncludeComposition);
                SpecialFiledsHelper.LoadCareInstructions(ref line, ref head, styleColor, hasHead, orderData.LabelOrder.LabelOrderId, log, notIncludeFields, includeComposition.IncludeCareInstructions);
                SpecialFiledsHelper.LoadFieldsSizeRange(ref line, ref head, styleColor.SizeRange.FirstOrDefault(), hasHead, log, notIncludeFields);


                //Las etiquetas de prepack se cargan de la siguiente manera:
                //Si el prepackData es null, se carga el prepackdata vacio, si no se busca el prepack por el color del estilo.
                //Si no se encuentra el prepack por el color del estilo, se carga el prepackdata vacio.
                //Esto es para que no se rompa el codigo si no se encuentra el prepack por el color del estilo.
                var prepack = orderData.PrepackData == null ?
                    prepackdata :
                    orderData.PrepackData.FirstOrDefault(p => p.PrepackColor == styleColor.MangoColorCode) ?? prepackdata;


                FieldsHelper.LoadFields(ref line, ref head, prepack, hasHead, log, notIncludeFields);

                //Estos son los nuevas campos
                FieldsHelper.LoadFields(ref line, ref head, styleColor, hasHead, log,
                 new string[] { "ItemData", "LabelData", "Composition", "CareInstructions", "SizeRange",
                 "ReferenceID","Line", "Age", "Gender", "Packaging", "GenName", "Generic", "FAMILYID", "FAMILY", "ProductTypeCode",
                 "ProductTypeCodeLegacy", "ProductType", "ProductTypeES", "RFIDMark", "Iconic", "SizeGroupLegay", "Destination","Origin", "Pvps" });

                SpecialFiledsHelper.LoadSizeDescriptions(ref line, ref head, item.SizeDescriptions, hasHead, orderData.LabelOrder.LabelOrderId, log, notIncludeFields);
                SpecialFiledsHelper.LoadSizeRangeAll(ref line, ref head, styleColor.ItemData, hasHead, log, SpecialFiledsHelper.LoadSizeIdByLabelId(label.LabelID), notIncludeFields);
                SpecialFiledsHelper.LoadPvps(ref line, ref head, styleColor.Pvps, hasHead, orderData.LabelOrder.LabelOrderId, log, notIncludeFields);
                SpecialFiledsHelper.LoadSizeWithColumCountries(ref line, ref head, item, hasHead, log, notIncludeFields);
                SpecialFiledsHelper.LoadPrintDescriptionGender(ref line, ref head, styleColor, hasHead, log);
                if(!hasHead)
                {
                    dataLabel = head + "\r\n";
                    hasHead = true;
                }
                dataLabel += line + "\r\n";

                item.itemQty = itemQty;
                item.EAN13 = EAN13;
                item.SizePack.SizeBarCode = sizeBarCode;
                item.MangoColorCode = mangoColorCode;
            }
        }

        public class JsonColorConfig
        {
            public bool IsOnlyColor { get; set; }
        }

    }
}
