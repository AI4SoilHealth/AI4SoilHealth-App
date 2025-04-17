using System.Globalization;

namespace PublicAPI.Common.Services {
    /**
        * App options
        @module AppOptions
*/

    /**
        * App options
        @class AppOptions
*/

    public class AppOptions {
        public string ConnectionString { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public string SecretKey { get; set; }

        public string GoogleClientId { get; set; }

        public string KeycloakAudience { get; set; }
        public string KeycloakAuthority { get; set; }

        public string GoogleAPI { get; set; }
    }
}