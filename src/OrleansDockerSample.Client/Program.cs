using System;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

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

        private static async Task<int> Main(string[] args)
        {
            Console.WriteLine("Press Ctrl+C to exit...");
            Console.WriteLine();

            // Cancel all tasks on SIGINT (Ctrl+C) and SIGTERM (process exiting).
            var cancellationTokenSource = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += context =>
            {
                cancellationTokenSource.Cancel();
            };
            Console.CancelKeyPress += (sender, e) =>
            {
                cancellationTokenSource.Cancel();
                e.Cancel = true;
            };

            using (var services = new ServiceCollection()
                .AddLogging(builder =>
                {
                    builder.AddConsole(options =>
                    {
                        options.DisableColors = false;
                        options.IncludeScopes = true;
                    });
                })
                .AddSingleton<IConfiguration>(new ConfigurationBuilder().AddEnvironmentVariables().Build())
                .AddSingleton(container =>
                {
                    var configuration = container.GetService<IConfiguration>();
                    var logger = container.GetService<ILogger<IClusterClient>>();

                    var connectionString = configuration["ConnectionString"];
                    IClientBuilder clientBuilder;
                    if (string.IsNullOrWhiteSpace(connectionString))
                    {
                        logger.LogInformation("Using development clustering");
                        clientBuilder = new ClientBuilder()
                            .UseLocalhostClustering(
                                gatewayPort: 30000,
                                serviceId: "OrleansDockerSample",
                                clusterId: "Primary");
                    }
                    else
                    {
                        logger.LogInformation("Using Azure Table clustering");
                        clientBuilder = new ClientBuilder()
                            .Configure<ClusterOptions>(options =>
                            {
                                options.ClusterId = "Primary";
                                options.ServiceId = "OrleansDockerSample";
                            })
                            .UseAzureStorageClustering(options =>
                            {
                                options.ConnectionString = connectionString;
                            });
                    }

                    return clientBuilder
                        .ConfigureLogging(config =>
                        {
                            config.AddConsole(options =>
                            {
                                options.DisableColors = RunningInContainer;
                                options.IncludeScopes = true;
                            });
                        })
                        .Build();
                })
                .BuildServiceProvider())
            {
                var client = services.GetService<IClusterClient>();
                var logger = services.GetService<ILogger<IClusterClient>>();

                try
                {
                    cancellationTokenSource.Token.ThrowIfCancellationRequested();
                    logger.LogInformation("Connecting to the cluster");
                    try
                    {
                        await client.Connect();
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Failed to connect to the cluster");
                        return 1;
                    }

                    logger.LogInformation("Connected to the cluster");

                    var grain = client.GetGrain<ICounterGrain>(Guid.Empty);
                    long value;

                    cancellationTokenSource.Token.ThrowIfCancellationRequested();
                    logger.LogInformation("Getting the initial counter value");
                    try
                    {
                        value = await grain.GetValueAsync();
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Failed to get to the initial counter value");
                        return 1;
                    }

                    logger.LogInformation("Initial counter value: {0:d}", value);

                    cancellationTokenSource.Token.ThrowIfCancellationRequested();
                    logger.LogInformation("Incrementing the counter value (approximately once every second)");
                    try
                    {
                        while (true)
                        {
                            cancellationTokenSource.Token.ThrowIfCancellationRequested();
                            value = await grain.IncrementAndGetValueAsync();

                            logger.LogInformation("Incremented counter value: {0:d}", value);

                            await Task.Delay(1000, cancellationTokenSource.Token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Failed to increment to the counter value");
                        return 1;
                    }
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    await client.Close();
                }
            }

            return 0;
        }
    }
}
