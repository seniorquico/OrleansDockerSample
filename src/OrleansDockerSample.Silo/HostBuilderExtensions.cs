using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;

namespace Orleans.Statistics
{
    /// <summary>
    ///     Temporarily works around limitation described in
    ///     <a href="https://github.com/dotnet/orleans/issues/5497"/>issue #5497</a>.
    /// </summary>
    internal static class HostBuilderExtensions
    {
        public static ISiloBuilder UseLinuxEnvironmentStatistics(this ISiloBuilder builder)
        {
            new SiloBuilderWrapper(builder).UseLinuxEnvironmentStatistics();
            return builder;
        }

        private sealed class SiloBuilderWrapper : ISiloHostBuilder
        {
            private readonly ISiloBuilder builder;

            public SiloBuilderWrapper(ISiloBuilder builder) =>
                this.builder = builder ?? throw new ArgumentNullException(nameof(builder));

            public IDictionary<object, object> Properties =>
                this.builder.Properties;

            public ISiloHost Build() =>
                throw new NotImplementedException();

            public ISiloHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate) =>
                throw new NotImplementedException();

            public ISiloHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate) =>
                throw new NotImplementedException();

            public ISiloHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate) =>
                throw new NotImplementedException();

            public ISiloHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
            {
                this.builder.ConfigureServices(services =>
                {
                    configureDelegate(null, services);
                });
                return this;
            }

            public ISiloHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory) =>
                throw new NotImplementedException();
        }
    }
}
