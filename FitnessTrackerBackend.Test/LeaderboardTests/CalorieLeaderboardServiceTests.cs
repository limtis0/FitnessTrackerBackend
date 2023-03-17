using FitnessTrackerBackend.Services.Leaderboard;
using FitnessTrackerBackend.Services.Workouts;
using FitnessTrackerBackend.Test.Fixtures.Redis;
using StackExchange.Redis;

namespace FitnessTrackerBackend.Test.LeaderboardTests
{
    public class CalorieLeaderboardServiceTests : IClassFixture<RedisFixture>, IDisposable
    {
        private readonly RedisFixture _redisFixture;
        private readonly IDatabase _redis;
        private readonly WorkoutService _workoutService;
        private readonly CalorieLeaderboardService _leaderboardService;

        public CalorieLeaderboardServiceTests(RedisFixture redisFixture)
        {
            _redisFixture = redisFixture;
            _redis = redisFixture.DB;
            _workoutService = new WorkoutService(_redis);
            _leaderboardService = new(_redis, _workoutService);
        }

        public void Dispose()
        {
            _redisFixture.FlushDatabase();

            GC.SuppressFinalize(this);
        }

    }
}
