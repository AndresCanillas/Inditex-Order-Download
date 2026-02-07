using Service.Contracts.Authentication;
using System;
using System.Security.Claims;

namespace WebLink.Contracts
{
	public static class ClaimsPrincipalExtensions
	{
		//public static string GetID(this ClaimsPrincipal principal)
		//{
		//	return principal.GetClaimAsString("Id");
		//}

		//public static int GetCompanyID(this ClaimsPrincipal principal)
		//{
		//	return principal.GetClaimAsInt("CompanyID");
		//}

		//public static int GetSelectedCompanyID(this ClaimsPrincipal principal)
		//{
		//	if(principal.IsIDT())
		//		return principal.GetClaimAsInt("SelectedCompanyID");
		//	return principal.GetClaimAsInt("CompanyID");
		//}

		//public static int GetSelectedBrandID(this ClaimsPrincipal principal)
		//{
		//	var brandID = principal.GetClaimAsInt("SelectedBrandID");
		//	return brandID;
		//}

		//public static int GetSelectedProjectID(this ClaimsPrincipal principal)
		//{
		//	var projectID = principal.GetClaimAsInt("SelectedProjectID");
		//	return projectID;
		//}

		//public static int GetLocationID(this ClaimsPrincipal principal)
		//{
		//	return principal.GetClaimAsInt("LocationID");
		//}


		public static bool ValidateRoles(this ClaimsPrincipal principal, string requiredRoles)
		{
			if (String.IsNullOrWhiteSpace(requiredRoles))
				return true;
			if (principal.IsInRole(Roles.SysAdmin))
				return true;
			var roles = requiredRoles.Split(",", StringSplitOptions.RemoveEmptyEntries);
			foreach (var role in roles)
			{
				if (principal.IsInRole(role))
					return true;
			}
			return false;
		}


		//public static void AddClaims<T>(this ClaimsPrincipal principal, T user)
		//{
		//	PropertyInfo[] props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
		//	ClaimsIdentity identity = principal.Identity as ClaimsIdentity;
		//	foreach (var p in props)
		//	{
		//		if (p.IsSpecialName || !p.CanRead || !IsSupportedDataType(p.PropertyType) || p.Name.ToLower().Contains("password")) continue;
		//		var value = Reflex.GetProperty(user, p.Name);

		//		var claim = identity.Claims.FirstOrDefault(c => c.Type == p.Name);
		//		if (claim != null) identity.RemoveClaim(claim);

		//		if (value != null)
		//			identity.AddClaim(new Claim(p.Name, value.ToString()));
		//		else
		//			identity.AddClaim(new Claim(p.Name, ""));
		//	}
		//}

		//private static bool IsSupportedDataType(Type t)
		//{
		//	return t == typeof(int) || t == typeof(long) ||
		//		t == typeof(float) || t == typeof(double) ||
		//		t == typeof(decimal) || t == typeof(bool) ||
		//		t == typeof(DateTime) || t == typeof(string) ||
		//		t == typeof(Nullable<int>);
		//}
	}
}
