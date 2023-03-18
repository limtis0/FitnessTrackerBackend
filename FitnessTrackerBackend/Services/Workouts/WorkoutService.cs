using FitnessTrackerBackend.Models.Workouts;
using StackExchange.Redis;
using System.Text.Json;

namespace FitnessTrackerBackend.Services.Workouts
{
    public delegate Task OnWorkoutUpdatedDelegate(Workout? oldWorkout, Workout? newWorkout, string userId);

    public class WorkoutService
    {
        private readonly IDatabase _redis;

        public WorkoutService(IDatabase redis)
        {
            _redis = redis;
        }

        private const string UserWorkoutsIdKey = "workouts:{0}:id";  // {0} - UserId

        private static string WorkoutByIdHashKey(string userId, string workoutId)
        {
            return string.Format("workouts:{0}:{1}", userId, workoutId);
        }

        private async Task<bool> WorkoutExistsAsync(string userId, string workoutId)
        {
            return await _redis.KeyExistsAsync(WorkoutByIdHashKey(userId, workoutId));
        }

        public async Task<Workout> AddWorkoutAsync(string userId, WorkoutInput workout)
        {
            string workoutId = await GetUserNextWorkoutId(userId);

            return await SetWorkoutAsync(userId, workoutId, workout);
        }

        public async Task<Workout?> UpdateWorkoutAsync(string userId, string workoutId, WorkoutInput workout)
        {
            return await WorkoutExistsAsync(userId, workoutId) ? await SetWorkoutAsync(userId, workoutId, workout) : null;
        }

        public event OnWorkoutUpdatedDelegate? OnWorkoutUpdated;

        private async Task InvokeOnWorkoutUpdatedAsync(Workout? oldWorkout, Workout? newWorkout, string userId)
        {
            // CONCURRENTLY await every subscriber-task
            if (OnWorkoutUpdated is not null)
            {
                await Task.WhenAll(OnWorkoutUpdated.GetInvocationList().OfType<OnWorkoutUpdatedDelegate>().Select(h => h(oldWorkout, newWorkout, userId)));
            }
        }

        private async Task<Workout> SetWorkoutAsync(string userId, string workoutId, WorkoutInput workout)
        {
            // Invoke OnWorkoutUpdated event
            Workout? oldWorkout = await GetWorkoutByIdAsync(userId, workoutId);

            var newWorkout = new Workout
            {
                Id = workoutId,
                UserId = userId,
                Name = workout.Name,
                Description = workout.Description,
                StartTime = workout.StartTime,
                EndTime = workout.EndTime,
                Exercises = workout.Exercises
            };

            await InvokeOnWorkoutUpdatedAsync(oldWorkout, newWorkout, userId);

            // Add the hash entries to Redis
            var hashEntries = new HashEntry[]
            {
                new HashEntry("UserId", userId),
                new HashEntry("Name", workout.Name),
                new HashEntry("Description", workout.Description),
                new HashEntry("StartTime", workout.StartTime.ToString("o")),
                new HashEntry("EndTime", workout.EndTime.ToString("o")),
                new HashEntry("Exercises", JsonSerializer.Serialize(workout.Exercises.ToList()))
            };

            await _redis.HashSetAsync(WorkoutByIdHashKey(userId, workoutId), hashEntries);

            return newWorkout;
        }

        public async Task<Workout?> GetWorkoutByIdAsync(string userId, string workoutId)
        {
            if (!await WorkoutExistsAsync(userId, workoutId))
            {
                return null;
            }

            // Get the hash key for the workout
            string hashKey = WorkoutByIdHashKey(userId, workoutId);

            // Get all the fields and values of the hash
            HashEntry[] hashEntries = await _redis.HashGetAllAsync(hashKey);

            // Deserialize the Exercises field
            List<Exercise> exercises = JsonSerializer.Deserialize<List<Exercise>>(hashEntries.First(x => x.Name == "Exercises").Value!) ?? new();

            return new Workout
            {
                Id = workoutId,
                UserId = userId,
                Name = hashEntries.First(x => x.Name == "Name").Value!,
                Description = hashEntries.First(x => x.Name == "Description").Value!,
                StartTime = DateTimeOffset.Parse(hashEntries.First(x => x.Name == "StartTime").Value!),
                EndTime = DateTimeOffset.Parse(hashEntries.First(x => x.Name == "EndTime").Value!),
                Exercises = exercises
            };
        }

        public async Task<List<Workout>> GetWorkoutsInIdRangeAsync(string userId, int from, int to)
        {
            List<Workout> workouts = new();

            int lastWorkoutId = int.Parse(await GetUserLastWorkoutId(userId));

            from = Math.Max(from, 1);
            to = Math.Min(to, lastWorkoutId);

            while (from <= to)
            {
                Workout? workout = await GetWorkoutByIdAsync(userId, from.ToString());

                if (workout is not null)
                {
                    workouts.Add((Workout)workout);
                }

                from++;
            }

            return workouts;
        }

        public async Task<List<Workout>> GetLastWorkoutsAsync(string userId, int amount)
        {
            int lastWorkoutId = int.Parse(await GetUserLastWorkoutId(userId));

            return await GetWorkoutsInIdRangeAsync(userId, lastWorkoutId - amount + 1, lastWorkoutId);
        }

        public async Task<bool> DeleteWorkoutAsync(string userId, string workoutId)
        {
            Workout? oldWorkout = await GetWorkoutByIdAsync(userId, workoutId);

            await InvokeOnWorkoutUpdatedAsync(oldWorkout, null, userId);

            return await _redis.KeyDeleteAsync(WorkoutByIdHashKey(userId, workoutId));
        }

        public async Task<string> GetUserLastWorkoutId(string userId)
        {
            string key = string.Format(UserWorkoutsIdKey, userId);
            string? id = await _redis.StringGetAsync(key);

            return id ?? "-1";
        }

        private async Task<string> GetUserNextWorkoutId(string userId)
        {
            string key = string.Format(UserWorkoutsIdKey, userId);
            long id = await _redis.StringIncrementAsync(key);

            return id.ToString();
        }
    }
}
