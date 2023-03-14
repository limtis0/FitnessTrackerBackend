using System.ComponentModel.DataAnnotations;

namespace FitnessTrackerBackend.Models.Authentication
{
    public class UserLoginModel
    {
        [Required]
        public string UsernameOrEmail { get; init; }

        [Required]
        [StringLength(128)]
        public string Password { get; init; }

        public UserLoginModel(string usernameOrEmail, string password)
        {
            UsernameOrEmail = usernameOrEmail;
            Password = password;
        }
    }
}
