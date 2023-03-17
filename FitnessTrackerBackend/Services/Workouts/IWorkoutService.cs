using FitnessTrackerBackend.Models.Workouts;

namespace FitnessTrackerBackend.Services.Workouts
{
    public delegate Task OnWorkoutUpdatedDelegate(Workout? oldWorkout, Workout newWorkout);

    public interface IWorkoutService
    {
        event OnWorkoutUpdatedDelegate? OnWorkoutUpdated;

        Task<Workout> AddWorkoutAsync(string userId, WorkoutInput workout);
        Task<Workout?> GetWorkoutByIdAsync(string userId, string workoutId);
        Task<List<Workout>> GetWorkoutsInIdRangeAsync(string userId, int from, int to);
        Task<List<Workout>> GetLastWorkoutsAsync(string userId, int amount);
        Task<Workout?> UpdateWorkoutAsync(string userId, string workoutId, WorkoutInput workout);
        Task<bool> DeleteWorkoutAsync(string userId, string workoutId);

        Task<string> GetUserLastWorkoutId(string userId);
    }
}
