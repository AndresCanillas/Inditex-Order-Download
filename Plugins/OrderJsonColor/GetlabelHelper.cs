using StructureMangoOrderFileColor;
using System.Linq;

namespace JsonColor
{
    using Service.Contracts.Database;
    using Service.Contracts.PrintCentral;
    using Services.Core;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.Data.SqlTypes;

    public static class GetlabelHelper
    {
        //LÍNEA                       | REFERENCIA  | SET COMPOS  			| ADHESIVA  
        //-------------------------------------------------------------------------------
        //WOMAN                      | GI000PRO   | CB000R82   |			    | 041 y 151
        //MAN                        | GI000PRO   | CB000R82   |			    | 041 y 151
        //KIDS                       | GI000PRO   | CB000C82   |			    | K41 y KS1
        //HOME                       | GI000PRO   | CB000C82   |			    | K41 y KS1
        //KIDS/BABY/NEWBORN/HOME     | GI000RIM   | GI000IR2   | GI000YI3       | K41 y KS1
        //WOMAN                      | GI000RIM   | GI000IR2   | GI000RI3       | 041 y 151
        //MAN                        | GI000RIM   | GI000IR2   | GI000RI3       | 041 y 151
        //HOME                       | GI000HPO   | GI000HI2   | GI000HO3       | K41 y KS1
        //HOME                       | GI000FIL   |            |			    | K41 y KS1
        //WOMAN/MAN                  | GI002NPO   | GI001N82   |			    | 041 y 151
        //WOMAN                      | GI003NTU   | GI001IN2   | GI004CI2       | 041 y 151
        //MAN                        | GI003NTU   | GI001IN2   | GI004CI2       | 041 y 151
        //KIDS/HOME                  | GI003NTU   | GI001IN2   | GI004KI2       | K41 y KS1
        //WOMAN                      | GI000DPO   | DB000D82   |			    | 041 y 151
        //MAN                        | GI000DPO   | DB000D82   |			    | 041 y 151
        //KIDS                       | GI000DPO   | CB000B82   |			    | K41 y KS1
        //HOME                       | GI000DPO   | CB000B82   |			    | K41 y KS1
        //WOMAN/MAN                  | GI000DIM   | GI000ID2   | GI000CD3       | 041 y 151
        //BABY/KIDS/NEWBORN/HOME     | GI000DIM   | GI000ID2   | GI000KD3       | K41 y KS1
        //WOMAN/MAN                  | GI003TCO   | CB001T82   |			    | 041 y 151
        //WOMAN/MAN                  | GI000NTO   | CB001K82   |			    | 041 y 151

        private static readonly Dictionary<string, Dictionary<string, string>> SetComposMap =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                 { "GI000DIM", new Dictionary<string, string>
                    {
                        { "BABY", "GI000ID2,GI000KD3" },
                        { "KIDS", "GI000ID2,GI000KD3" },
                        { "TEEN", "GI000ID2,GI000KD3" },
                        { "HOME", "GI000ID2,GI000KD3" },
                        { "HOME UNISEX", "GI000ID2,GI000KD3" },
                        { "MAN", "GI000ID2,GI000CD3" },
                        { "NEWBORN", "GI000ID2,GI000KD3" },
                        { "WOMAN", "GI000ID2,GI000CD3" }
                    }
                 },
                 { "GI000RIM", new Dictionary<string, string>
                    {
                        { "BABY", "GI000IR2,GI000YI3" },
                        { "KIDS", "GI000IR2,GI000YI3" },
                        { "TEEN", "GI000IR2,GI000YI3" },
                        { "HOME", "GI000IR2,GI000YI3" },
                        { "HOME UNISEX", "GI000IR2,GI000YI3" },
                        { "MAN", "GI000IR2,GI000RI3" },
                        { "NEWBORN", "GI000IR2,GI000YI3" },
                        { "WOMAN", "GI000IR2,GI000RI3" }
                    }
                 },
                 { "GI000DPO", new Dictionary<string, string>
                    {
                        { "KIDS", "CB000B82" },
                        { "TEEN", "CB000B82" },
                        { "HOME", "CB000B82" },
                        { "HOME UNISEX", "CB000B82" },
                        { "MAN", "DB000D82" },
                        { "WOMAN", "DB000D82" }
                    }
                 },
                { "GI000PRO", new Dictionary<string, string>
                    {
                        { "KIDS", "CB000C82" },
                        { "TEEN", "CB000C82" },
                        { "HOME", "CB000C82" },
                        { "HOME UNISEX", "CB000C82" },
                        { "MAN", "CB000R82" },
                        { "WOMAN", "CB000R82" }
                    }
                },
                { "GI003NTU", new Dictionary<string, string>
                    {
                        { "KIDS", "GI001IN2,GI004KI2" },
                        { "TEEN", "GI001IN2,GI004KI2" },
                        { "HOME", "GI001IN2,GI004KI2" },
                        { "HOME UNISEX", "GI001IN2,GI004KI2" },
                        { "MAN", "GI001IN2,GI004CI2" },
                        { "WOMAN", "GI001IN2,GI004CI2" }
                    }
                },
                { "GI000HPO", new Dictionary<string, string>
                    {
                        { "HOME", "GI000HI2,GI000HO3" },
                        { "HOME UNISEX", "GI000HI2,GI000HO3" },
                    }
                },
                { "GI002NPO", new Dictionary<string, string>
                    {
                        { "WOMAN", "GI001N82" },
                        { "MAN", "GI001N82" }
                    }
                },
                { "GI003TCO", new Dictionary<string, string>
                    {
                        { "WOMAN", "CB001T82" },
                        { "MAN", "CB001T82" }
                    }
                },
                { "GI000NTO", new Dictionary<string, string>
                    {
                        { "WOMAN", "CB001K82" },
                        { "MAN", "CB001K82" }
                    }
                },
                { "GI000SPO", new Dictionary<string, string>
                    {
                        { "HOME", "CB000S82" },
                        { "HOME UNISEX", "CB000S82" }
                    }
                },
                { "GI000HRO", new Dictionary<string, string>
                    {
                        { "HOME", "GI000HR2,GI000HR3" },
                        { "HOME UNISEX", "GI000HR2,GI000HR3" }
                    }
                }
            };






        public static string GetSetComposLabel(string labelId, string line)
        {

            if(string.IsNullOrEmpty(labelId) || string.IsNullOrEmpty(line))
                return null;


            foreach(var reference in SetComposMap)
            {
                if(labelId.Contains(reference.Key))
                {
                    if(reference.Value.TryGetValue(line, out string setCompos))
                    {
                        return setCompos;
                    }
                    break;
                }
            }

            return null;
        }



        public static void CheckLabelType(Labeldata label, MangoOrderData orderData, string originalLabelId = null)
        {
            string[] labelsWithDiferentLayout = LoadJsonConfig.GetLabelsWithDiferentLayout();

            var baseLabelId = originalLabelId ?? label.LabelID;

            if(!labelsWithDiferentLayout.Contains(baseLabelId)) return;

            label.LabelID = string.Concat(label.LabelID, orderData.LabelOrder.TypePO);
        }

        public static void CheckLabePiggyback(Labeldata label, Itemdata item, List<PVP> pvps, string typePo = null, string originalLabelId = null)
        {

            string[] labelsWithPiggyback = LoadJsonConfig.GetLabelsWithPiggyback();

            var baseLabelId = originalLabelId ?? label.LabelID;

            if(!labelsWithPiggyback.Contains(baseLabelId)) return;

            var typeRules = LoadJsonConfig.GetLabelsWithPiggybackTypeRules();

            if(typeRules.TryGetValue(baseLabelId, out var allowedTypes))
            {
                if(string.IsNullOrWhiteSpace(typePo) || !allowedTypes.Contains(typePo, StringComparer.OrdinalIgnoreCase))
                {
                    label.LabelID = string.Concat(label.LabelID, "0");
                    return;
                }
            }

            label.LabelID = AddPiggyNumber(label.LabelID, item, pvps);
        }

        private static string AddPiggyNumber(string labelID, Itemdata item, List<PVP> pvps)
        {
            var PVP_ES = "";
            var PVP_EU = "";
            if(pvps == null)
            {
                PVP_ES = item.PVP_ES;
                PVP_EU = item.PVP_EU;
            }
            else
            {

                if(!pvps.Any(p => p.PVP_ES != null) || !pvps.Any(p => p.PVP_EU != null))
                {
                    throw new InvalidOperationException("PVP list must contain at least one PVP with a non-null PVP_ES and PVP_EU value.");
                }

                PVP_ES = pvps.First(p => p.PVP_ES != null).PVP_ES;
                PVP_EU = pvps.First(p => p.PVP_EU != null).PVP_EU;
            }


            if(PVP_ES != PVP_EU)
            {
                return string.Concat(labelID, "2");
            }

            if(item.stocksegment.Substring(3, 3) == "EMG" || item.stocksegment.Substring(6, 3) == "MOL")
            {
                return string.Concat(labelID, "0");
            }
            return string.Concat(labelID, "1");
        }

        public static string GetPackLabel(string sizePackQty, string line, string famCode)
        {
            //All orders will include an individual sticker(1 unit), regardless of whether it comes in a packQTY of  2.
            //  FAM CODE    DESCRIPTION
            //------------------------------------
            //  601         BOLSO
            //  602         BOLSA
            //  603         FUNDA ORDENADOR
            //  605         MOCHILA
            //  606         NECESER HOMBRE
            //  607         MOCHILA HOMBRE
            //  608         NECESER
            //  625         SOMBRERO
            //  650         CINTURON
            //  651         PARAGUAS
            //  656         RELOJ
            //  660         PARAGUAS HOMBRE
            //  662         SOMBRERO HOMBRE
            //  666         CINTURON HOMBRE
            //  669         BOLSO HOMBRE
            //  750         MALETAS
            //  751         FUNDAS

            var notIncluedeAdhesiva = new string[]
            { "601", "602", "603", "605", "606", "607", "608", "625", "650", "651", "656", "660", "662", "666", "669", "750", "751" };



            int packQty = int.Parse(sizePackQty);

            if(packQty < 1)
                throw new InvalidOperationException($"Error sizePackQty must be 1 or above 1 and it has a value of {sizePackQty}");

            string labelIdRetun = "CB000K41";
            if(notIncluedeAdhesiva.Contains(famCode))
            {

                switch(line)
                {
                    case "MAN":
                    case "WOMAN":
                        labelIdRetun = packQty > 1 ? "CB000151" : "CB000041";
                        break;
                    default:
                        labelIdRetun = packQty > 1 ? "CB000KS1" : "CB000K41";
                        break;
                }
            }
            else
            {
                switch(line)
                {

                    case "MAN":
                    case "WOMAN":
                        labelIdRetun = packQty > 1 ? "CB000041,CB000151" : "CB000041";
                        break;
                    default:
                        labelIdRetun = packQty > 1 ? "CB000K41,CB000KS1" : "CB000K41";
                        break;


                }
            }
            return labelIdRetun;

        }

        public static string GetSpecialRuleLabel(string labelID, MangoOrderData orderData, Itemdata item)
        {
            // Añadir esta etiqueta PVP02MNG siempre y cuando recibamos la PVZ00WXG solo para pedidos Direct Sourcing.
            if(labelID.Contains("PVZ00WXG") && orderData.LabelOrder.TypePO == "ZDS0")
            {
                return "PVP02MNG";
            }
            if(labelID.Contains("PVZ00W2G") && orderData.LabelOrder.TypePO == "ZDS0")
            {
                return "PVP03MNG ";
            }
            return null;
        }

        public static IsCompoDto GetLabelCompositionInfo(
         int projectId,
         string articleCode,
         IConnectionManager connMng,
         ILogService log)
        {
#if DEBUG
            
            return new IsCompoDto
            {
                IncludeComposition = true,
                IncludeCareInstructions = false,
                LabelId = articleCode 
            };
#else
            using (var db = connMng.OpenDB("PrintDB"))
            {
                var query = @"
                    SELECT 
                        lb.IncludeComposition,
                        lb.IncludeCareInstructions,
                        a.ArticleCode AS LabelId
                    FROM Labels lb
                    INNER JOIN Articles a 
                        ON lb.ProjectID = a.ProjectID
                       AND lb.id  = a.LabelID  
                    WHERE a.ArticleCode = @articleCode
                      AND a.ProjectID   = @projectId;";

                
                var dto = db.SelectOne<IsCompoDto>(query, 
                    new SqlParameter("@articleCode", SqlDbType.NVarChar) { Value = articleCode },
                    new SqlParameter("@projectId", SqlDbType.Int) { Value = projectId }
                    );

                if (dto == null)
                {
                    log?.LogWarning($"Not found Label to ArticleCode='{articleCode}' into ProjectID={projectId}.");
                    return new IsCompoDto
                    {
                        IncludeComposition = false,
                        IncludeCareInstructions = false,
                        LabelId = articleCode
                    };
                }

                dto.LabelId = articleCode; // si te interesa guardar el contexto de consulta
                return dto;
            }
#endif
        }


    }
    public class IsCompoDto
    {
        public bool IncludeComposition { get; set; }
        public bool IncludeCareInstructions { get; set; }
        public string LabelId { get;  set; }
    }
}
