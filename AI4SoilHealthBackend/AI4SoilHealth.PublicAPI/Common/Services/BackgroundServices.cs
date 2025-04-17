using PublicAPI.Common.Extensions;
using Microsoft.Extensions.Caching.Distributed;

namespace PublicAPI.Common.Services {

    /**
        * Mailing related operations   
        @module MailService
*/

    public class CacheExpirationService : BackgroundService {

        private readonly ILogger<CacheExpirationService> _logger;
        private readonly IDistributedCache _cache;

        public CacheExpirationService(ILogger<CacheExpirationService> logger, IDistributedCache cache) {
            _logger = logger;
            _cache = cache;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            while (!stoppingToken.IsCancellationRequested) {
                await Task.Delay(60000, stoppingToken);
                Console.WriteLine("CacheExpirationService running at: " + DateTimeOffset.Now);
                _cache.ClearAllExpired();
            }
        }
    }
}
