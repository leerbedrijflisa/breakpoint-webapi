using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc;

namespace Lisa.Breakpoint.WebApi.controllers
{
    // https://github.com/mrsheepuk/ASPNETSelfCreatedTokenAuthExample
    [Route("token")]
    public class TokenController : BaseController
    {
        private readonly TokenAuthOptions tokenOptions;

        public TokenController(TokenAuthOptions tokenOptions, RavenDB db)
            : base (db)
        {
            this.tokenOptions = tokenOptions;
            //this.bearerOptions = options.Value;
            //this.signingCredentials = signingCredentials;
        }

        /// <summary>
        /// Check if currently authenticated. Will throw an exception of some sort which should be caught by a general
        /// exception handler and returned to the user as a 401, if not authenticated. Will return a fresh token if
        /// the user is authenticated, which will reset the expiry.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize("Bearer")]
        public IActionResult Get()
        {
            bool authenticated = false;
            string username = null;
            int entityId = -1;
            string token = null;
            DateTime? tokenExpires = default(DateTime?);

            var currentUser = HttpContext.User;
            if (currentUser != null)
            {
                authenticated = currentUser.Identity.IsAuthenticated;
                if (authenticated)
                {
                    username = currentUser.Identity.Name;
                    foreach (Claim c in currentUser.Claims) if (c.Type == "EntityID") entityId = Convert.ToInt32(c.Value);
                    tokenExpires = DateTime.UtcNow.AddHours(2);
                    token = GetToken(currentUser.Identity.Name, tokenExpires);
                }
            }
            var tokenResponse = new TokenResponse
            {
                User = username,
                Token = token,
                TokenExpires = tokenExpires
            };

            return new HttpOkObjectResult(tokenResponse);
        }

        /// <summary>
        /// Request a new token for a given username/password pair.
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult Post([FromBody] AuthRequest req)
        {
            if (req == null)
            {
                return new BadRequestResult();
            }

            if (!ModelState.IsValid)
            {
                if (ErrorList.FromModelState(ModelState))
                {
                    return new UnprocessableEntityObjectResult(ErrorList.FatalErrors);
                }

                return new UnprocessableEntityObjectResult(ErrorList.Errors);
            }

            if (!Db.UserExists(req.username))
            {
                return new HttpUnauthorizedResult();
            }

            DateTime? expires = DateTime.UtcNow.AddHours(2);
            var token = GetToken(req.username, expires);

            var tokenResponse = new TokenResponse
            {
                User = req.username,
                Token = token,
                TokenExpires = expires
            };

            return new HttpOkObjectResult(tokenResponse);
        }

        /// <summary>
        /// Creates a JWT token from the given parameters and global signature
        /// </summary>
        /// <param name="username">The username to base the token off</param>
        /// <param name="expires">The time until which the token is valid</param>
        /// <returns></returns>
        private string GetToken(string username, DateTime? expires)
        {
            var handler = new JwtSecurityTokenHandler();

            var user = Db.GetUser(username);

            var claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.Name, user.FullName, ClaimValueTypes.String));

            var identity = new ClaimsIdentity(new GenericIdentity(user.UserName, "TokenAuth"), claims);

            var securityToken = handler.CreateToken(
                audience: tokenOptions.Audience,
                issuer: tokenOptions.Issuer,
                signingCredentials: tokenOptions.SigningCredentials,
                subject: identity,
                expires: expires
                );
            return handler.WriteToken(securityToken);
        }
    }
}
