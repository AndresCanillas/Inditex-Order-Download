using System;

namespace OrderDownloadWebApi.Models
{

    public interface IUser
    {
        int ID { get; set; }
        string UserId { get; set; }
        string Name { get; set; }
        string FirstName { get; set; }
        string LastName { get; set; }
        string Email { get; set; }
        string PhoneNumber { get; set; }
        string Language { get; set; }
        string PwdHash { get; set; }
        string Roles { get; set; }
        DateTime CreatedDate { get; set; }
        DateTime UpdatedDate { get; set; }
    }

    public class User : IUser
    {
        public int ID { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Language { get; set; }
        public string PwdHash { get; set; }
        public string Roles { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        internal bool IsNew;
    }
}
