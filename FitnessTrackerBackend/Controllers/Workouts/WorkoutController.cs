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

            return Ok(new Dictionary<string, Workout> { { "result", result } });
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
                return BadRequest($"Workout with workoutId={workoutId} can not be found.");
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
                return BadRequest($"Workout with workoutId={workoutId} can not be found.");
            }

            return Ok(result);
        }
    }
}
