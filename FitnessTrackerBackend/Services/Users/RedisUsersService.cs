using FitnessTrackerBackend.Configuration;
using FitnessTrackerBackend.Models.Authentication;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FitnessTrackerBackend.Services.Authentication
{
    public class RedisUsersService : IRedisUsersService
    {
        private readonly IDatabase _redis;

        public RedisUsersService(IDatabase redis)
        {
            _redis = redis;
        }

        public async Task<string?> RegisterUserAsync(UserRegistrationModel registration)
        {
            // Check if user already exists
            if (await GetUserByUsernameAsync(registration.Username) is not null || await GetUserByEmailAsync(registration.Email) is not null)
            {
                return null;
            }

            // Set user ID from Redis increment
            string id = (await _redis.StringIncrementAsync("users:Ids")).ToString();

            // Generate salt and hash password
            string salt = BCrypt.Net.BCrypt.GenerateSalt();
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(registration.Password, salt);

            UserModel user = new(id, registration.Username, registration.Email, hashedPassword);

            // Create hash entries from user object
            var userEntries = new HashEntry[]
            {
                new HashEntry("username", user.Username),
                new HashEntry("email", user.Email),
                new HashEntry("passwordHash", user.PasswordHash)
            };

            // Add user hash to Redis
            await _redis.HashSetAsync("users:byEmail", user.Email, user.Id);
            await _redis.HashSetAsync("users:byUsername", user.Username, user.Id);
            await _redis.HashSetAsync($"users:byId:{user.Id}", userEntries);

            return GenerateUserJWTToken(user);
        }

        public async Task<string?> LoginUserAsync(UserLoginModel login)
        {
            // Get a user by username or email
            var user = await GetUserByUsernameAsync(login.UsernameOrEmail) ?? await GetUserByEmailAsync(login.UsernameOrEmail);

            // Assert user exists
            if (user == null)
            {
                return null;
            }

            // Verify the password is correct
            if (!BCrypt.Net.BCrypt.Verify(login.Password, user.PasswordHash))
            {
                return null;
            }

            return GenerateUserJWTToken(user);
        }

        private static string GenerateUserJWTToken(UserModel user)
        {
            // Create claims for the JWT token
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Username)
            };

            // Generate the JWT token
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(EnvVars.JWTSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: JWTConfig.issuer,
                audience: JWTConfig.audience,
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public Task<bool> RemoveUserAsync(string userId)
        {
            throw new NotImplementedException();
        }

        public Task<UserModel> GetUserByEmailAsync(string email)
        {
            throw new NotImplementedException();
        }

        public Task<UserModel> GetUserByUsernameAsync(string username)
        {
            throw new NotImplementedException();
        }
    }
}
