using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.Authentication
{
	public class SystemIdentity : ClaimsPrincipal, IPrincipal, IIdentity
	{
		private List<Claim> claims = new List<Claim>();

		public SystemIdentity()
		{
			claims.Add(new Claim("CompanyID", "1"));
			claims.Add(new Claim("LocationID", "1"));
		}

		public override IEnumerable<Claim> FindAll(string type)
		{
			return claims.FindAll(p => p.Type == type);
		}

		public override Claim FindFirst(string type)
		{
			return claims.FirstOrDefault(p => p.Type == type);
		}

		public override bool IsInRole(string role)
		{
			return true;
		}

		public override IIdentity Identity
		{
			get
			{
				return this;
			}
		}

		public override IEnumerable<Claim> Claims
		{
			get
			{
				return claims;
			}
		}

		public string AuthenticationType
		{
			get
			{
				return "none";
			}
		}

		public bool IsAuthenticated
		{
			get
			{
				return true;
			}
		}

		public string Name
		{
			get
			{
				return "SYSTEM";
			}
		}
	}
}
