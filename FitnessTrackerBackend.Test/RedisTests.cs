using FitnessTrackerBackend.Test.Fixtures.Redis;
using StackExchange.Redis;

namespace FitnessTrackerBackend.Test
{
    public class RedisTests : IClassFixture<RedisFixture>
    {
        private readonly IDatabase _redis;

        public RedisTests(RedisFixture redisFixture)
        {
            _redis = redisFixture.DB;
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