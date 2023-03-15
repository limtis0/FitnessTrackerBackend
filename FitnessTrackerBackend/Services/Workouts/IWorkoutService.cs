using FitnessTrackerBackend.Models.Workouts;

namespace FitnessTrackerBackend.Test.Workouts
{
    public interface IWorkoutService
    {
        Task<Workout> AddWorkoutAsync(string userId, WorkoutInput workout);
        Task<Workout?> GetWorkoutByIdAsync(string userId, string workoutId);
        Task<Workout?> UpdateWorkoutAsync(string userId, string workoutId, WorkoutInput workout);
        Task<bool> DeleteWorkoutAsync(string userId, string workoutId);

        Task<string> GetUserLastWorkoutId(string userId);
    }
}
