// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Stream.Binder;
using Steeltoe.Stream.Binding;
using Steeltoe.Stream.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Stream.StreamsHost
{
#pragma warning disable S3881 // "IDisposable" should be implemented correctly
    public class StreamsHost : IHost
#pragma warning restore S3881 // "IDisposable" should be implemented correctly
    {
        public static IHostBuilder CreateDefaultBuilder<T>() => new StreamsHostBuilder<T>(Host.CreateDefaultBuilder());

        public static IHostBuilder CreateDefaultBuilder<T>(string[] args) => new StreamsHostBuilder<T>(Host.CreateDefaultBuilder(args));

        public StreamsHost(IHost host)
        {
            _host = host;
        }

        public IServiceProvider Services => _host.Services;

        public IHost _host;

        public void Dispose()
        {
            _host.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            var lifecycleProcessor = _host.Services.GetRequiredService<ILifecycleProcessor>();
            lifecycleProcessor.OnRefresh();
            var processor = _host.Services.GetRequiredService<StreamListenerAttributeProcessor>();
            processor.AfterSingletonsInstantiated();
            return _host.StartAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            var lifecycleProcessor = _host.Services.GetRequiredService<ILifecycleProcessor>();
            lifecycleProcessor.Stop();

            // Stop that thing
            return _host.StopAsync();
        }
    }

    public class StreamsHostBuilder<T> : IHostBuilder
    {
        private readonly IHostBuilder _hostBuilder;

        public StreamsHostBuilder(IHostBuilder hostBuilder)
        {
            _hostBuilder = hostBuilder.ConfigureServices(services =>
              {
                  var configuration = services.BuildServiceProvider().GetService<IConfiguration>();

                  services.AddOptions();
                  services.AddLogging((b) =>
                  {
                      b.AddDebug();
                      b.SetMinimumLevel(LogLevel.Trace);
                  });

                  services.AddSingleton<IConfiguration>(configuration);
                  services.AddSingleton<IApplicationContext, GenericApplicationContext>();

                  services.AddStreamConfiguration(configuration);
                  services.AddCoreServices();
                  services.AddIntegrationServices(configuration);
                  services.AddStreamCoreServices(configuration);

                  services.AddBinderServices(configuration);
                  services.AddSourceStreamBinding();
                  services.AddSinkStreamBinding();
                  services.AddEnableBinding<T>();
              });
        }

        public IDictionary<object, object> Properties => _hostBuilder.Properties;

        public IHost Build()
        {
            var host = _hostBuilder.Build();
            return new StreamsHost(host);
        }

        public IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
            => _hostBuilder.ConfigureAppConfiguration(configureDelegate);

        public IHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate)
            => _hostBuilder.ConfigureContainer(configureDelegate);

        public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate)
            => _hostBuilder.ConfigureHostConfiguration(configureDelegate);

        public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
            => _hostBuilder.ConfigureServices(configureDelegate);

        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory)
            => _hostBuilder.UseServiceProviderFactory(factory);

        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory)
            => _hostBuilder.UseServiceProviderFactory(factory);
    }

#pragma warning disable S3881 // "IDisposable" should be implemented correctly

}
