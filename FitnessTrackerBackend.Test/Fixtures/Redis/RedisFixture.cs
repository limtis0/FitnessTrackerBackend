using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace FitnessTrackerBackend.Test.Fixtures.Redis
{
    public class RedisFixture : IDisposable
    {
        private readonly DockerClient _dockerClient;
        private readonly string _containerId;
        private static int containerCount = 0;

        public RedisFixture()
        {
            string containerPort = (6380 + containerCount++).ToString();

            // Create a Docker client
            _dockerClient = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine")).CreateClient();

            _containerId = StartRedisContainer(containerPort).ID;

            _dockerClient.Containers.StartContainerAsync(_containerId, null).GetAwaiter().GetResult();

            var services = new ServiceCollection();
            services.AddSingleton<IConnectionMultiplexer>(provider =>
            {
                var configuration = ConfigurationOptions.Parse($"localhost:{containerPort}");
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
                Name = $"redis.test.{hostPort}",
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

        public void Dispose()
        {
            // Stop and remove the Redis container
            _dockerClient.Containers.StopContainerAsync(_containerId, new ContainerStopParameters()).GetAwaiter().GetResult();
            _dockerClient.Containers.RemoveContainerAsync(_containerId, new ContainerRemoveParameters()).GetAwaiter().GetResult();

            GC.SuppressFinalize(this);
        }

        public IServiceProvider ServiceProvider { get; private set; }

        public IDatabase DB
        {
            get
            {
                var redis = ServiceProvider.GetService<IConnectionMultiplexer>() ?? throw new ArgumentException("Redis service is not set up, or set up incorrectly");
                return redis.GetDatabase();
            }
        }

        public void FlushDatabase()
        {
            DB.Execute("FLUSHDB");
        }

    }
}
