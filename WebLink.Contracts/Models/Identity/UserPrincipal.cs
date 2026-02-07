using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;

namespace WebLink.Contracts.Models
{
	public class UserIdentity : IIdentity
	{
		public UserIdentity(AppUser user, List<string> roles)
		{
			Id = user.Id;
			UserData = user;
			Name = user.UserName;
			IsAuthenticated = true;
			Roles = roles;
		}

		public string Id { get; set; }
		public string Name { get; set; }
		public string AuthenticationType { get => "Basic"; }
		public bool IsAuthenticated { get; set; }
		public List<string> Roles { get; set; }
		public AppUser UserData { get; set; }
	}


	public class UserPrincipal : ClaimsPrincipal
	{
		private readonly UserIdentity user;
		private ClaimsIdentity identity;

		public UserPrincipal(UserIdentity user)
		{
			identity = new ClaimsIdentity(user);
			this.user = user;
			this.AddIdentity(identity);
			foreach (var role in user.Roles)
				identity.AddClaim(new Claim(ClaimTypes.Role, role));
			identity.AddClaim(new Claim("Language", user.UserData.Language ?? "en-US"));
		}

		public UserIdentity User { get => user; }

		public override IIdentity Identity { get => identity; }

		public override bool IsInRole(string role)
		{
			return user.Roles.Contains(role);
		}
	}
}
