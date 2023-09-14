using System;
using Microsoft.AspNetCore.Identity;

namespace JwtTemlate.Models.Domain
{
	public class ApplicationUser : IdentityUser
	{
		public string? Name { get; set; }
	}
}

