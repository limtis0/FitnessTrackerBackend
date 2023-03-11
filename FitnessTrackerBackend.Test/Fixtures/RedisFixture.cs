using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace FitnessTrackerBackend.Test.Fixtures
{
    public class RedisFixture : IDisposable
    {
        public RedisFixture()
        {
            // Set up the Redis instance
            var services = new ServiceCollection();
            services.AddSingleton<IConnectionMultiplexer>(provider =>
            {
                var configuration = ConfigurationOptions.Parse("localhost:6379");
                return ConnectionMultiplexer.Connect(configuration);
            });
            ServiceProvider = services.BuildServiceProvider();
        }


        public IServiceProvider ServiceProvider { get; private set; }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }

    [CollectionDefinition("RedisCollection")]
    public class RedisCollection : ICollectionFixture<RedisFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
