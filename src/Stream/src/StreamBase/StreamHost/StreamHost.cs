// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Extensions.Configuration.SpringBoot;
using Steeltoe.Stream.Binding;
using Steeltoe.Stream.Extensions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Stream.StreamHost
{
    public sealed class StreamHost : IHost
    {
        public static IHostBuilder CreateDefaultBuilder<T>() => new StreamsHostBuilder<T>(Host.CreateDefaultBuilder());

        public static IHostBuilder CreateDefaultBuilder<T>(string[] args) => new StreamsHostBuilder<T>(Host.CreateDefaultBuilder(args));

        public StreamHost(IHost host)
        {
            _host = host;
        }

        public IServiceProvider Services => _host.Services;

        private readonly IHost _host;

        public void Dispose()
        {
            _host.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            var lifecycleProcessor = _host.Services.GetRequiredService<ILifecycleProcessor>();
            lifecycleProcessor.OnRefresh();
            var processor = _host.Services.GetRequiredService<StreamListenerAttributeProcessor>();
            processor.Initialize();
            return _host.StartAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            var lifecycleProcessor = _host.Services.GetRequiredService<ILifecycleProcessor>();
            lifecycleProcessor.Stop();

            // Stop that thing
            return _host.StopAsync(cancellationToken);
        }
    }

    public class StreamsHostBuilder<T> : IHostBuilder
    {
        private readonly IHostBuilder _hostBuilder;

        public StreamsHostBuilder(IHostBuilder hostBuilder)
        {
            _hostBuilder = hostBuilder
                                .ConfigureAppConfiguration(cb => cb.AddSpringBootEnv())
                                .ConfigureServices((context, services) => services.AddStreamServices<T>(context.Configuration));
        }

        public IDictionary<object, object> Properties => _hostBuilder.Properties;

        public IHost Build()
        {
            var host = _hostBuilder.Build();
            return new StreamHost(host);
        }

        public IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            _hostBuilder.ConfigureAppConfiguration(configureDelegate);
            return this;
        }

        public IHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate)
        {
            _hostBuilder.ConfigureContainer(configureDelegate);
            return this;
        }

        public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate)
        {
            _hostBuilder.ConfigureHostConfiguration(configureDelegate);
            return this;
        }

        public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
        {
            _hostBuilder.ConfigureServices(configureDelegate);
            return this;
        }

        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory)
        {
            _hostBuilder.UseServiceProviderFactory(factory);
            return this;
        }

        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory)
        {
            _hostBuilder.UseServiceProviderFactory(factory);
            return this;
        }
    }
}
