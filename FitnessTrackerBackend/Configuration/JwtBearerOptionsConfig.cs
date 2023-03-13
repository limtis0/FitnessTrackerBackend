namespace FitnessTrackerBackend.Configuration
{
    public class JwtBearerOptionsConfig
    {
        public string Audience { get; set; }
        public string Issuer { get; set; }
        public string Secret { get; set; }

        public JwtBearerOptionsConfig(string audience, string issuer, string secret)
        {
            Audience = audience;
            Issuer = issuer;
            Secret = secret;
        }
    }
}
