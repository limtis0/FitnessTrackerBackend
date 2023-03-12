namespace FitnessTrackerBackend.Configuration
{
    public static class EnvVars
    {
        public static readonly string JWTSecret;

        static EnvVars()
        {
            bool isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

            JWTSecret = isDevelopment ? "MY-VERY-SECRET-KEY-FOR-DEVELOPMENT" : GetEnvVarOrGiveWarning("JWT_SECRET", "JWT-SECRET-DEFAULT-VALUE");
        }

        private static string GetEnvVarOrGiveWarning(string envVarName, string defaultValue)
        {
            string? env = Environment.GetEnvironmentVariable(envVarName);

            if (string.IsNullOrEmpty(env))
            {
                Console.WriteLine($"[Warning] The {envVarName} environment variable is not set. Using a default value {defaultValue}.");
            }

            return env ?? defaultValue;
        }
    }
}
