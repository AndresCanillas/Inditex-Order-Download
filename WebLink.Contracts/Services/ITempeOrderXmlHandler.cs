using System.IO;
using System.Threading.Tasks;

namespace WebLink.Contracts.Services
{
    public interface ITempeOrderXmlHandler
    {
        Task<TempeOrderData> ProcessingFile(Stream stream, ManualEntryOrderFileDTO manualEntryOrderFileDTO);
        Task<string> GetInditexAPIData(APIInditexDataRq rq); 
    }
}