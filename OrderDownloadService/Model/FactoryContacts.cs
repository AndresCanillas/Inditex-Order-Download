using System.Collections.Generic;

namespace OrderDonwLoadService.Model
{
    public class FactoryContacts
    {
        public List<FactoryContact> Factories { get; set; }
    }

    public class FactoryContact
    {
        public string Name { get; set; }
        public string VendorId { get; set; }
        public string SendTo { get; set; }
        public string CopyTo { get; set; }
    }
}
