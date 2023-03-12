using FitnessTrackerBackend.Models.Authentication;

namespace FitnessTrackerBackend.Services.Authentication
{
    public interface IRedisUsersService
    {
        Task<string?> RegisterUserAsync(UserRegistrationModel registration);
        Task<string?> LoginUserAsync(UserLoginModel login);
        Task<UserModel?> GetUserByIdAsync(string userId);
        Task<UserModel?> GetUserByEmailAsync(string email);
        Task<UserModel?> GetUserByUsernameAsync(string username);
        Task<bool> RemoveUserAsync(string userId);
        Task<string> GetUserCount();
    }
}
