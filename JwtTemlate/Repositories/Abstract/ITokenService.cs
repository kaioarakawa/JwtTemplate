using System;
using System.Security.Claims;
using JwtTemlate.Models.DTO;

namespace JwtTemlate.Repositories.Abstract
{
	public interface ITokenService
	{
		TokenResponse GetToken(IEnumerable<Claim> claim);

		string GetRefreshToken();

		ClaimsPrincipal GetPrincipalFromExpiredToken(string token); 
	}
}

