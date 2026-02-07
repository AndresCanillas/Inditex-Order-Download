using Newtonsoft.Json.Linq;
using Service.Contracts.Database;
using Service.Contracts.Documents;
using Services.Core;

public interface IInditexTrackingCodeMaskCalculator
{
    string GetMask(int projectID, string articleCode, IConnectionManager connectionManager);
    string GetTrackingCodeValue(ImportedData data, int projectID, string articleCode, IConnectionManager connectionManager);
    string GetTrackingCodeValue(JObject data, int projectID, string articleCode, IConnectionManager connectionManager);
    string ProcessMask(string mask, ImportedData data);
    string ProcessMask(string mask, JObject variableDataRow);
}