using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Newtonsoft.Json;
using PublicAPI.Common.Services;

namespace PublicAPI.Common.Controllers {
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]

    /**
    * Authentication controller
    * @module AuthController
    */
    public class AuthController : ControllerBase {
        private readonly AppOptions _options;
        private readonly ILogger<AuthController> _logger;
        private readonly Db _db;
        private readonly AuthService _authService;
        private readonly CurrentUserInfo _currentUserInfo;
        public AuthController(IOptions<AppOptions> options, ILogger<AuthController> logger, Db db, AuthService authService, CurrentUserInfo currentUserInfo) {
            _options = options.Value;
            _logger = logger;
            _db = db;
            _authService = authService;
            _currentUserInfo = currentUserInfo;
        }

        /**
        * Get routes for the current user
        * @return {ActionResult} - List of routes
        */
        [AllowAnonymous]
        [HttpGet("GetRoutes")]
        public async Task<ActionResult> GetRoutes() {
            var ret = await _db.QueryJsonAsyncCached("meta.get_routes", new { _currentUserInfo.PersonId, _currentUserInfo.LangId }, _options.ConnectionString, "long");
            return Ok(ret);
        }

        /**
        * Get user information
        * @param {string} hash - Referral hash
        * @return {ActionResult} - User information
        */
        [AllowAnonymous]
        [HttpGet("GetUser/{hash?}")]
        public async Task<IActionResult> GetUser(string hash) {

            try {
                if (_authService.User != null) {
                    // get ip address
                    var ip = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                    if (string.IsNullOrEmpty(ip)) {
                        ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                    }
                    var ret = await _db.QueryJsonAsync("auth.login", new { _currentUserInfo.PersonId, IP = ip, Ref = hash }, _options.ConnectionString);
                    return Ok(ret);
                } else
                    return Ok(null);
            } catch (Exception ex) {
                return Ok(Helper.CreateError(_logger, "Error in token creation: " + ex.Message));
            }
        }

        /**
         * Accept agreement
         * 
         */
        [HttpPost("AcceptAgreement")]
        public async Task<IActionResult> AcceptAgreement() {
            var ret = await _db.QueryScalarAsync("auth.accept_agreement", new { _currentUserInfo.PersonId }, _options.ConnectionString, true);
            return Ok(ret);
        }

        /**
         * Get all users for impersonation
         *
        */
        [HttpGet("GetUsers")]
        public async Task<IActionResult> GetUsers() {
            var ret = await _db.QueryJsonAsync("auth.get_users", new { }, _options.ConnectionString, true);
            return Ok(ret);
        }

        /**
        * Google authentication
        * @param {GoogleAuthRequest} request - Google authentication request
        * @return {ActionResult} - User information
        */
        /*
        [AllowAnonymous]
        [HttpPost("google")]
        public async Task<IActionResult> GoogleAuth([FromBody] GoogleAuthRequest request)
        {
            try
            {

                if (request == null || request.IdToken == null)
                {
                    return BadRequest();
                }

                var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken);

                if (payload == null || string.IsNullOrEmpty(payload.Email))
                {
                    return BadRequest("Payload is empty or Email is null");
                }

                var UserId = await _db.QueryScalarAsync("SELECT Id FROM Auth.User WHERE user_name = @Email", new { payload.Email }, _options.ConnectionString, false, System.Data.CommandType.Text);
                if (UserId == null)
                {
                    return Ok(Helper.CreateError(_logger, "User not found"));
                }

                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, payload.Name),
                    new Claim(ClaimTypes.Email, payload.Email),
                    new Claim(ClaimTypes.Sid, UserId)
                    // Add more claims as needed
                };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var expires = DateTime.Now.AddMinutes(1);
                //var expires = DateTime.Now.AddHours(1); // Set token expiration as needed

                var token = new JwtSecurityToken(
                    issuer: _options.Issuer,
                    audience: _options.Audience,
                    claims: claims,
                    expires: expires,
                    signingCredentials: creds
                );

                var ret = new JwtSecurityTokenHandler().WriteToken(token);
                // get initials from payload.GivenName and payload.FamilyName, if set
                var initials = "";
                if (!string.IsNullOrEmpty(payload.GivenName))
                {
                    initials += payload.GivenName.Substring(0, 1);
                }
                if (!string.IsNullOrEmpty(payload.FamilyName))
                {
                    initials += payload.FamilyName.Substring(0, 1);
                }
                return Ok(JsonConvert.SerializeObject(new { name = payload.Name, initials, givenName = payload.GivenName, familyName = payload.FamilyName, userToken = ret }));
            }
            catch (Exception ex)
            {
                return Ok(Helper.CreateError(_logger, "Error in token creation: " + ex.Message));
            }
        }
        */
    }

    /**
    * Google authentication request
    * @typedef {Object} GoogleAuthRequest
    * @property {string} IdToken - Google authentication token
    */
    /*
    public class GoogleAuthRequest
    {
        public string IdToken { get; set; }
    }
    */

}
