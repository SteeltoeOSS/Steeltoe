// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Connector.RabbitMQ;
using Steeltoe.Messaging.RabbitMQ.Configuration;
using Steeltoe.Messaging.RabbitMQ.Extensions;

namespace Steeltoe.Messaging.RabbitMQ.Host;

public static class RabbitMQHostExtensions
{
    public static void ConfigureRabbitServices(this IServiceCollection services, IConfiguration configuration)
    {
        // IConfigurationSection rabbitConfigSection = configuration.GetSection(RabbitOptions.Prefix);
        // services.Configure<RabbitOptions>(rabbitConfigSection);
        services.AddOptions();
        services.SafeAddRabbitMQConnection(configuration);
        services.AddRabbitConnectionFactory();
        services.ConfigureRabbitOptions(configuration);

        services.AddRabbitServices();
        services.AddRabbitAdmin();
        services.AddRabbitTemplate();
    }

    public static void SafeAddRabbitMQConnection(this IServiceCollection services, IConfiguration configuration)
    {
        var loggerFactory = services.BuildServiceProvider().GetService<ILoggerFactory>();
        ILogger logger = loggerFactory?.CreateLogger("Steeltoe.Stream.Extensions.SafeAddRabbitMQConnection");

        try
        {
            services.AddRabbitMQConnection(configuration);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, ex.Message);
        }
    }
}
