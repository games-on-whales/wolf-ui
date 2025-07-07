using System;
using System.Runtime.Caching;

namespace Resources.WolfAPI;

public partial class WolfApi
{
    private static readonly MemoryCache _cache = MemoryCache.Default;

    private static CacheItemPolicy WolfAPICachePolicy
    {
        get
        {
            Random random = new();
            var variance = random.NextDouble() * 60;

            var cacheItemPolicy = new CacheItemPolicy()
            {
                //Set your Cache expiration.
                AbsoluteExpiration = DateTime.Now.AddSeconds(90 + variance)
            };
            return cacheItemPolicy;
        }
    }
}
