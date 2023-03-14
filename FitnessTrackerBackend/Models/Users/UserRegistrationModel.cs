using System.ComponentModel.DataAnnotations;

namespace FitnessTrackerBackend.Models.Authentication
{
    public class UserRegistrationModel
    {
        [Required]
        [StringLength(32, MinimumLength = 1)]
        public string Username { get; init; }

        [Required]
        [StringLength(320, MinimumLength = 3)]
        [RegularExpression(@"^[\w!#$%&'*+\-/=?\^_`{|}~]+(\.[\w!#$%&'*+\-/=?\^_`{|}~]+)*" + "@" + @"((([\-\w]+\.)+[a-zA-Z]{2,4})|(([0-9]{1,3}\.){3}[0-9]{1,3}))$", ErrorMessage = "Specified email has invalid format")]
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
