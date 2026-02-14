using System.Collections.Generic;

namespace OrderDonwLoadService.Model
{
    public class ApiCredentials
    {
        public List<Credential> Credentials { get; set; }


    }
    public class Credential
    {
        public string Name { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
    }
}

