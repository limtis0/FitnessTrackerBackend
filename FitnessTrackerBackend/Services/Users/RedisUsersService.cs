using FitnessTrackerBackend.Configuration;
using FitnessTrackerBackend.Models.Authentication;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

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

            // Add user hash to Redis
            await _redis.HashSetAsync("users:idsByEmail", user.Email, user.Id);
            await _redis.HashSetAsync("users:idsByUsername", user.Username, user.Id);
            await _redis.HashSetAsync($"users:byId", user.Id, JsonSerializer.Serialize(user));

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

        public async Task<UserModel?> GetUserByIdAsync(string userId)
        {
            RedisValue json = await _redis.HashGetAsync($"users:byId", userId);

            if (!json.HasValue)
            {
                return null;
            }


            return JsonSerializer.Deserialize<UserModel>(json!);
        }

        public async Task<UserModel?> GetUserByEmailAsync(string email)
        {
            string? userId = await _redis.HashGetAsync("users:idsByEmail", email);

            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }

            return await GetUserByIdAsync(userId);
        }

        public async Task<UserModel?> GetUserByUsernameAsync(string username)
        {
            string? userId = await _redis.HashGetAsync("users:idsByUsername", username);

            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }

            return await GetUserByIdAsync(userId);
        }

        public async Task<bool> RemoveUserAsync(string userId)
        {
            UserModel? user = await GetUserByIdAsync(userId);

            if (user is null)
            {
                return false;
            }

            bool removeUser = await _redis.HashDeleteAsync($"users:byId", user.Id);
            bool removeEmail = await _redis.HashDeleteAsync("users:idsByEmail", user.Email);
            bool removeUsername = await _redis.HashDeleteAsync("users:idsByUsername", user.Username);

            return removeUser && removeEmail && removeUsername;
        }

        public async Task<string> GetUserCount()
        {
            string? count = await _redis.StringGetAsync("users:Ids");

            return count ?? "0";
        }
    }
}
