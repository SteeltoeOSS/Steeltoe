// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Extensions.Configuration.SpringBoot;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using System;
using System.Collections.Generic;

namespace Steeltoe.Messaging.RabbitMQ.Host
{
    public class RabbitHostBuilder : IHostBuilder
    {
        public IDictionary<object, object> Properties => _hostbuilder.Properties;

        private readonly IHostBuilder _hostbuilder;

        public RabbitHostBuilder(IHostBuilder hostbuilder)
        {
            _hostbuilder = hostbuilder;

            _hostbuilder
                .ConfigureAppConfiguration(configBuilder =>
                {
                    configBuilder.AddSpringBootEnv();
                })
                .ConfigureServices((hostBuilderContext, services) =>
                {
                    var rabbitConfigSection = hostBuilderContext.Configuration.GetSection(RabbitOptions.PREFIX);
                    services.Configure<RabbitOptions>(rabbitConfigSection);

                    services.AddRabbitServices();
                    services.AddRabbitAdmin();
                    services.AddRabbitTemplate();
                });
        }

        public IHost Build()
        {
            var host = _hostbuilder.Build();

            return new RabbitHost(host);
        }

        public IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            _hostbuilder.ConfigureAppConfiguration(configureDelegate);

            return this;
        }

        public IHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate)
        {
            _hostbuilder.ConfigureContainer(configureDelegate);

            return this;
        }

        public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate)
        {
            _hostbuilder.ConfigureHostConfiguration(configureDelegate);

            return this;
        }

        public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
        {
            _hostbuilder.ConfigureServices(configureDelegate);

            return this;
        }

        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory)
        {
            _hostbuilder.UseServiceProviderFactory(factory);

            return this;
        }

        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory)
        {
            _hostbuilder.UseServiceProviderFactory(factory);

            return this;
        }
    }
}
