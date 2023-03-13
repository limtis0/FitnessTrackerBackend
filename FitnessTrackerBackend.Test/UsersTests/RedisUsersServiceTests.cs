using FitnessTrackerBackend.Configuration;
using FitnessTrackerBackend.Models.Authentication;
using FitnessTrackerBackend.Services.Authentication;
using FitnessTrackerBackend.Test.Fixtures.Redis;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FitnessTrackerBackend.Test.UsersTests
{
    public class RedisUsersServiceTests : IClassFixture<RedisFixture>, IDisposable
    {
        private readonly RedisFixture _redisFixture;
        private readonly IDatabase _redis;
        private readonly JwtBearerOptionsConfig _jwtBearerOptions;

    private readonly RedisUsersService _usersService;

        public RedisUsersServiceTests(RedisFixture redisFixture)
        {
            _redisFixture = redisFixture;
            _redis = redisFixture.DB;
            _jwtBearerOptions = new JwtBearerOptionsConfig("TestAudience", "TestIssuer", "TEST-SECRET-VERY-VERY-SECURE");
            _usersService = new(_redis, _jwtBearerOptions);
        }

        public void Dispose()
        {
            _redisFixture.FlushDatabase();

            GC.SuppressFinalize(this);
        }

        #region RegisterUserAsync

        [Fact]
        public async Task RegisterUserAsync_ShouldReturnNull_WhenUserAlreadyExists()
        {
            // Arrange
            var registration = new UserRegistrationModel("testuser", "test@example.com", "testpassword");

            await _usersService.RegisterUserAsync(registration);

            // Act
            var result = await _usersService.RegisterUserAsync(registration);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task RegisterUserAsync_ShouldReturnToken_WhenUserDoesNotExist()
        {
            // Arrange
            var registration = new UserRegistrationModel("testuser", "test@example.com", "testpassword");

            // Act
            var result = await _usersService.RegisterUserAsync(registration);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task RegisterUserAsync_ShouldIncrementId()
        {
            // Arrange
            var registration = new UserRegistrationModel("testuser", "test@example.com", "testpassword");

            // Act
            var idBeforeRegistration = await _usersService.GetUserCount();
            var user = await _usersService.RegisterUserAsync(registration);
            var idAfterRegistration = await _usersService.GetUserCount();

            // Assert
            Assert.NotNull(user);
            Assert.NotEqual(idBeforeRegistration, idAfterRegistration);
        }

        #endregion

        #region LoginUserAsync

        [Fact]
        public async Task LoginUserAsync_ReturnsNull_WhenUserDoesNotExist()
        {
            // Arrange
            var login = new UserLoginModel("nonexistentuser", "password");

            // Act
            var user = await _usersService.LoginUserAsync(login);

            // Assert
            Assert.Null(user);
        }

        [Fact]
        public async Task LoginUserAsync_ReturnsNull_WhenPasswordIsIncorrect()
        {
            // Arrange

            var registration = new UserRegistrationModel("testuser", "test@example.com", "testpassword");

            await _usersService.RegisterUserAsync(registration);

            var login = new UserLoginModel(registration.Username, "wrongpassword");

            // Act
            var user = await _usersService.LoginUserAsync(login);

            // Assert
            Assert.Null(user);
        }

        [Fact]
        public async Task LoginUserAsync_ReturnsToken_WhenCredentialsAreCorrect_WithEmail()
        {
            // Arrange
            var registration = new UserRegistrationModel("testuser", "test@example.com", "testpassword");

            await _usersService.RegisterUserAsync(registration);

            var login = new UserLoginModel(registration.Email, registration.Password);

            // Act
            var user = await _usersService.LoginUserAsync(login);

            // Assert
            Assert.NotNull(user);
        }

        [Fact]
        public async Task LoginUserAsync_ReturnsToken_WhenCredentialsAreCorrect_WithUsername()
        {
            // Arrange
            var registration = new UserRegistrationModel("testuser", "test@example.com", "testpassword");

            await _usersService.RegisterUserAsync(registration);

            var login = new UserLoginModel(registration.Username, registration.Password);

            // Act
            var user = await _usersService.LoginUserAsync(login);

            // Assert
            Assert.NotNull(user);
        }

        #endregion

        #region GetUserByIdAsync

        [Fact]
        public async Task GetUserByIdAsync_ReturnsNull_WhenUserDoesNotExist()
        {
            // Arrange
            var userId = "nonexistentuserid";

            // Act
            var user = await _usersService.GetUserByIdAsync(userId);

            // Assert
            Assert.Null(user);
        }

        [Fact]
        public async Task GetUserByIdAsync_ReturnsUser_WhenUserExists()
        {
            // Arrange
            var registration = new UserRegistrationModel("testuser", "test@example.com", "testpassword");

            var user = await _usersService.RegisterUserAsync(registration);

            // Act
            var userById = await _usersService.GetUserByIdAsync(user!.Id);

            // Assert
            Assert.NotNull(userById);
            Assert.Equal(user.Id, userById.Id);
            Assert.Equal(user.Username, userById.Username);
            Assert.Equal(user.Email, userById.Email);
            Assert.Equal(user.PasswordHash, userById.PasswordHash);
        }

        #endregion

        #region GetUserByEmailAsync

        [Fact]
        public async Task GetUserByEmailAsync_ReturnsNull_WhenUserDoesNotExist()
        {
            // Arrange
            var userEmail = "nonexistentuser@email.nx";

            // Act
            var user = await _usersService.GetUserByEmailAsync(userEmail);

            // Assert
            Assert.Null(user);
        }

        [Fact]
        public async Task GetUserByEmailAsync_ReturnsUser_WhenUserExists()
        {
            // Arrange
            var registration = new UserRegistrationModel("testuser", "test@example.com", "testpassword");

            var user = await _usersService.RegisterUserAsync(registration);

            // Act
            var userById = await _usersService.GetUserByEmailAsync(user!.Email);

            // Assert
            Assert.NotNull(userById);
            Assert.Equal(user.Id, userById.Id);
            Assert.Equal(user.Username, userById.Username);
            Assert.Equal(user.Email, userById.Email);
            Assert.Equal(user.PasswordHash, userById.PasswordHash);
        }

        #endregion

        #region GetUserByUsernameAsync

        [Fact]
        public async Task GetUserByUsernameAsync_ReturnsNull_WhenUserDoesNotExist()
        {
            // Arrange
            var username = "idonotexist1998";

            // Act
            var user = await _usersService.GetUserByUsernameAsync(username);

            // Assert
            Assert.Null(user);
        }

        [Fact]
        public async Task GetUserByUsernameAsync_ReturnsUser_WhenUserExists()
        {
            // Arrange
            var registration = new UserRegistrationModel("testuser", "test@example.com", "testpassword");

            var user = await _usersService.RegisterUserAsync(registration);

            // Act
            var userById = await _usersService.GetUserByUsernameAsync(user!.Username);

            // Assert
            Assert.NotNull(userById);
            Assert.Equal(user.Id, userById.Id);
            Assert.Equal(user.Username, userById.Username);
            Assert.Equal(user.Email, userById.Email);
            Assert.Equal(user.PasswordHash, userById.PasswordHash);
        }

        #endregion

        #region GenerateUserJWTToken

        [Fact]
        public async Task GenerateUserJWTToken_ReturnsValidTokenForUser()
        {
            // Arrange
            var registration = new UserRegistrationModel("testuser", "test@example.com", "testpassword");

            var user = (await _usersService.RegisterUserAsync(registration))!;

            // Act
            var token = _usersService.GenerateUserJWTToken(user);
            var handler = new JwtSecurityTokenHandler();
            var valParams = new TokenValidationParameters()
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtBearerOptions.Issuer,
                ValidAudience = _jwtBearerOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtBearerOptions.Secret))
            };

            var claimsPrincipal = handler.ValidateToken(token, valParams, out var validatedToken);

            var jwtValidatedToken = (JwtSecurityToken)validatedToken;

            // Assert
            Assert.Equal(user.Id, claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            Assert.Equal(user.Email, claimsPrincipal.FindFirst(ClaimTypes.Email)!.Value);
            Assert.Equal(user.Username, claimsPrincipal.FindFirst(ClaimTypes.Name)!.Value);
            Assert.Equal(_jwtBearerOptions.Issuer, jwtValidatedToken.Issuer);
            Assert.Equal(_jwtBearerOptions.Audience, jwtValidatedToken.Audiences.First());
            Assert.NotNull(jwtValidatedToken.SignatureAlgorithm);
        }

        [Fact]
        public void GenerateUserJWTToken_ThrowsException_WhenUserIsNull()
        {
            // Arrange
            UserModel? user = null;

            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => _usersService.GenerateUserJWTToken(user!));
        }

        #endregion

        #region GetUserByJWTToken
        
        [Fact]
        public async Task GetUserByJWTToken_ReturnsUser_ForValidToken()
        {
            // Arrange
            var registration = new UserRegistrationModel("testuser", "test@example.com", "testpassword");

            var user = (await _usersService.RegisterUserAsync(registration))!;

            var token = _usersService.GenerateUserJWTToken(user);

            // Act
            var userByToken = (await _usersService.GetUserByJWTToken(token))!;

            // Assert
            Assert.Equal(user.Id, userByToken.Id);
            Assert.Equal(user.Email, userByToken.Email);
            Assert.Equal(user.Username, userByToken.Username);
        }

        [Fact]
        public async Task GetUserByJWTToken_ReturnsNull_ForInValidToken()
        {
            // Arrange
            string token = "INVALID-TOKEN-ASDFGHJKL-QWERTYUIOP-ZXCVBNM";

            // Act

            var userByToken = await _usersService.GetUserByJWTToken(token);

            // Assert
            Assert.Null(userByToken);
        }

        #endregion

        #region RemoveUserAsync

        [Fact]
        public async Task RemoveUserAsync_UserExists_RemovesUserFromRedis()
        {
            // Arrange
            var registration = new UserRegistrationModel("testuser", "test@example.com", "testpassword");

            var user = (await _usersService.RegisterUserAsync(registration))!;

            // Act
            bool result = await _usersService.RemoveUserAsync(user.Id);

            // Assert
            Assert.True(result);
            Assert.Null(await _usersService.GetUserByIdAsync(user.Id));
            Assert.Null(await _usersService.GetUserByEmailAsync(user.Email));
            Assert.Null(await _usersService.GetUserByUsernameAsync(user.Username));
        }

        [Fact]
        public async Task RemoveUserAsync_UserDoesNotExist_ReturnsFalse()
        {
            // Arrange
            string userId = "nonexistentuser";

            // Act
            bool result = await _usersService.RemoveUserAsync(userId);

            // Assert
            Assert.False(result);
        }

        #endregion
    }
}
