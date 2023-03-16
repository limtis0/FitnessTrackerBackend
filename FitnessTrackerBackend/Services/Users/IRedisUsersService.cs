using FitnessTrackerBackend.Models.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace FitnessTrackerBackend.Services.Authentication
{
    public interface IRedisUsersService
    {
        Task<UserModel?> RegisterUserAsync(UserRegistrationModel registration);
        Task<UserModel?> LoginUserAsync(UserLoginModel login);

        string GenerateUserJWTToken(UserModel user);
        Task<UserModel?> GetUserByJWTToken(string token);

        string? GetUserIdFromAuth(ControllerBase controller);
        Task<UserModel?> GetUserFromAuth(ControllerBase controller);

        Task<UserModel?> GetUserByIdAsync(string userId);
        Task<UserModel?> GetUserByEmailAsync(string email);
        Task<UserModel?> GetUserByUsernameAsync(string username);

        Task<bool> RemoveUserAsync(string userId);

        Task<string> GetUserCount();
    }
}
