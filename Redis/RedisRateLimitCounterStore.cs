using AspNetCoreRateLimit;
using StackExchange.Redis;
using System;

namespace SimpleBankingApp.Redis
{
    public class RedisRateLimitCounterStore : IRateLimitCounterStore
    {
        private readonly IDatabase _database;
        private readonly string _prefix = "rateLimit:";

        public RedisRateLimitCounterStore(IConnectionMultiplexer redis)
        {
            _database = redis.GetDatabase();
        }

        // Check if the rate limit counter exists
        public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
        {
            var redisKey = $"{_prefix}{id}";
            return await _database.KeyExistsAsync(redisKey);
        }

        // Get the rate limit counter for a specific id
        public async Task<RateLimitCounter?> GetAsync(string id, CancellationToken cancellationToken = default)
        {
            var redisKey = $"{_prefix}{id}";
            var counter = await _database.HashGetAsync(redisKey, "counter");

            // If no counter exists, return null
            if (counter.IsNullOrEmpty)
                return null;

            var timestamp = await _database.HashGetAsync(redisKey, "timestamp");

            // Return the rate limit counter
            return new RateLimitCounter
            {
                Count = (int)counter,
                Timestamp = timestamp.HasValue ? DateTime.Parse(timestamp) : DateTime.MinValue
            };
        }

        // Remove the rate limit counter from Redis
        public async Task RemoveAsync(string id, CancellationToken cancellationToken = default)
        {
            var redisKey = $"{_prefix}{id}";
            await _database.KeyDeleteAsync(redisKey);
        }

        // Set the rate limit counter for a specific id
        public async Task SetAsync(string id, RateLimitCounter? entry, TimeSpan? expirationTime = null, CancellationToken cancellationToken = default)
        {
            var redisKey = $"{_prefix}{id}";

            if (entry == null)
                return;

            var values = new HashEntry[]
            {
                new HashEntry("counter", entry?.Count),
                new HashEntry("timestamp", entry?.Timestamp.ToString()) // Convert DateTime to string
            };

            // Store the counter and set the expiration time for the key
            await _database.HashSetAsync(redisKey, values);

            // If an expiration time is provided, set it for the key
            if (expirationTime.HasValue)
            {
                await _database.KeyExpireAsync(redisKey, expirationTime.Value);
            }
        }
    }
}
