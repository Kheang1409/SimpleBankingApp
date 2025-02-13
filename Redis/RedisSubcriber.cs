using SimpleBankingApp.Services;
using StackExchange.Redis;

namespace SimpleBankingApp.Redis
{
    public class RedisSubscriber : BackgroundService
    {
        private readonly CacheService _cache;
        private readonly IConnectionMultiplexer _redis;

        public RedisSubscriber(CacheService cache, IConnectionMultiplexer redis)
        {
            _cache = cache;
            _redis = redis;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var sub = _redis.GetSubscriber();
            await sub.SubscribeAsync("cache-invalidation", async (channel, message) =>
            {
                await _cache.RemoveDataAsync(message);
                Console.WriteLine($"Cache invalidated for key: {message}");
            });
        }
    }
}
