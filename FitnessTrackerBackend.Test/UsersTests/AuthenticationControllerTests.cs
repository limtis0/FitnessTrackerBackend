using FitnessTrackerBackend.Configuration;
using FitnessTrackerBackend.Controllers.Authentication;
using FitnessTrackerBackend.Models.Authentication;
using FitnessTrackerBackend.Services.Authentication;
using FitnessTrackerBackend.Test.Fixtures.Redis;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace FitnessTrackerBackend.Test.UsersTests
{
    public class AuthenticationControllerTests : IClassFixture<RedisFixture>, IDisposable
    {
        private readonly RedisFixture _redisFixture;
        private readonly IDatabase _redis;
        private readonly RedisUsersService _usersService;
        private readonly AuthenticationController _controller;

        public AuthenticationControllerTests(RedisFixture redisFixture)
        {
            _redisFixture = redisFixture;
            _redis = redisFixture.DB;

            var jwtBearerOptions = new JwtBearerOptionsConfig()
            {
                Audience = "TestAudience",
                Issuer = "TestIssuer",
                Secret = "TEST-SECRET-VERY-VERY-SECURE",
            };

            _usersService = new(_redis, jwtBearerOptions);
            _controller = new(_usersService);
        }

        public void Dispose()
        {
            _redisFixture.FlushDatabase();

            GC.SuppressFinalize(this);
        }


        [Fact]
        public async Task Registration_ReturnsBadRequest_WhenUserAlreadyExists()
        {
            // Arrange
            var registration = new UserRegistrationModel("testuser", "test@example.com", "testpassword");
            await _usersService.RegisterUserAsync(registration);
            
            // Act
            var result = await _controller.Registration(registration);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("User with this email/username already exists", badRequestResult.Value);
        }

        [Fact]
        public async Task Registration_ReturnsOkResult_WhenUserDoesNotExist()
        {
            // Arrange
            var registration = new UserRegistrationModel("testuser", "test@example.com", "testpassword");

            // Act
            var result = await _controller.Registration(registration);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<Dictionary<string, string>>(okResult.Value);
            Assert.Contains("jwtBearer", response.Keys);
        }

        [Fact]
        public async Task Login_ReturnsBadRequest_WhenCredentialsAreInvalid_AndUserDoesNotExist()
        {
            // Arrange
            var login = new UserLoginModel("test@example.com", "password");

            // Act
            var result = await _controller.Login(login);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid credentials", badRequestResult.Value);
        }

        [Fact]
        public async Task Login_ReturnsBadRequest_WhenCredentialsAreInvalid_AndUserExist()
        {
            // Arrange
            var registration = new UserRegistrationModel("testuser", "test@example.com", "testpassword");
            await _usersService.RegisterUserAsync(registration);

            var login = new UserLoginModel("testuser", "invalidpassword");

            // Act
            var result = await _controller.Login(login);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid credentials", badRequestResult.Value);
        }

        [Fact]
        public async Task Login_ReturnsOkResult_WhenCredentialsAreValid()
        {
            // Arrange
            var registration = new UserRegistrationModel("testuser", "test@example.com", "testpassword");
            await _usersService.RegisterUserAsync(registration);

            var login = new UserLoginModel("test@example.com", "testpassword");

            // Act
            var result = await _controller.Login(login);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<Dictionary<string, string>>(okResult.Value);
            Assert.Contains("jwtBearer", response.Keys);
        }
    }
}