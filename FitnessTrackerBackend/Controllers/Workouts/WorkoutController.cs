using FitnessTrackerBackend.Models.Workouts;
using FitnessTrackerBackend.Services.Authentication;
using FitnessTrackerBackend.Services.Workouts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitnessTrackerBackend.Controllers.Workouts
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class WorkoutController : ControllerBase
    {
        private readonly IWorkoutService _workoutService;
        private readonly IRedisUsersService _usersService;

        public WorkoutController(IWorkoutService workoutService, IRedisUsersService usersService)
        {
            _workoutService = workoutService;
            _usersService = usersService;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Add([FromBody] WorkoutInput workout)
        {
            string? userId = _usersService.GetUserIdFromAuth(this);

            if (userId is null)
            {
                return Unauthorized();
            }

            var result = await _workoutService.AddWorkoutAsync(userId, workout);

            return Ok(result);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetById(string workoutId)
        {
            string? userId = _usersService.GetUserIdFromAuth(this);

            if (userId is null)
            {
                return Unauthorized();
            }

            var result = await _workoutService.GetWorkoutByIdAsync(userId, workoutId);

            if (result is null)
            {
                return NotFound($"Workout with workoutId={workoutId} can not be found.");
            }

            return Ok(result);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetByIdInRange(int from, int to)
        {
            string? userId = _usersService.GetUserIdFromAuth(this);

            if (userId is null)
            {
                return Unauthorized();
            }

            var result = await _workoutService.GetWorkoutsInIdRangeAsync(userId, from, to);

            return Ok(result);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetLastWorkouts(int amount)
        {
            string? userId = _usersService.GetUserIdFromAuth(this);

            if (userId is null)
            {
                return Unauthorized();
            }

            var result = await _workoutService.GetLastWorkoutsAsync(userId, amount);

            return Ok(result);
        }

        [HttpPut]
        [Authorize]
        public async Task<IActionResult> Update(string workoutId, [FromBody] WorkoutInput workout)
        {
            string? userId = _usersService.GetUserIdFromAuth(this);

            if (userId is null)
            {
                return Unauthorized();
            }

            var result = await _workoutService.UpdateWorkoutAsync(userId, workoutId, workout);

            if (result is null)
            {
                return NotFound($"Workout with workoutId={workoutId} can not be found.");
            }

            return Ok(result);
        }

        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> Delete(string workoutId)
        {
            string? userId = _usersService.GetUserIdFromAuth(this);

            if (userId is null)
            {
                return Unauthorized();
            }

            var result = await _workoutService.DeleteWorkoutAsync(userId, workoutId);

            if (!result)
            {
                return NotFound($"Workout with workoutId={workoutId} can not be found.");
            }

            return Ok();
        }
    }
}
