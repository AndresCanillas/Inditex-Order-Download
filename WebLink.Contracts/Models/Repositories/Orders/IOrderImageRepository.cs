using System.Collections.Generic;
using System.IO;
using WebLink.Contracts.Models.Repositories;

namespace WebLink.Contracts.Models
{
    public interface IOrderImageRepository
    {
        IEnumerable<ImageOrderMetadata> GetListByOrderID(int orderID);
        void UpdateImageMetadata(ImageOrderMetadata meta);
        ImageOrderMetadata UploadImage(int orderID, string fileName, Stream content);
        Stream GetImage(int orderID, string fileName);
        Stream GetThumbnail(int orderID, string fileName);
        void DeleteImage(int orderID, string fileName);
    }
}
