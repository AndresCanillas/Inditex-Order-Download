using Service.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using WebLink.Contracts.Models;

namespace WebLink.Contracts
{
    /// <summary>
    /// Orders Business 
    /// </summary>
    public interface IOrderUtilService
    {
        IEnumerable<OrderGroupSelectionDTO> CurrentOrderedLablesGroupBySelection(IEnumerable<OrderGroupSelectionDTO> selection);
        IEnumerable<OrderGroupSelectionDTO> CurrentOrderedLablesGroupBySelectionV2(IEnumerable<OrderGroupSelectionDTO> selection);
        object GetCompositionCatalogBySelection(IEnumerable<OrderGroupSelectionDTO> selection);
        IList<CompositionDefinition> GetComposition(int orderGroupID, bool joinLang = true, IDictionary<CompoCatalogName, IEnumerable<string>> languages = null, string langSepartor = ",");
        CompositionDefinition GetCompositionDetailsForOrder(int orderID);
        void SaveComposition(int projectId, int rowId, string composition, string careInstructions,string symbols = null);// TODO: add symbols
        void SaveComposition(int projectId, int rowId, Dictionary<string, string> composition, string careInstructions, string symbols);
        IProject GetProjectById(int projectId);
        CompositionDefinition SaveCompositionDefinition(CompositionDefinition composition);
        Dictionary<string,string> GetCompositionData(int project, int rowId);
        
        Task SendToPool(int projectID, string fileName, Stream fileContent);
        Task SaveOrderPoolList(int projectid, List<OrderPool> orderPools);
    }
}
