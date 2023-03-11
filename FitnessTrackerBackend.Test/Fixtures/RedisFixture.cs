using Docker.DotNet.Models;
using Docker.DotNet;
using StackExchange.Redis;
using Microsoft.Extensions.DependencyInjection;

namespace FitnessTrackerBackend.Test.Fixtures
{
    public class RedisFixture : IDisposable
    {
        private readonly DockerClient _dockerClient;
        private readonly string _containerId;
        private readonly ConnectionMultiplexer _redis;

        public RedisFixture()
        {
            // Create a Docker client
            _dockerClient = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine")).CreateClient();

            _containerId = StartRedisContainer("6380").ID;

            _dockerClient.Containers.StartContainerAsync(_containerId, null).GetAwaiter().GetResult();

            // Connect to the Redis container
            _redis = ConnectionMultiplexer.Connect("localhost:6380");

            var services = new ServiceCollection();
            services.AddSingleton<IConnectionMultiplexer>(provider =>
            {
                var configuration = ConfigurationOptions.Parse("localhost:6380");
                return ConnectionMultiplexer.Connect(configuration);
            });
            ServiceProvider = services.BuildServiceProvider();
        }

        private CreateContainerResponse StartRedisContainer(string hostPort)
        {
            // Pull the Redis image
            _dockerClient.Images.CreateImageAsync(new ImagesCreateParameters
            {
                FromImage = "redis",
                Tag = "latest"
            }, null, new Progress<JSONMessage>()).GetAwaiter().GetResult();

            // Start a Redis container
            var createContainerResponse = _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Image = "redis:latest",
                Name = "redis.test",
                HostConfig = new HostConfig
                {
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                {
                    {
                        "6379/tcp",
                        new List<PortBinding>
                        {
                            new PortBinding
                            {
                                HostPort = hostPort
                            }
                        }
                    }
                }
                }
            }).GetAwaiter().GetResult();

            return createContainerResponse;
        }

        public IServiceProvider ServiceProvider { get; private set; }

        public void Dispose()
        {
            // Stop and remove the Redis container
            _dockerClient.Containers.StopContainerAsync(_containerId, new ContainerStopParameters()).GetAwaiter().GetResult();
            _dockerClient.Containers.RemoveContainerAsync(_containerId, new ContainerRemoveParameters()).GetAwaiter().GetResult();

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
