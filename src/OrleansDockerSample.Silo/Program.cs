using System;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

        private static async Task Main(string[] args)
        {
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
                        options.DisableColors = RunningInContainer;
                        options.IncludeScopes = true;
                    });
                })
                .AddSingleton<IConfiguration>(new ConfigurationBuilder().AddEnvironmentVariables().Build())
                .AddSingleton<SiloService>()
                .BuildServiceProvider())
            {
                var service = services.GetService<SiloService>();
                try
                {
                    await service.StartAsync(cancellationTokenSource.Token).ConfigureAwait(false);
                    await Task.Delay(Timeout.Infinite, cancellationTokenSource.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    await service.StopAsync(CancellationToken.None).ConfigureAwait(false);
                }
            }
        }
    }
}
