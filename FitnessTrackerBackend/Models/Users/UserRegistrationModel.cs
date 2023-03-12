using System.ComponentModel.DataAnnotations;

namespace FitnessTrackerBackend.Models.Authentication
{
    public class UserRegistrationModel
    {
        [Required]
        [StringLength(32, MinimumLength = 1)]
        public string Username { get; init; }

        [Required]
        [EmailAddress]
        public string Email { get; init; }

        [Required]
        [StringLength(128, MinimumLength = 8)]
        public string Password { get; init; }

        public UserRegistrationModel(string username, string email, string password)
        {
            Username = username;
            Email = email;
            Password = password;
        }
    }
}
