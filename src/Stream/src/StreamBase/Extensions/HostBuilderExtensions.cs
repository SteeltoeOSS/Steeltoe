// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Contexts;
using Steeltoe.Connector.RabbitMQ;
using Steeltoe.Extensions.Configuration.SpringBoot;
using Steeltoe.Integration.Extensions;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Stream.StreamsHost;
using System;

namespace Steeltoe.Stream.Extensions
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder AddStreamsServices<T>(this IHostBuilder builder)
        {
            return builder
                .ConfigureAppConfiguration(cb => cb.AddSpringBootEnv())
                .ConfigureServices((context, services) =>
                {
                    services.AddStreamServices<T>(context.Configuration);
                    services.AddHostedService<StreamsLifeCycleService>();
                });
        }
    }
}
