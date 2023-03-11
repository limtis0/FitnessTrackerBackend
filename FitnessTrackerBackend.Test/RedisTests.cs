using FitnessTrackerBackend.Test.Fixtures;
using StackExchange.Redis;

namespace FitnessTrackerBackend.Test
{
    [Collection("RedisCollection")]
    public class RedisTests
    {
        private readonly IDatabase _redis;

        public RedisTests(RedisFixture redisFixture)
        {
            if (redisFixture.ServiceProvider.GetService(typeof(IConnectionMultiplexer)) is not IConnectionMultiplexer redis)
            {
                throw new ArgumentException("Redis service is not set up, or set up incorrectly");
            }
            _redis = redis.GetDatabase();
        }

        [Fact]
        public void Redis_Should_Return_Pong()
        {
            // Act
            var result = _redis.Execute("PING");

            // Assert
            Assert.Equal("PONG", result.ToString());
        }
    }
}