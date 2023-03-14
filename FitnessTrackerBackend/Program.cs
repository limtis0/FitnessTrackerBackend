using FitnessTrackerBackend.Configuration;
using FitnessTrackerBackend.Services.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
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

        ConfigureCustomServices(builder);

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
        var config = builder.Configuration.GetSection("RedisConnection").Get<RedisConnectionConfig>()
            ?? throw new Exception("'RedisConnection' section not found in 'appsettings.json'"); ;

        builder.Services.AddSingleton<IConnectionMultiplexer>(provider => ConnectionMultiplexer.Connect(config.ConnectionString!));  // Add IConnectionMultiplexer service to DI
        builder.Services.AddSingleton(provider => provider.GetRequiredService<IConnectionMultiplexer>().GetDatabase());  // Add IDatabase service to DI
    }

    private static void ConfigureJWTAuth(WebApplicationBuilder builder)
    {
        var config = builder.Configuration.GetSection("JwtBearerOptions");

        builder.Services.Configure<JwtBearerOptionsConfig>(config);

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwtBearerOptions = config.Get<JwtBearerOptionsConfig>()
                    ?? throw new Exception("'JwtBearerOptions' section not found in 'appsettings.json'");
                
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtBearerOptions.Issuer!,
                    ValidAudience = jwtBearerOptions.Audience!,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtBearerOptions.Secret!))
                };
            });
    }

    private static void ConfigureCustomServices(WebApplicationBuilder builder)
    {
        // Add IRedisUserService singletone to DI
        builder.Services.AddSingleton<IRedisUsersService, RedisUsersService>(provider =>
        {
            var redis = provider.GetRequiredService<IDatabase>();
            var jwtBearerOptions = provider.GetRequiredService<IOptions<JwtBearerOptionsConfig>>().Value;

            return new RedisUsersService(redis, jwtBearerOptions);
        });
    }
}