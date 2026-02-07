using Service.Contracts;
using System.Threading.Tasks;
using WebLink.Contracts.Models;

namespace WebLink.Contracts.Services
{
    public interface IPDFZaraExtractorService
    {
        Task<OperationResult> SendOrder(PDFZaraExtractor entry);
        Task<CompositionDefinition> GetCompositionDefinitionFromCompositionJSON(int projectid, int[] orderid); 
    }
}