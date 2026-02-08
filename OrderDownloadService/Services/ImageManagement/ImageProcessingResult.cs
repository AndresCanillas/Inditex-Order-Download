using System.Collections.Generic;

namespace OrderDonwLoadService.Services.ImageManagement
{
    public class ImageProcessingResult
    {
        public bool RequiresApproval { get; set; }
        public List<string> NewOrUpdatedUrls { get; } = new List<string>();
    }
}
