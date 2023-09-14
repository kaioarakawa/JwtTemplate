using System;
namespace JwtTemlate.Models.DTO
{
	public class TokenResponse
	{
		public string? TokenString { get; set; }

		public DateTime ValidTo { get; set; }
	}
}

