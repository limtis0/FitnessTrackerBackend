﻿using FitnessTrackerBackend.Models.Workouts;
using FitnessTrackerBackend.Test.Workouts;
using StackExchange.Redis;
using System.Text.Json;

namespace FitnessTrackerBackend.Services.Workouts
{
    public class WorkoutService : IWorkoutService
    {
        private readonly IDatabase _redis;

        public WorkoutService(IDatabase redis)
        {
            _redis = redis;
        }

        private const string UserWorkoutsIdKey = "workouts:{0}:id";  // {0} - UserId
        private const string WorkoutByIdHashKey = "workouts:{0}:{1}";  // {0} - UserId, {1} - WorkoutId

        private static string WorkoutById(string userId, string workoutId) => string.Format(WorkoutByIdHashKey, userId, workoutId);

        private async Task<bool> WorkoutExistsAsync(string userId, string workoutId) => await _redis.KeyExistsAsync(WorkoutById(userId, workoutId));

        public async Task<Workout> AddWorkoutAsync(string userId, WorkoutInput workout)
        {
            string workoutId = await GetUserNextWorkoutId(userId);

            return await SetWorkoutAsync(userId, workoutId, workout);
        }

        public async Task<Workout?> GetWorkoutByIdAsync(string userId, string workoutId)
        {
            // Get the hash key for the workout
            string hashKey = string.Format(WorkoutByIdHashKey, userId, workoutId);

            // Get all the fields and values of the hash
            HashEntry[] hashEntries = await _redis.HashGetAllAsync(hashKey);

            if (hashEntries.Length == 0)
            {
                // Return null if the workout does not exist
                return null;
            }

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

        public async Task<Workout?> UpdateWorkoutAsync(string userId, string workoutId, WorkoutInput workout)
        {
            return await WorkoutExistsAsync(userId, workoutId) ? await SetWorkoutAsync(userId, workoutId, workout) : null;
        }

        public async Task<bool> DeleteWorkoutAsync(string userId, string workoutId)
        {
            return await _redis.KeyDeleteAsync(WorkoutById(userId, workoutId));
        }

        private async Task<Workout> SetWorkoutAsync(string userId, string workoutId, WorkoutInput workout)
        {
            var hashEntries = new HashEntry[]
            {
                new HashEntry("UserId", userId),
                new HashEntry("Name", workout.Name),
                new HashEntry("Description", workout.Description),
                new HashEntry("StartTime", workout.StartTime.ToString("o")),
                new HashEntry("EndTime", workout.EndTime.ToString("o")),
                new HashEntry("Exercises", JsonSerializer.Serialize(workout.Exercises.ToList()))
            };

            // Add the hash entries to Redis
            string key = string.Format(WorkoutByIdHashKey, userId, workoutId);
            await _redis.HashSetAsync(key, hashEntries);

            return new Workout
            {
                Id = workoutId,
                UserId = userId,
                Name = workout.Name,
                Description = workout.Description,
                StartTime = workout.StartTime,
                EndTime = workout.EndTime,
                Exercises = workout.Exercises
            };
        }

        public async Task<string> GetUserLastWorkoutId(string userId)
        {
            string key = string.Format(UserWorkoutsIdKey, userId);
            string? id = await _redis.StringGetAsync(key);

            return  id ?? "-1";
        }

        private async Task<string> GetUserNextWorkoutId(string userId)
        {
            string key = string.Format(UserWorkoutsIdKey, userId);
            long id = await _redis.StringIncrementAsync(key);

            return id.ToString();
        }
    }
}
