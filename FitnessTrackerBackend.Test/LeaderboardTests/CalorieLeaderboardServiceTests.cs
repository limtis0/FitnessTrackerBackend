using FitnessTrackerBackend.Models.Workouts;
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

        [Fact]
        public async Task CalorieLeaderboardService_UpdatesCorrectly_OnWorkoutAdded()
        {
            // Arrange
            string userId = "1";

            var workoutInput = new WorkoutInput
            {
                Name = "Leg Day",
                Description = "A workout focused on the legs",
                StartTime = new DateTimeOffset(2023, 03, 15, 12, 0, 0, TimeSpan.Zero),
                EndTime = new DateTimeOffset(2023, 03, 15, 13, 0, 0, TimeSpan.Zero),
                Exercises = new List<Exercise>
                {
                    new Exercise { Name = "Squats", Reps = 10, Sets = 3, Weight = 200, Calories = 100 },
                    new Exercise { Name = "Lunges", Reps = 12, Sets = 3, Weight = 150, Calories = 100 }
                }
            };

            List<Dictionary<string, object>> expected = new()
            {
                new Dictionary<string, object>() { { "userId", userId }, { "calories", 600 } }
            };

            // Act

            for (int i = 0; i < 3; i++)
            {
                await _workoutService.AddWorkoutAsync(userId, workoutInput);
            }
            
            var result = await _leaderboardService.GetCalorieLeaderboardRange(0, 99);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task CalorieLeaderboardService_SortsDescending()
        {
            // Arrange
            string userIdOne = "1";
            string userIdTwo = "2";

            var workoutInput = new WorkoutInput
            {
                Name = "Leg Day",
                Description = "A workout focused on the legs",
                StartTime = new DateTimeOffset(2023, 03, 15, 12, 0, 0, TimeSpan.Zero),
                EndTime = new DateTimeOffset(2023, 03, 15, 13, 0, 0, TimeSpan.Zero),
                Exercises = new List<Exercise>
                {
                    new Exercise { Name = "Squats", Reps = 10, Sets = 3, Weight = 200, Calories = 100 },
                    new Exercise { Name = "Lunges", Reps = 12, Sets = 3, Weight = 150, Calories = 100 }
                }
            };

            List<Dictionary<string, object>> expected = new()
            {
                new Dictionary<string, object>() { { "userId", userIdTwo }, { "calories", 600 } },
                new Dictionary<string, object>() { { "userId", userIdOne }, { "calories", 200 } }
            };

            // Act
            await _workoutService.AddWorkoutAsync(userIdOne, workoutInput);

            for (int i = 0; i < 3; i++)
            {
                await _workoutService.AddWorkoutAsync(userIdTwo, workoutInput);
            }

            var result = await _leaderboardService.GetCalorieLeaderboardRange(0, 99);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task CalorieLeaderboardService_ReturnsEmpty_WhenNoExcercisesRegistered()
        {
            // Act
            var result = await _leaderboardService.GetCalorieLeaderboardRange(0, 99);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task CalorieLeaderboardService_ReturnsCorrectRange()
        {
            // Arrange
            string userIdOne = "1";
            string userIdTwo = "2";
            string userIdThree = "3";

            var workoutInput = new WorkoutInput
            {
                Name = "Leg Day",
                Description = "A workout focused on the legs",
                StartTime = new DateTimeOffset(2023, 03, 15, 12, 0, 0, TimeSpan.Zero),
                EndTime = new DateTimeOffset(2023, 03, 15, 13, 0, 0, TimeSpan.Zero),
                Exercises = new List<Exercise>
                {
                    new Exercise { Name = "Squats", Reps = 10, Sets = 3, Weight = 200, Calories = 100 },
                    new Exercise { Name = "Lunges", Reps = 12, Sets = 3, Weight = 150, Calories = 100 }
                }
            };

            List<Dictionary<string, object>> expected = new()
            {
                new Dictionary<string, object>() { { "userId", userIdTwo }, { "calories", 400 } },
                new Dictionary<string, object>() { { "userId", userIdThree }, { "calories", 200 } }
            };

            // Act
            for (int i = 0; i < 3; i++)
            {
                await _workoutService.AddWorkoutAsync(userIdOne, workoutInput);
            }

            for (int i = 0; i < 2; i++)
            {
                await _workoutService.AddWorkoutAsync(userIdTwo, workoutInput);
            }

            await _workoutService.AddWorkoutAsync(userIdThree, workoutInput);

            var result = await _leaderboardService.GetCalorieLeaderboardRange(1, 2);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task CalorieLeaderboardService_UpdatesCorrectly_OnWorkoutUpdated()
        {
            // Arrange
            string userIdOne = "1";

            var workoutInput = new WorkoutInput
            {
                Name = "Leg Day",
                Description = "A workout focused on the legs",
                StartTime = new DateTimeOffset(2023, 03, 15, 12, 0, 0, TimeSpan.Zero),
                EndTime = new DateTimeOffset(2023, 03, 15, 13, 0, 0, TimeSpan.Zero),
                Exercises = new List<Exercise>
                {
                    new Exercise { Name = "Squats", Reps = 10, Sets = 3, Weight = 200, Calories = 100 },
                    new Exercise { Name = "Lunges", Reps = 12, Sets = 3, Weight = 150, Calories = 100 }
                }
            };

            var workoutInputUpdated = new WorkoutInput
            {
                Name = "Leg Day",
                Description = "A workout focused on the legs",
                StartTime = new DateTimeOffset(2023, 03, 15, 12, 0, 0, TimeSpan.Zero),
                EndTime = new DateTimeOffset(2023, 03, 15, 13, 0, 0, TimeSpan.Zero),
                Exercises = new List<Exercise>
                {
                    new Exercise { Name = "Squats", Reps = 10, Sets = 3, Weight = 200, Calories = 50 },
                    new Exercise { Name = "Lunges", Reps = 12, Sets = 3, Weight = 150, Calories = 50 }
                }
            };

            List<Dictionary<string, object>> expected = new()
            {
                new Dictionary<string, object>() { { "userId", userIdOne }, { "calories", 100 } },
            };

            // Act    
            var workout = await _workoutService.AddWorkoutAsync(userIdOne, workoutInput);
            await _workoutService.UpdateWorkoutAsync(userIdOne, workout.Id, workoutInputUpdated);

            var result = await _leaderboardService.GetCalorieLeaderboardRange(0, 99);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task CalorieLeaderboardService_UpdatesCorrectly_OnWorkoutDeleted()
        {
            // Arrange
            string userIdOne = "1";

            var workoutInput = new WorkoutInput
            {
                Name = "Leg Day",
                Description = "A workout focused on the legs",
                StartTime = new DateTimeOffset(2023, 03, 15, 12, 0, 0, TimeSpan.Zero),
                EndTime = new DateTimeOffset(2023, 03, 15, 13, 0, 0, TimeSpan.Zero),
                Exercises = new List<Exercise>
                {
                    new Exercise { Name = "Squats", Reps = 10, Sets = 3, Weight = 200, Calories = 100 },
                    new Exercise { Name = "Lunges", Reps = 12, Sets = 3, Weight = 150, Calories = 100 }
                }
            };

            List<Dictionary<string, object>> expected = new()
            {
                new Dictionary<string, object>() { { "userId", userIdOne }, { "calories", 0 } },
            };

            // Act
            var workout = await _workoutService.AddWorkoutAsync(userIdOne, workoutInput);

            await _workoutService.DeleteWorkoutAsync(userIdOne, workout.Id);

            var result = await _leaderboardService.GetCalorieLeaderboardRange(0, 99);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
