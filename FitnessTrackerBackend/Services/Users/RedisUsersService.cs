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
        private readonly JwtBearerOptionsConfig _jwtBearerOptions;
        private readonly TokenValidationParameters _tokenValidationParameters;

        private const string UserIdByEmailHashKey = "users:idsByEmail";
        private const string UserIdByUsernameHashKey = "users:idsByUsername";
        private const string UserByIdHashKey = "users:byId";
        private const string UserCountStringKey = "users:Ids";

        public RedisUsersService(IDatabase redis, JwtBearerOptionsConfig jwtBearerOptions)
        {
            _redis = redis;

            _jwtBearerOptions = jwtBearerOptions;

            _tokenValidationParameters = new()
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtBearerOptions.Issuer,
                ValidAudience = _jwtBearerOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtBearerOptions.Secret!))
            };
        }

        public async Task<UserModel?> RegisterUserAsync(UserRegistrationModel registration)
        {
            // Check if user already exists
            if (await GetUserByUsernameAsync(registration.Username) is not null || await GetUserByEmailAsync(registration.Email) is not null)
            {
                return null;
            }

            // Set user ID from Redis increment
            string id = (await _redis.StringIncrementAsync(UserCountStringKey)).ToString();

            // Generate salt and hash password
            string salt = BCrypt.Net.BCrypt.GenerateSalt();
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(registration.Password, salt);

            UserModel user = new(id, registration.Username, registration.Email, hashedPassword);

            // Add user hash to Redis
            await _redis.HashSetAsync(UserIdByEmailHashKey, user.Email, user.Id);
            await _redis.HashSetAsync(UserIdByUsernameHashKey, user.Username, user.Id);
            await _redis.HashSetAsync(UserByIdHashKey, user.Id, JsonSerializer.Serialize(user));

            return user;
        }

        public async Task<UserModel?> LoginUserAsync(UserLoginModel login)
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

            return user;
        }

        public string GenerateUserJWTToken(UserModel user)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            // Create claims for the JWT token
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Username)
            };

            // Generate the JWT token
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtBearerOptions.Secret!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _jwtBearerOptions.Issuer,
                audience: _jwtBearerOptions.Audience,
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<UserModel?> GetUserByJWTToken(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                
                var claimsPrincipal = handler.ValidateToken(token, _tokenValidationParameters, out var validatedToken);
                
                var jwtToken = (JwtSecurityToken)validatedToken;

                string userId = jwtToken.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;

                return await GetUserByIdAsync(userId);
            }
            catch
            {
                return null;
            }
        }

        public async Task<UserModel?> GetUserByIdAsync(string userId)
        {
            RedisValue json = await _redis.HashGetAsync(UserByIdHashKey, userId);

            if (!json.HasValue)
            {
                return null;
            }

            return JsonSerializer.Deserialize<UserModel>(json!);
        }

        public async Task<UserModel?> GetUserByEmailAsync(string email)
        {
            string? userId = await _redis.HashGetAsync(UserIdByEmailHashKey, email);

            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }

            return await GetUserByIdAsync(userId);
        }

        public async Task<UserModel?> GetUserByUsernameAsync(string username)
        {
            string? userId = await _redis.HashGetAsync(UserIdByUsernameHashKey, username);

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

            bool removeUser = await _redis.HashDeleteAsync(UserByIdHashKey, user.Id);
            bool removeEmail = await _redis.HashDeleteAsync(UserIdByEmailHashKey, user.Email);
            bool removeUsername = await _redis.HashDeleteAsync(UserIdByUsernameHashKey, user.Username);

            return removeUser && removeEmail && removeUsername;
        }

        public async Task<string> GetUserCount()
        {
            string? count = await _redis.StringGetAsync(UserCountStringKey);

            return count ?? "0";
        }
    }
}
