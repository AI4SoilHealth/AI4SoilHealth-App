using PublicAPI.Common.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Serilog.Context;

namespace PublicAPI.Common.Filters {

    /**
        * Authentication filter
        @module AuthFilter
*/
    public sealed class AuthFilter : IAsyncAuthorizationFilter {
        private readonly AppOptions _options;
        private readonly AuthService _authService;
        private readonly CurrentUserInfo _currentUserInfo;

        public AuthFilter(IOptions<AppOptions> options, AuthService authService, CurrentUserInfo currentUserInfo) {
            _options = options.Value;
            _authService = authService;
            _currentUserInfo = currentUserInfo;
        }

        /** 
            * On authorization
            * @param {AuthorizationFilterContext} context - Context
*/
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context) {
            var user = context.HttpContext.User;
            if (user.Identity.IsAuthenticated) {
                //SetUserAsync(user, context).Wait();
                await   SetUserAsync(user, context);
            }
            System.Diagnostics.Debug.WriteLine("Pushing PersonId" + _currentUserInfo.PersonId.ToString());
            LogContext.PushProperty("PersonId", _currentUserInfo.PersonId);
        }

        /**
            * Set user
            * @param {ClaimsPrincipal} user - User
            * @param {AuthorizationFilterContext} context - Context
            * @return {Task} - Task
*/
        private async Task SetUserAsync(ClaimsPrincipal user, AuthorizationFilterContext context) {
            if (!await _authService.SetUser(user)) {
                context.Result = new UnauthorizedResult();
            }
        }

        /**
            * Has allow anonymous
            * @param {AuthorizationFilterContext} context - Context
            * @return {bool} - True if anonymous context is allowed, false otherwise
*/
        private static bool HasAllowAnonymous(AuthorizationFilterContext context) {
            if (context.ActionDescriptor.EndpointMetadata.Any(x => x is AllowAnonymousAttribute)) {
                return true;
            }
            return false;
        }
    }
}
