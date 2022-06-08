// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Stream.Attributes;
using Steeltoe.Stream.Binder.Rabbit;
using Steeltoe.Stream.Binder.Rabbit.Config;
using Steeltoe.Stream.Binder.Rabbit.Provisioning;

[assembly: Binder("rabbit", typeof(Startup))]

namespace Steeltoe.Stream.Binder.Rabbit;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public bool ConfigureServicesInvoked { get; set; } = false;

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.ConfigureRabbitOptions(Configuration);
        var c = Configuration.GetSection(RabbitBindingsOptions.PREFIX);
        services.Configure<RabbitBindingsOptions>(c);
        services.Configure<RabbitBindingsOptions>(o => o.PostProcess());

        services.Configure<RabbitBinderOptions>(Configuration.GetSection(RabbitBinderOptions.PREFIX));

        services.AddSingleton<IConnectionFactory, CachingConnectionFactory>();
        services.AddSingleton<RabbitExchangeQueueProvisioner>();
        services.AddSingleton<RabbitMessageChannelBinder>();
        services.AddSingleton<IBinder>(p =>
        {
            var logger = p.GetRequiredService<ILogger<RabbitMessageChannelBinder>>();
            var exchangeprov = p.GetRequiredService<RabbitExchangeQueueProvisioner>();
            return p.GetRequiredService<RabbitMessageChannelBinder>();
        });
    }
}
