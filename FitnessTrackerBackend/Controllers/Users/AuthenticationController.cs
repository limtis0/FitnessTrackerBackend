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

        [HttpPost]
        public async Task<IActionResult> Registration([FromBody] UserRegistrationModel registration)
        {
            var user = await _usersService.RegisterUserAsync(registration);
            if (user == null)
            {
                return BadRequest("User with this email/username already exists");
            }

            var jwtBearer = _usersService.GenerateUserJWTToken(user);
            return Ok(new { jwtBearer });
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] UserLoginModel login)
        {
            var user = await _usersService.LoginUserAsync(login);
            if (user == null)
            {
                return BadRequest("Invalid credentials");
            }

            var jwtBearer = _usersService.GenerateUserJWTToken(user);
            return Ok(new { jwtBearer });
        }
    }
}
