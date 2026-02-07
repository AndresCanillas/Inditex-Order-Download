using System.Collections.Generic;
using System.IO;

namespace WebLink.Contracts.Models
{
    public interface IProjectImageRepository
    {
        IEnumerable<ImageMetadata> GetListByProjectID(int projectID);
        Stream GetImage(int projectID, string fileName);
        ImageMetadata GetImageMetadata(int projectID, string fileName);
        void UpdateImageMetadata(ImageMetadata meta);
        ImageMetadata UploadImage(int projectID, string fileName, Stream content);
        Stream GetThumbnail(int projectID, string fileName);
        void DeleteImage(int projectID, string fileName);

    }
}
