using PublicAPI.Common.Services;

namespace PublicAPI.Common.Filters {

    /** 
        * Header middleware
        @module HeaderMiddleware
*/
    public class HeaderMiddleware {
        private readonly RequestDelegate _next;
        
        public HeaderMiddleware(RequestDelegate next) {
            _next = next;
        }

        /**
            * Invoke method
            * @param {HttpContext} httpContext - Context
            * @param {AuthService} authService - Authentication service
            * @return {Task} - Task
        */
        public async Task InvokeAsync(HttpContext httpContext, AuthService authService) {

            
            var tempLang = httpContext.Request.Headers["LangId"];
            if (int.TryParse(tempLang.FirstOrDefault(), out int langId)) {
                authService.SetCurrentLang(langId);
            }

            var tempEmulatedPersonId = httpContext.Request.Headers["EU"];
            if (int.TryParse(tempEmulatedPersonId.FirstOrDefault(), out int emulatedPersonId)) {
                authService.SetEmulatedPerson(emulatedPersonId);
            } else {
                authService.SetEmulatedPerson(null);
            }


            await _next(httpContext);
        }
    }


    /**
        * Middleware extensions
    */
    //public static class MiddlewareExtensions {
    //    public static IApplicationBuilder UseAI4SoilHealthHeaders(this IApplicationBuilder builder) {
    //        return builder.UseMiddleware<HeaderMiddleware>();
    //    }
    //}
}
