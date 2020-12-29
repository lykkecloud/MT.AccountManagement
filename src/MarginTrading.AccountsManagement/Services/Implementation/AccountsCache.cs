﻿using System;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.AccountsManagement.Settings;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Internal;
using Newtonsoft.Json;

namespace MarginTrading.AccountsManagement.Services.Implementation
{
    public class AccountsCache
    {
        public enum Category
        {
            GetAccount,
            GetAccountBalanceChanges,
            GetCompensations,
            GetDividends,
            GetTaxFileMissingDays,
            GetDeals
        }

        private readonly IDistributedCache _cache;
        private readonly ISystemClock _systemClock;
        private readonly CacheSettings _cacheSettings;
        private readonly JsonSerializerSettings _serializerSettings;

        public AccountsCache(IDistributedCache cache, ISystemClock systemClock, CacheSettings cacheSettings)
        {
            _cache = cache;
            _systemClock = systemClock;
            _cacheSettings = cacheSettings;
            _serializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            };
        }


        public Task<T> Get<T>(string accountId, Category category, Func<Task<T>> getValue)
        {
            return Get(accountId, category, async () => (value: await getValue(), shouldCache: true));
        }

        public async Task<T> Get<T>(string accountId, Category category, Func<Task<(T value, bool shouldCache)>> getValue)
        {
            var cacheKey = BuildCacheKey(accountId, category);
            var cached = await _cache.GetStringAsync(BuildCacheKey(accountId, category));

            if (cached != null)
            {
                return  JsonConvert.DeserializeObject<T>(cached, _serializerSettings);
            }

            var result = await getValue();
            if (result.shouldCache)
            {
                var serialized = JsonConvert.SerializeObject(result.value, _serializerSettings);
                await _cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _cacheSettings.ExpirationPeriod
                });
            }

            return result.value;
        }

        public async Task Invalidate(string accountId)
        {
            foreach (var cat in Enum.GetValues(typeof(Category)).Cast<Category>())
            {
                await InvalidateCache(accountId, cat);
            }
        }


        private Task InvalidateCache(string accountId, Category category)
        {
            var cacheKey = BuildCacheKey(accountId, category);
            return _cache.RemoveAsync(cacheKey);
        }

        private string BuildCacheKey(string accountId, Category category)
        {
            var now = _systemClock.UtcNow.Date;
            return $"ac:{accountId}:{category:G}:{now:yyyy-MM-dd}";
        }
    }
}
