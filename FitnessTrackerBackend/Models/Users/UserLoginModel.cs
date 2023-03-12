using System.ComponentModel.DataAnnotations;

namespace FitnessTrackerBackend.Models.Authentication
{
    public class UserLoginModel
    {
        [Required]
        public string UsernameOrEmail { get; init; }

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; init; }

        public UserLoginModel(string emailOrUsername, string password)
        {
            UsernameOrEmail = emailOrUsername;
            Password = password;
        }
    }
}
