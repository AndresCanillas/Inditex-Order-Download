using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebLink.Contracts;

namespace WebLink.Contracts.Models
{
    public class IdentityDB : IdentityDbContext<AppUser, AppRole, string>
    {
        public IdentityDB(DbContextOptions<IdentityDB> options)
            : base(options)
        {
        }
		public DbSet<ResetToken> ResetTokens { get; set; }
	}

	public class AppUser : IdentityUser, IAppUser
    {
        public AppUser() { }
        public AppUser(string name) : base(name) { }

		public int? CompanyID { get; set; }
		public int? SelectedCompanyID { get; set; }
        public int? SelectedBrandID { get; set; }
        public int? SelectedProjectID { get; set; }
        public int? LocationID { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string Language { get; set; }
		public bool ShowAsUser { get; set; }
		public DbSet<AppUserCompany> Companies { get; set; }
        public bool LockoutEnabled {  get; set; }   
        public DateTimeOffset? LockoutEnd { get; set; }
	}

	public class ResetToken: IResetToken
	{
		public string ID { get; set; }
		public string UserName { get; set; }
		public DateTime ValidUntil { get; set; }
	}

    public class AppRole : IdentityRole, IAppRole
    {


    }

    public class AppUserRole : IdentityUserRole<string>
    {
        public virtual AppUser User { get; set; }
        public virtual AppRole Role { get; set; }
    }

    public class AppUserCompany
    {
        public int ID { get; set; }
        public int CompanyID { get; set; }
    }
}
