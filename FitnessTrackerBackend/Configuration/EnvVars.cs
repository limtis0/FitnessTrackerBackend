namespace FitnessTrackerBackend.Configuration
{
    public static class EnvVars
    {
        public static readonly string JWTSecret;

        static EnvVars()
        {
            bool isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

            // TODO: Manage to get EnvVars working in tests
            JWTSecret = isDevelopment ? "MY-VERY-SECRET-KEY-FOR-DEVELOPMENT" : "MY-VERY-SECRET-KEY-FOR-DEVELOPMENT"; //GetEnvVarOrThrowError("JWT_SECRET");
        }

        private static string GetEnvVarOrThrowError(string envVarName)
        {
            string? env = Environment.GetEnvironmentVariable(envVarName);

            if (string.IsNullOrEmpty(env))
            {
                throw new InvalidOperationException($"The {envVarName} environment variable is not set.");
            }

            return env;
        }
    }
}
