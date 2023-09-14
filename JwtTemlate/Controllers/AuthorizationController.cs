using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using JwtTemlate.Models;
using JwtTemlate.Models.Domain;
using JwtTemlate.Models.DTO;
using JwtTemlate.Repositories.Abstract;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace JwtTemlate.Controllers
{
    [Route("api/[controller]/{action}")]
    [ApiController]
    public class AuthorizationController : ControllerBase
    {
        private readonly DatabaseContext _context;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly ITokenService _tokenService;

        public AuthorizationController(DatabaseContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ITokenService tokenService)
        {
            this._context = context;
            this.userManager = userManager;
            this.roleManager = roleManager;
            this._tokenService = tokenService;
        }

        [HttpPost]
        [Route("/Login")]
        public async Task<IActionResult> Login([FromBody]LoginModel loginModel)
        {
            var user = await userManager.FindByEmailAsync(loginModel.Username);

            if(user != null && await userManager.CheckPasswordAsync(user, loginModel.Password))
            {
                var userRoles = await userManager.GetRolesAsync(user);
                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };

                foreach (var userRole in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                }

                var token = _tokenService.GetToken(authClaims);
                var refreshToken = _tokenService.GetRefreshToken();
                var tokenInfo = _context.TokenInfo.FirstOrDefault(x => x.Username == user.UserName);

                if(tokenInfo == null)
                {
                    var info = new TokenInfo
                    {
                        Username = user.UserName,
                        RefreshToken = refreshToken,
                        RefreshTokenExpiry = DateTime.Now.AddDays(1)
                    };

                    await _context.TokenInfo.AddAsync(info);
                }
                else
                {
                    tokenInfo.RefreshToken = refreshToken;
                    tokenInfo.RefreshTokenExpiry = DateTime.Now.AddDays(1);
                }

                try
                {
                    _context.SaveChanges();
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }

                return Ok(new LoginResponse
                {
                    Name = user.Name,
                    Username = user.UserName,
                    Token = token.TokenString,
                    RefreshToken = refreshToken,
                    Expiration = token.ValidTo,
                    StatusCode = 1,
                    Message = "Logged in"
                });
            }


            //Login failed
            return Ok(
               new LoginResponse
               {
                   StatusCode = 0,
                   Message = "Invalid Username or Password",
                   Token = "",
                   Expiration = null
               });
        }

        [HttpPost]
        [Route("/Registration")]
        public async Task<IActionResult> Registration([FromBody]RegistrationModel registrationModel)
        {
            var status = new Status();

            if (!ModelState.IsValid)
            {
                status.StatusCode = 0;
                status.Message = "Please pass all the required fields";

                return Ok(status);
            }

            //check if user exists
            var userExists = await userManager.FindByNameAsync(registrationModel.Username);

            if(userExists != null)
            {
                status.StatusCode = 0;
                status.Message = "User alredy register";
                return Ok(status);
            }

            var user = new ApplicationUser
            {
                UserName = registrationModel.Username,
                Name = registrationModel.Name,
                SecurityStamp = Guid.NewGuid().ToString(),
                Email = registrationModel.Email,
            };

            //create user
            var result = await userManager.CreateAsync(user, registrationModel.Password);

            if (!result.Succeeded)
            {
                status.StatusCode = 0;
                status.Message = "User creation failed";

                return Ok(status);
            }

            //add roles
            if (!await roleManager.RoleExistsAsync(UserRoles.User))
                await roleManager.CreateAsync(new IdentityRole(UserRoles.User));

            if (await roleManager.RoleExistsAsync(UserRoles.User))
            {
                await userManager.AddToRoleAsync(user, UserRoles.User);
            }

            status.StatusCode = 1;
            status.Message = "Sucessfully registered";

            return Ok(status);
        }

        [HttpPost]
        [Route("/RegistrationAdmin")]
        public async Task<IActionResult> RegistrationAdmin([FromBody] RegistrationModel registrationModel)
        {
            var status = new Status();

            if (!ModelState.IsValid)
            {
                status.StatusCode = 0;
                status.Message = "Please pass all the required fields";

                return Ok(status);
            }

            //check if user exists
            var userExists = await userManager.FindByNameAsync(registrationModel.Username);

            if (userExists != null)
            {
                status.StatusCode = 0;
                status.Message = "User alredy register";
                return Ok(status);
            }

            var user = new ApplicationUser
            {
                UserName = registrationModel.Username,
                Name = registrationModel.Name,
                SecurityStamp = Guid.NewGuid().ToString(),
                Email = registrationModel.Email,
            };

            //create user
            var result = await userManager.CreateAsync(user, registrationModel.Password);

            if (!result.Succeeded)
            {
                status.StatusCode = 0;
                status.Message = "User creation failed";

                return Ok(status);
            }

            //add roles
            if (!await roleManager.RoleExistsAsync(UserRoles.Admin))
                await roleManager.CreateAsync(new IdentityRole(UserRoles.Admin));

            if (await roleManager.RoleExistsAsync(UserRoles.Admin))
            { 
                await userManager.AddToRoleAsync(user, UserRoles.Admin);
            }

            status.StatusCode = 1;
            status.Message = "Sucessfully registered";

            return Ok(status);
        }

        [HttpPost]
        [Route("/ChangePassword")]
        public async Task<IActionResult> ChangePassword(ChangePasswordModel changePasswordModel)
        {
            var status = new Status();

            //check validation
            if (!ModelState.IsValid)
            {
                status.StatusCode = 0;
                status.Message = "Please pass all the valid fields";

                return Ok(status);
            }

            //find user
            var user = await userManager.FindByNameAsync(changePasswordModel.Username);

            if(user is null)
            {
                status.StatusCode = 0;
                status.Message = "Invalid username";

                return Ok(status);
            }

            //check current password
            if(!await userManager.CheckPasswordAsync(user, changePasswordModel.CurrentPassword))
            {
                status.StatusCode = 0;
                status.Message = "Invalid current password";

                return Ok(status);
            }

            //change password
            var result = await userManager.ChangePasswordAsync(user, changePasswordModel.CurrentPassword, changePasswordModel.NewPassword);

            if (!result.Succeeded)
            {
                status.StatusCode = 0;
                status.Message = "Failed to change password";

                return Ok(status);
            }

            status.StatusCode = 1;
            status.Message = "Password has change succesfully";
            return Ok(result);
        }
    }
}

