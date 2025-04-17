using Microsoft.Extensions.Options;
using System.Security.Claims;
using Newtonsoft.Json;
using System.Drawing;

namespace PublicAPI.Common.Services {
    public class UserBasics {
        public int id { get; set; }
        public bool is_admin { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string nickname { get; set; }

    }
    /**
        * Authentication service
        @module AuthService
*/
    public class AuthService : IAuthService {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        private readonly AppOptions _options;
        private readonly Db _db;
        private readonly CurrentUserInfo _currentUserInfo;

        public ClaimsPrincipal User;
        public Guid UserId { get { return Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value); } }
        public string FirstName { get { return User.FindFirst(ClaimTypes.GivenName)?.Value; } }
        public string LastName { get { return User.FindFirst(ClaimTypes.Surname)?.Value; } }
        public string Email { get { return User.FindFirst(ClaimTypes.Email)?.Value; } }

        public AuthService(IConfiguration configuration, ILogger<AuthService> logger, Db db, IOptions<AppOptions> options, CurrentUserInfo currentUserInfo) {
            _configuration = configuration;
            _logger = logger;
            _options = options.Value;
            _db = db;
            _currentUserInfo = currentUserInfo;
        }

        /**
            * Set user
            * @param {ClaimsPrincipal} User - User
            * @return {Task<bool>} - Result of the operation    
*/
        public async Task<bool> SetUser(ClaimsPrincipal User) {
            this.User = User;
            var r = await _db.QueryScalarAsyncCached("auth.get_or_create_person_id", new { Email, FirstName, LastName }, _options.ConnectionString, "long");

            if (r != null) {
                var json = JsonConvert.DeserializeObject<UserBasics>(r);
                _currentUserInfo.PersonId = json.id;
                _currentUserInfo.IsAdmin = json.is_admin;
                _currentUserInfo.FirstName = json.first_name;
                _currentUserInfo.LastName = json.last_name;
                _currentUserInfo.Nickname = json.nickname;
                if (_currentUserInfo.EmulatedPersonId != null && _currentUserInfo.IsAdmin) {
                    _currentUserInfo.PersonId = _currentUserInfo.EmulatedPersonId;
                    _currentUserInfo.IsAdmin = false;
                } else {
                    _currentUserInfo.PersonId = json.id;
                }
                return true;
            } else {
                _currentUserInfo.PersonId = null;
                return false;
            }
        }

        /**
            * Set current language
            * @param {int} LangId - Language id
            * @return {void}
        */
        public void SetCurrentLang(int LangId) {
            _currentUserInfo.LangId = LangId;
        }

        /**
            * Set emulated user
            * @param {int} EmulatedPersonId - Emulated user id
            * @return {void}
        */
        public void SetEmulatedPerson(int? EmulatedPersonId) {
            _currentUserInfo.EmulatedPersonId = EmulatedPersonId;
        }
    }
}
