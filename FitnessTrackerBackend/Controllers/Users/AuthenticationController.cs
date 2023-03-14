using FitnessTrackerBackend.Models.Authentication;
using FitnessTrackerBackend.Services.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace FitnessTrackerBackend.Controllers.Authentication
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IRedisUsersService _usersService;

        public AuthenticationController(IRedisUsersService usersService)
        {
            _usersService = usersService;
        }

        [HttpPost()]
        public async Task<IActionResult> Registration([FromBody] UserRegistrationModel registration)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _usersService.RegisterUserAsync(registration);
            if (user == null)
            {
                return BadRequest("User with this email/username already exists");
            }

            var token = _usersService.GenerateUserJWTToken(user);
            return Ok(new { token });
        }
    }
}
