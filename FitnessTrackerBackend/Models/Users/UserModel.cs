namespace FitnessTrackerBackend.Models.Authentication
{
    public class UserModel
    {
        public string Id { get; init; }
        public string Username { get; init; }
        public string Email { get; init; }
        public string PasswordHash { get; init; }

        public UserModel(string id, string username, string email, string passwordHash)
        {
            Id = id;
            Username = username;
            Email = email;
            PasswordHash = passwordHash;
        }
    }
}
