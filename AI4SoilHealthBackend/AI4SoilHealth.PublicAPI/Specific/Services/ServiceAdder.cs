using PublicAPI.Common.Services;
using System.Threading.RateLimiting;

namespace PublicAPI.Specific.Services {
    public class ServiceAdder {
        public static void AddSpecificServices(WebApplicationBuilder builder) {
            builder.Services.AddTransient<GdalService>();
            builder.Services.AddHostedService<CleanerService>();
            if (!Directory.Exists(GlobalConstants.imageDir)) {
                Directory.CreateDirectory(GlobalConstants.imageDir);
            }
        }

        public static void AddSpecificThings(WebApplication app) {
        }
    }
}
