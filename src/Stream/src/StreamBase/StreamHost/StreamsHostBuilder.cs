// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Extensions.Configuration.SpringBoot;
using Steeltoe.Stream.Extensions;

namespace Steeltoe.Stream.StreamHost;

public class StreamsHostBuilder<T> : IHostBuilder
{
    private readonly IHostBuilder _hostBuilder;

    public StreamsHostBuilder(IHostBuilder hostBuilder)
    {
        _hostBuilder = hostBuilder.AddSpringBootConfiguration()
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
