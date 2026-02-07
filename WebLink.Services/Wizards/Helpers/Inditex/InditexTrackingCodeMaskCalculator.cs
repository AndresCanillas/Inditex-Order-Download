using Newtonsoft.Json.Linq;
using Service.Contracts;
using Service.Contracts.Database;
using Service.Contracts.Documents;
using Services.Core;
using System;
using System.Linq;
using System.Text.RegularExpressions;

// +--------------------------------------------------------------------------+
// | WARNING: duplicated code between WebLinkService in NetCore 2.1 
// | and InditexHelperLib inner Allplugin Solution in .net 4.74
// | Create a solution with multiple projects  configuration 
// | like ServiceContracts
// | Video: https://indetgroup-my.sharepoint.com/:v:/p/rafael_guerrero/EYDEn6R0GKJKmJ6vmbWmssQBH74PKINIr6igfs93fDUxaA?e=W9sZXA&nav=eyJyZWZlcnJhbEluZm8iOnsicmVmZXJyYWxBcHAiOiJTdHJlYW1XZWJBcHAiLCJyZWZlcnJhbFZpZXciOiJTaGFyZURpYWxvZy1MaW5rIiwicmVmZXJyYWxBcHBQbGF0Zm9ybSI6IldlYiIsInJlZmVycmFsTW9kZSI6InZpZXcifX0%3D
// +--------------------------------------------------------------------------+

namespace Inditex.HelperLib
{
    public class InditexTrackingCodeMaskCalculator : IInditexTrackingCodeMaskCalculator, IConfigurable<InditexTrackingCodeMaskConfiguration>
    {
        public InditexTrackingCodeMaskConfiguration Config;

        public InditexTrackingCodeMaskCalculator()
        {
            
            Config = new InditexTrackingCodeMaskConfiguration()
            {
                CatalogName = "TrackingCodeConfigLookup",
                DefaultMask = "[Barcode]", // PrintCentral Wizard, DefaultMask refer to VariableDataField
                TargetProductField = "TrackingCode" // PrintCentral Wizard, TargetProductField refer to VariableDataField
            };

        }

        /*
         the mask value configure in database inner TrackingCodeConfigLookup Catalog will be replaced with the values found in ImportedData
         */
        public string GetTrackingCodeValue(ImportedData data, int projectID, string articleCode, IConnectionManager connectionManager)
        {
            var mask = this.GetMask(projectID, articleCode, connectionManager);

            return ProcessMask(mask, data);
        }

        /*
         the mask value configure in database inner TrackingCodeConfigLookup Catalog will be replaced with the values found in VariableDataRow
         */
        public string GetTrackingCodeValue(JObject data, int projectID, string articleCode, IConnectionManager connectionManager)
        {
            var mask = this.GetMask(projectID, articleCode, connectionManager);

            return ProcessMask(mask, data);
        }

        /*
         if mask is empty default value is BARCODE for RFID labels or empty for no RFID labels
         */
        public string GetMask(int projectID, string articleCode, IConnectionManager connectionManager)
        {
            CatalogInfo catalog = null;
            JObject article = null;

            using(var conn = connectionManager.OpenDB())
            {

                article = conn.SelectOneToJson(@"SELECT 
                        a.ArticleCode,
                        CASE 
                            WHEN(l.ID IS NULL) THEN 0
                            ELSE l.EncodeRFID 
                        END as EncodeRFID
                        FROM Articles a
                        LEFT JOIN Labels l on a.LabelID = l.ID 
                        WHERE a.ProjectID = @projectID
                        AND a.ArticleCode = @articleCode", projectID, articleCode);


                // no rfid
                if(article.GetValue<int>("EncodeRFID", 0) == 0)
                {
                    return string.Empty;
                }

                catalog = conn.SelectOne<CatalogInfo>(@"SELECT CatalogID, Name 
                        FROM Catalogs
                        WHERE ProjectID = @projectID
                        AND Name = @catalogName", projectID, Config.CatalogName);

                if(catalog == null)
                {
                    return Config.DefaultMask;
                }
            }

            using(var conn = connectionManager.OpenDB("CatalogDB"))
            {
                var x = conn.Select<TrackingCodeMask>($@"
                            SELECT TOP 1
                            CASE 
                                WHEN ( Mask IS NULL OR LEN(Mask) < 1)  THEN @defaultMask
                                ELSE Mask
                            END as Mask
                            FROM {catalog.Name}_{catalog.CatalogID}
                            WHERE ArticleCode = @articleCode", Config.DefaultMask, articleCode);

                if(x.Count > 0)
                {
                    return x.FirstOrDefault().Mask;
                }
            }

            return Config.DefaultMask;

        }



        /// <summary>
        /// process mask
        /// keys values in mask will be  using like column mapping names to looking inner document data in current index,
        /// 
        /// "[Barcode]-00-[Size]-XY|[]|" -> Barcode, Size
        /// empty values are ignored or not detected
        /// nested values are not supported "[Barcode[nested]]-00-[Size]-XY|[]|"
        ///
        /// </summary>
        /// <param name="mask">[Barcode]-00-[Size]</param>
        /// <param name="data">Document Service Imported Data Response </param>
        /// <returns></returns>
        public string ProcessMask(string mask, ImportedData data)
        {
            if(String.IsNullOrWhiteSpace(mask))
                return "";

            var trackingCode = mask;
            //var pattern = @"\[(.*?)\]";// un expected result
            var pattern = "(?<=\\[)([^\\]]+)(?=\\])"; // get values between '[' and ']',  ignore nested values "[]"

            Regex regex = new Regex(pattern);
            MatchCollection matches = regex.Matches(mask);

            foreach(Match match in matches)
            {
                trackingCode = trackingCode.Replace($"[{match.Value}]", data.GetValue(match.Value).ToString());
            }


            return trackingCode;

        }

        public string ProcessMask(string mask, JObject variableDataRow)
        {
            if(String.IsNullOrWhiteSpace(mask))
                return "";

            var trackingCode = mask;
            //var pattern = @"\[(.*?)\]";// un expected result
            var pattern = "(?<=\\[)([^\\]]+)(?=\\])"; // get values between '[' and ']',  ignore nested values "[]"

            Regex regex = new Regex(pattern);
            MatchCollection matches = regex.Matches(mask);

            foreach(Match match in matches)
            {
                trackingCode = trackingCode.Replace($"[{match.Value}]", variableDataRow.GetValue(match.Value).ToString());
            }


            return trackingCode;

        }

        public InditexTrackingCodeMaskConfiguration GetConfiguration()
        {
            return Config;
        }

        public void SetConfiguration(InditexTrackingCodeMaskConfiguration config)
        {
            this.Config = config;
        }
    }

    public class TrackingCodeMask
    {
        public string ArticleCode;

        public string Mask;

    }

    public class InditexTrackingCodeMaskConfiguration
    {
        public string CatalogName;

        public string DefaultMask;

        public string TargetProductField;
    }
}
