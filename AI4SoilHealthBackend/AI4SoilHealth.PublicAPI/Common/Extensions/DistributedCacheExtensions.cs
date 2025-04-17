using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace PublicAPI.Common.Extensions {
    public static class DistributedCacheExtensions {

        public class CacheKey {
            public string key;
            public DateTime expiration;
        }

        static Dictionary<string, CacheKey> cacheKeys = new Dictionary<string, CacheKey>();

        public static DistributedCacheEntryOptions GetCacheOptions(string cacheType) {

            var timespan = cacheType switch
            {
                "long" => TimeSpan.FromSeconds(5),
                "short" => TimeSpan.FromSeconds(5),
                _ => TimeSpan.FromSeconds(5)
            };

#if DEBUG
            //timespan = TimeSpan.FromSeconds(5);
#endif
            var opts = new DistributedCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = timespan
            };
            return opts;
        }

        public static void Set<T>(this IDistributedCache cache, string key, T value, string cacheType) {
            cache.SetString(key, JsonConvert.SerializeObject(value), GetCacheOptions(cacheType));
            cacheKeys[key] = new CacheKey() { key = key, expiration = DateTime.UtcNow.Add((TimeSpan)GetCacheOptions(cacheType).AbsoluteExpirationRelativeToNow) };
        }

        public static async Task SetAsync<T>(this IDistributedCache cache, string key, T value, string cacheType) {
            await cache.SetStringAsync(key, JsonConvert.SerializeObject(value), GetCacheOptions(cacheType));
            cacheKeys[key] = new CacheKey() { key = key, expiration = DateTime.UtcNow.Add((TimeSpan)GetCacheOptions(cacheType).AbsoluteExpirationRelativeToNow) };
        }

        public static T Get<T>(this IDistributedCache cache, string key) {
            var temp = cache.GetString(key);
            if (temp == null) {
                return default;
            }
            return JsonConvert.DeserializeObject<T>(temp);
        }

        public static async Task<T> GetAsync<T>(this IDistributedCache cache, string key) {
            var temp = await cache.GetStringAsync(key);
            if (temp == null) {
                return default;
            }
            return JsonConvert.DeserializeObject<T>(temp);
        }

        public static async Task ClearAsync(this IDistributedCache cache, string key) {
            await cache.RemoveAsync(key);
        }

        public static async Task Clear(this IDistributedCache cache, string key) {
            await cache.RemoveAsync(key);
        }

        public static async Task ClearAll(this IDistributedCache cache) {
            foreach (var key in cacheKeys.Keys) {
                await cache.RemoveAsync(key);
                cacheKeys.Remove(key);
            }
        }

        public static void ClearAllExpired(this IDistributedCache cache) {
            foreach (var key in cacheKeys.Keys) {
                if (cacheKeys[key].expiration < DateTime.UtcNow) {
                    cacheKeys.Remove(key);
                }
            }
        }
    }
}
