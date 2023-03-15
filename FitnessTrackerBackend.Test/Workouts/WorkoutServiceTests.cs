using FitnessTrackerBackend.Models.Workouts;
using FitnessTrackerBackend.Services.Workouts;
using FitnessTrackerBackend.Test.Fixtures.Redis;
using System.Text.Json;
using StackExchange.Redis;

namespace FitnessTrackerBackend.Test.Workouts
{
    public class WorkoutServiceTests : IClassFixture<RedisFixture>, IDisposable
    {
        private readonly RedisFixture _redisFixture;
        private readonly IDatabase _redis;
        private readonly WorkoutService _workoutService;

        public WorkoutServiceTests(RedisFixture redisFixture)
        {
            _redisFixture = redisFixture;
            _redis = redisFixture.DB;
            _workoutService = new WorkoutService(_redis);
        }

        public void Dispose()
        {
            _redisFixture.FlushDatabase();

            GC.SuppressFinalize(this);
        }

        #region AddWorkoutToUserAsync

        [Fact]
        public async Task AddWorkoutToUserAsync_AddsWorkoutToRedis()
        {
            // Arrange
            string userId = "test_user_id";
            var workoutInput = new WorkoutInput
            {
                Name = "Leg Day",
                Description = "A workout focused on the legs",
                StartTime = new DateTimeOffset(2023, 03, 15, 12, 0, 0, TimeSpan.Zero),
                EndTime = new DateTimeOffset(2023, 03, 15, 13, 0, 0, TimeSpan.Zero),
                Exercises = new List<Exercise>
                {
                    new Exercise { Name = "Squats", Reps = 10, Sets = 3, Weight = 200, Calories = 100 },
                    new Exercise { Name = "Lunges", Reps = 12, Sets = 3, Weight = 150, Calories = 80 }
                }
            };

            // Act
            var addedWorkout = await _workoutService.AddWorkoutToUserAsync(userId, workoutInput);

            // Assert
            string key = $"workouts:{userId}:{addedWorkout.Id}";
            var hashEntries = await _redis.HashGetAllAsync(key);

            Assert.Equal(6, hashEntries.Length);
            Assert.Equal(await _workoutService.GetUserLastWorkoutId(userId), addedWorkout.Id);

            Assert.Equal(userId, hashEntries.First(e => e.Name == "UserId").Value);
            Assert.Equal(workoutInput.Name, hashEntries.First(e => e.Name == "Name").Value);
            Assert.Equal(workoutInput.Description, hashEntries.First(e => e.Name == "Description").Value);
            Assert.Equal(workoutInput.StartTime.ToString("o"), hashEntries.First(e => e.Name == "StartTime").Value);
            Assert.Equal(workoutInput.EndTime.ToString("o"), hashEntries.First(e => e.Name == "EndTime").Value);
            
            var exercisesJson = hashEntries.First(e => e.Name == "Exercises").Value;
            List<Exercise> deserializedExercises = JsonSerializer.Deserialize<List<Exercise>>(exercisesJson!)!;
            Assert.Equal(workoutInput.Exercises.Count, deserializedExercises.Count);
            Assert.Equal(workoutInput.Exercises.ElementAt(0).Name, deserializedExercises[0].Name);
            Assert.Equal(workoutInput.Exercises.ElementAt(1).Name, deserializedExercises[1].Name);
        }

        #endregion

        #region GetWorkoutByIdAsync

        [Fact]
        public async Task GetWorkoutByIdAsync_ReturnsNull_WhenWorkoutDoesNotExist()
        {
            // Arrange
            var service = new WorkoutService(_redis);
            string userId = "test_user_id";
            string workoutId = "non_existent_workout_id";

            // Act
            var workout = await service.GetWorkoutByIdAsync(userId, workoutId);

            // Assert
            Assert.Null(workout);
        }

        [Fact]
        public async Task GetWorkoutByIdAsync_ReturnsCorrectWorkout_WhenWorkoutExists()
        {
            // Arrange
            string userId = "test_user_id";
            var workoutInput = new WorkoutInput
            {
                Name = "Leg Day",
                Description = "A workout focused on the legs",
                StartTime = new DateTimeOffset(2023, 03, 15, 12, 0, 0, TimeSpan.Zero),
                EndTime = new DateTimeOffset(2023, 03, 15, 13, 0, 0, TimeSpan.Zero),
                Exercises = new List<Exercise>
                {
                    new Exercise { Name = "Squats", Reps = 10, Sets = 3, Weight = 200, Calories = 100 },
                    new Exercise { Name = "Lunges", Reps = 12, Sets = 3, Weight = 150, Calories = 80 }
                }
            };
            var addedWorkout = await _workoutService.AddWorkoutToUserAsync(userId, workoutInput);

            // Act
            var workout = (Workout)(await _workoutService.GetWorkoutByIdAsync(userId, addedWorkout.Id));

            // Assert
            Assert.Equal(userId, workout.UserId);
            Assert.Equal(addedWorkout.Id, workout.Id);
            Assert.Equal(workoutInput.Name, workout.Name);
            Assert.Equal(workoutInput.Description, workout.Description);
            Assert.Equal(workoutInput.StartTime, workout.StartTime);
            Assert.Equal(workoutInput.EndTime, workout.EndTime);
            Assert.Equal(workoutInput.Exercises.Count, workout.Exercises.Count);
            Assert.Equal(workoutInput.Exercises.ElementAt(1).Name, workout.Exercises.Skip(1).First().Name);
            Assert.Equal(workoutInput.Exercises.ElementAt(1).Reps, workout.Exercises.Skip(1).First().Reps);
            Assert.Equal(workoutInput.Exercises.ElementAt(1).Sets, workout.Exercises.Skip(1).First().Sets);
            Assert.Equal(workoutInput.Exercises.ElementAt(1).Weight, workout.Exercises.Skip(1).First().Weight);
            Assert.Equal(workoutInput.Exercises.ElementAt(1).Calories, workout.Exercises.Skip(1).First().Calories);
        }

        #endregion

        #region GetUserLastWorkoutId

        [Fact]
        public async Task GetUserLastWorkoutId_ReturnsNeg1_WhenUserHasNoWorkouts()
        {
            // Arrange
            string userId = "test_user_id";

            // Act
            string workoutId = await _workoutService.GetUserLastWorkoutId(userId);

            // Assert
            Assert.Equal("-1", workoutId);
        }

        [Fact]
        public async Task GetUserLastWorkoutId_Returns1_WhenUserHasOneWorkout()
        {
            // Arrange
            string userId = "test_user_id";
            var workoutInput = new WorkoutInput
            {
                Name = "Leg Day",
                Description = "A workout focused on the legs",
                StartTime = new DateTimeOffset(2023, 03, 15, 12, 0, 0, TimeSpan.Zero),
                EndTime = new DateTimeOffset(2023, 03, 15, 13, 0, 0, TimeSpan.Zero),
                Exercises = new List<Exercise>
                {
                    new Exercise { Name = "Squats", Reps = 10, Sets = 3, Weight = 200, Calories = 100 },
                    new Exercise { Name = "Lunges", Reps = 12, Sets = 3, Weight = 150, Calories = 80 }
                }
            };
            await _workoutService.AddWorkoutToUserAsync(userId, workoutInput);

            // Act
            string workoutId = await _workoutService.GetUserLastWorkoutId(userId);

            // Assert
            Assert.Equal("1", workoutId);
        }

        #endregion
    }
}
