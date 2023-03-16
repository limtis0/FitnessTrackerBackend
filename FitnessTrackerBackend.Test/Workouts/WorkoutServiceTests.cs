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
            var addedWorkout = await _workoutService.AddWorkoutAsync(userId, workoutInput);

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
            var addedWorkout = await _workoutService.AddWorkoutAsync(userId, workoutInput);

            // Act
            var result = (Workout)(await _workoutService.GetWorkoutByIdAsync(userId, addedWorkout.Id));

            // Assert
            Assert.Equal(userId, result.UserId);
            Assert.Equal(addedWorkout.Id, result.Id);

            Assert.Equal(workoutInput.Name, result.Name);
            Assert.Equal(workoutInput.Description, result.Description);
            Assert.Equal(workoutInput.StartTime, result.StartTime);
            Assert.Equal(workoutInput.EndTime, result.EndTime);

            Assert.Equal(workoutInput.Exercises.Count, result.Exercises.Count);

            for (int i = 0; i < result.Exercises.Count; i++)
            {
                var inputExcercises = workoutInput.Exercises.ElementAt(i);
                var resultExcercises = result.Exercises.ElementAt(i);

                Assert.Equal(inputExcercises.Name, resultExcercises.Name);
                Assert.Equal(inputExcercises.Reps, resultExcercises.Reps);
                Assert.Equal(inputExcercises.Sets, resultExcercises.Sets);
                Assert.Equal(inputExcercises.Weight, resultExcercises.Weight);
                Assert.Equal(inputExcercises.Calories, resultExcercises.Calories);
            }
        }

        #endregion

        #region GetWorkoutsInIdRangeAsync

        [Fact]
        public async Task GetWorkoutsInIdRangeAsync_ReturnsIdIncluded()
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

            for (int i = 0; i < 3; i++)
            {
                await _workoutService.AddWorkoutAsync(userId, workoutInput);
            }

            // Act
            List<Workout> workouts = await _workoutService.GetWorkoutsInIdRangeAsync(userId, 1, 2);

            // Assert
            Assert.Equal(2, workouts.Count);
            Assert.Equal("1", workouts[0].Id);
            Assert.Equal("2", workouts[1].Id);
        }

        [Fact]
        public async Task GetWorkoutsInIdRangeAsync_ClampsRange()
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

            for (int i = 0; i < 4; i++)
            {
                await _workoutService.AddWorkoutAsync(userId, workoutInput);
            }

            // Act
            List<Workout> workouts = await _workoutService.GetWorkoutsInIdRangeAsync(userId, -100, 2000);

            // Assert
            Assert.Equal(4, workouts.Count);
        }

        [Fact]
        public async Task GetWorkoutsInIdRangeAsync_ReturnsEmpty_WhenUserHasNoWorkouts()
        {
            // Arrange
            string userId = "test_user_id";

            // Act
            List<Workout> workouts = await _workoutService.GetWorkoutsInIdRangeAsync(userId, 1, 20);

            // Assert
            Assert.Empty(workouts);
        }

        #endregion

        #region GetLastWorkoutsAsync

        [Fact]
        public async Task GetLastWorkoutsAsync_ReturnsCorrect()
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

            for (int i = 0; i < 10; i++)
            {
                await _workoutService.AddWorkoutAsync(userId, workoutInput);
            }

            // Act
            List<Workout> workouts = await _workoutService.GetLastWorkoutsAsync(userId, 3);

            // Assert
            Assert.Equal(3, workouts.Count);
            Assert.NotEmpty(workouts.Where(w => w.Id == "8"));
            Assert.NotEmpty(workouts.Where(w => w.Id == "9"));
            Assert.NotEmpty(workouts.Where(w => w.Id == "10"));
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
            await _workoutService.AddWorkoutAsync(userId, workoutInput);

            // Act
            string workoutId = await _workoutService.GetUserLastWorkoutId(userId);

            // Assert
            Assert.Equal("1", workoutId);
        }

        #endregion

        #region UpdateWorkoutAsync

        [Fact]
        public async Task UpdateWorkoutAsync_UpdatesExistingWorkout()
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
            var workout = await _workoutService.AddWorkoutAsync(userId, workoutInput);

            var updateInput = new WorkoutInput
            {
                Name = "Arm Day",
                Description = "A workout focused on the arms",
                StartTime = new DateTimeOffset(2023, 03, 15, 12, 0, 0, TimeSpan.Zero),
                EndTime = new DateTimeOffset(2023, 03, 15, 13, 0, 0, TimeSpan.Zero),
                Exercises = new List<Exercise>
                {
                    new Exercise{ Name = "Deadlifts", Reps = 5, Sets = 3, Weight = 120, Calories = 80 },
                    new Exercise{ Name = "Pull-ups", Reps = 10, Sets = 4, Weight = 90, Calories = 20 }
                }
            };
            await _workoutService.AddWorkoutAsync(userId, workoutInput);

            // Act
            var result = (Workout) await _workoutService.UpdateWorkoutAsync(userId, workout.Id, updateInput);

            // Assert
            Assert.Equal(updateInput.Name, result.Name);
            Assert.Equal(updateInput.Description, result.Description);
            Assert.Equal(updateInput.StartTime, result.StartTime);
            Assert.Equal(updateInput.EndTime, result.EndTime);
            Assert.Equal(updateInput.Exercises.Count, result.Exercises.Count);

            for (int i = 0; i < updateInput.Exercises.Count; i++)
            {
                var inputExcercises = updateInput.Exercises.ElementAt(i);
                var resultExcercises = result.Exercises.ElementAt(i);

                Assert.Equal(inputExcercises.Name, resultExcercises.Name);
                Assert.Equal(inputExcercises.Reps, resultExcercises.Reps);
                Assert.Equal(inputExcercises.Sets, resultExcercises.Sets);
                Assert.Equal(inputExcercises.Weight, resultExcercises.Weight);
                Assert.Equal(inputExcercises.Calories, resultExcercises.Calories);
            }
        }

        [Fact]
        public async Task UpdateWorkoutAsync_ReturnsNull_WhenWorkoutDoesNotExists()
        {
            // Arrange
            string userId = "test_user_id";
            string workoutId = "1";
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
            var result = await _workoutService.UpdateWorkoutAsync(userId, workoutId, workoutInput);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region DeleteWorkoutAsync

        [Fact]
        public async Task DeleteWorkoutAsync_DeletesWorkoutFromRedis()
        {
            // Arrange
            var userId = "user1";
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
            var workout = await _workoutService.AddWorkoutAsync(userId, workoutInput);
            string workoutRedisKey = $"workouts:{userId}:{workout.Id}";

            // Act
            var result = await _workoutService.DeleteWorkoutAsync(userId, workout.Id);

            // Assert
            Assert.True(result);
            var exists = await _redis.KeyExistsAsync(workoutRedisKey);
            Assert.False(exists);
        }

        [Fact]
        public async Task DeleteWorkoutAsync_ReturnsFalseIfWorkoutDoesNotExist()
        {
            // Arrange
            var userId = "user1";
            var workoutId = "1";

            // Act
            var result = await _workoutService.DeleteWorkoutAsync(userId, workoutId);

            // Assert
            Assert.False(result);
        }

        #endregion
    }
}
