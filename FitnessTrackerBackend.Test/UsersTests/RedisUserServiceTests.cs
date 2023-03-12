using FitnessTrackerBackend.Models.Authentication;
using FitnessTrackerBackend.Services.Authentication;
using FitnessTrackerBackend.Test.Fixtures;
using StackExchange.Redis;


namespace FitnessTrackerBackend.Test.UsersTests
{
    [Collection("RedisCollection")]
    public class RedisUserServiceTests
    {
        private readonly IDatabase _redis;

        public RedisUserServiceTests(RedisFixture redisFixture)
        {
            if (redisFixture.ServiceProvider.GetService(typeof(IConnectionMultiplexer)) is not IConnectionMultiplexer redis)
            {
                throw new ArgumentException("Redis service is not set up, or set up incorrectly");
            }
            _redis = redis.GetDatabase();
        }

        [Fact]
        public async Task RegisterUserAsync_ShouldReturnNull_WhenUserAlreadyExists()
        {
            // Arrange
            var registration = new UserRegistrationModel("testuser", "test@example.com", "testpassword");
            var usersService = new RedisUsersService(_redis);

            await usersService.RegisterUserAsync(registration);

            // Act
            var result = await usersService.RegisterUserAsync(registration);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task RegisterUserAsync_ShouldReturnToken_WhenUserDoesNotExist()
        {
            // Arrange
            var registration = new UserRegistrationModel("testuser", "test@example.com", "testpassword");
            var usersService = new RedisUsersService(_redis);

            // Act
            var result = await usersService.RegisterUserAsync(registration);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task RegisterUserAsync_ShouldIncrementId()
        {
            // Arrange
            var userService = new RedisUsersService(_redis);
            var registration = new UserRegistrationModel("testuser", "test@example.com", "testpassword");

            // Act
            var idBeforeRegistration = await userService.GetUserCount();
            var token = await userService.RegisterUserAsync(registration);
            var idAfterRegistration = await userService.GetUserCount();

            // Assert
            Assert.NotNull(token);
            Assert.NotEqual(idBeforeRegistration, idAfterRegistration);
        }
    }
}