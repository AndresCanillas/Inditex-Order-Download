using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace Middleware
{
	public class AuthorizeRolesAttribute : AuthorizeAttribute
	{
		public AuthorizeRolesAttribute(params string[] roles) : base()
		{
			Roles = string.Join(",", roles);
		}
	}
}
