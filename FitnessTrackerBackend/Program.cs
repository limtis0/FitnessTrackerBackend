using FitnessTrackerBackend.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Text;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();

        ConfigureSwagger(builder);

        ConfigureRedis(builder);

        ConfigureJWTAuth(builder);

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }

    private static void ConfigureSwagger(WebApplicationBuilder builder)
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
    }

    private static void ConfigureRedis(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton(x => ConnectionMultiplexer.Connect("localhost:6379"));
        builder.Services.AddScoped(x => x.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
    }

    private static void ConfigureJWTAuth(WebApplicationBuilder builder)
    {
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = JWTConfig.audience,
                    ValidAudience = JWTConfig.issuer,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(EnvVars.JWTSecret))
                };
            });
    }
}