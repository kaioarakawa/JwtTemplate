using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JwtTemlate.Models.Domain;
using JwtTemlate.Models.DTO;
using JwtTemlate.Repositories.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace JwtTemlate.Controllers
{
    [Route("api/[controller]/{action}")]
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly DatabaseContext _context;
        private readonly ITokenService _service;

        public TokenController(DatabaseContext context, ITokenService service)
        {
            this._context = context;
            this._service = service;
        }

        [HttpPost]
        [Route("/Refresh")]
        public IActionResult Refresh(RefreshTokenRequest tokenApiModel)
        {
            if (tokenApiModel is null)
                return BadRequest("Invalid client request");

            string accessToken = tokenApiModel.AccessToken;
            string refreshToken = tokenApiModel.RefreshToken;
            var principal = _service.GetPrincipalFromExpiredToken(accessToken);
            var username = principal.Identity.Name;
            var user = _context.TokenInfo.SingleOrDefault(u => u.Username == username);


            if (user is null || user.RefreshToken != refreshToken || user.RefreshTokenExpiry <= DateTime.Now)
                return BadRequest("Invalid client request");


            var newAccessToken = _service.GetToken(principal.Claims);
            var newRefreshToken = _service.GetRefreshToken();


            user.RefreshToken = newRefreshToken;
            _context.SaveChanges();


            return Ok(new RefreshTokenRequest()
            {
                AccessToken = newAccessToken.TokenString,
                RefreshToken = newRefreshToken
            });
        }

        //Revoke is used to revoming token access
        [HttpPost, Authorize]
        [Route("/Revoke")]
        public IActionResult Revoke()
        {
            try
            {
                var username = User.Identity.Name;
                var user = _context.TokenInfo.SingleOrDefault(u => u.Username == username);

                if (user is null)
                    return BadRequest();

                user.RefreshToken = null;

                _context.SaveChanges();
                 
                return Ok(true);
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }
    }
}

