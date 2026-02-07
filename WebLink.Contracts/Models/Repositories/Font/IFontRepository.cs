using System.Collections.Generic;
using System.IO;

namespace WebLink.Contracts.Models
{
    public interface IFontRepository
    {
        IEnumerable<string> GetList();
        Dictionary<string, string> GetUpdatedDate();
        string UploadFont(string fileName, Stream content);
        Stream GetFont(string fileName);
        void DeleteFont(string fileName);
    }
}
