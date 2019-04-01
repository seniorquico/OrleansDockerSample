using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Statistics;

namespace OrleansDockerSample
{
    internal static class Program
    {
        internal static bool RunningInContainer
        {
            get
            {
                var runningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
                return "true".Equals(runningInContainer, StringComparison.OrdinalIgnoreCase) || "1".Equals(runningInContainer, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            return new HostBuilder()
                .UseOrleans(builder =>
                {
                    var connectionString = configuration["ConnectionString"];
                    if (string.IsNullOrWhiteSpace(connectionString))
                    {
                        builder
                            .UseLocalhostClustering(
                                siloPort: 11111,
                                gatewayPort: 30000,
                                primarySiloEndpoint: null,
                                serviceId: "OrleansDockerSample",
                                clusterId: "Primary");
                    }
                    else
                    {
                        builder
                            .Configure<ClusterOptions>(options =>
                            {
                                options.ClusterId = "Primary";
                                options.ServiceId = "OrleansDockerSample";
                            })
                            .UseAzureStorageClustering(options =>
                            {
                                options.ConnectionString = connectionString;
                            })
                            .ConfigureEndpoints(siloPort: 11111, gatewayPort: 30000);
                    }

                    builder
                        .Configure<LoadSheddingOptions>(options =>
                        {
                            options.LoadSheddingEnabled = true;
                            options.LoadSheddingLimit = 90;
                        })
                        .UseLinuxEnvironmentStatistics();
                })
                .ConfigureLogging(builder =>
                {
                    builder.AddConsole(options =>
                    {
                        options.DisableColors = RunningInContainer;
                        options.IncludeScopes = true;
                    });
                })
                .RunConsoleAsync();
        }
    }
}
