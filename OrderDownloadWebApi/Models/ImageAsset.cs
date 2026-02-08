using System;
using Service.Contracts.OrderImages;

namespace OrderDownloadWebApi.Models
{
    public class ImageAsset
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public string Hash { get; set; }
        public string ContentType { get; set; }
        public byte[] Content { get; set; }
        public ImageAssetStatus Status { get; set; }
        public bool IsLatest { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
