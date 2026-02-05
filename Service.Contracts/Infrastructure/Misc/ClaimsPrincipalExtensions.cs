using System;
using System.Security.Claims;

namespace Service.Contracts
{
	public static class ClaimsPrincipalExtensions
	{
		public static string GetClaimAsString(this ClaimsPrincipal principal, string claimName)
		{
			if (principal == null)
				return "";
			var claim = principal.FindFirst(claimName);
			if (claim != null)
				return claim.Value;
			else
				return "";
		}

		public static int GetClaimAsInt(this ClaimsPrincipal principal, string claimName)
		{
			if (principal == null)
				return 0;
			var claim = principal.FindFirst(claimName);
			if (claim != null && !String.IsNullOrWhiteSpace(claim.Value))
				return Convert.ToInt32(claim.Value);
			else
				return -1;
		}


		public static bool IsAnyRole(this ClaimsPrincipal principal, params string[] roles)
		{
			foreach (string role in roles)
				if (principal.IsInRole(role))
					return true;
			return false;
		}
	}
}
