using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Hosting;

namespace OrleansDockerSample
{
    internal sealed class SiloService : IDisposable
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<SiloService> logger;
        private SemaphoreSlim semaphore;
        private ISiloHost silo;
        private bool stopped;

        public SiloService(IConfiguration configuration, ILogger<SiloService> logger)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.semaphore = new SemaphoreSlim(1, 1);
        }

        public void Dispose()
        {
            if (this.silo != null)
            {
                this.silo.Dispose();
                this.silo = null;
            }

            if (this.semaphore != null)
            {
                this.semaphore.Dispose();
                this.semaphore = null;
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await this.semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (this.stopped)
                {
                    return;
                }

                this.logger.LogInformation("Starting silo");

                var connectionString = this.configuration["ConnectionString"];
                ISiloHostBuilder siloBuilder;
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    this.logger.LogInformation("Using development clustering");
                    siloBuilder = new SiloHostBuilder()
                        .UseLocalhostClustering(
                            siloPort: 11111,
                            gatewayPort: 30000,
                            primarySiloEndpoint: null,
                            serviceId: "OrleansDockerSample",
                            clusterId: "Primary");
                }
                else
                {
                    this.logger.LogInformation("Using Azure Table clustering");
                    siloBuilder = new SiloHostBuilder()
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

                this.silo = siloBuilder
                    .ConfigureLogging(config =>
                    {
                        config.AddConsole(options =>
                        {
                            options.DisableColors = Program.RunningInContainer;
                            options.IncludeScopes = true;
                        });
                    })
                    .Build();
                await this.silo.StartAsync(cancellationToken).ConfigureAwait(false);

                this.logger.LogInformation("Started silo");
            }
            finally
            {
                this.semaphore.Release();
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await this.semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                this.stopped = true;
                if (this.silo == null)
                {
                    return;
                }

                this.logger.LogInformation("Stopping silo");

                await this.silo.StopAsync(cancellationToken).ConfigureAwait(false);
                this.silo = null;

                this.logger.LogInformation("Stopped silo");
            }
            finally
            {
                this.semaphore.Release();
            }
        }
    }
}
