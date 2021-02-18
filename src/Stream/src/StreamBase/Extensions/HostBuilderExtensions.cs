// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Contexts;
using Steeltoe.Stream.StreamsHost;

namespace Steeltoe.Stream.Extensions
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder AddStreamsServices<T>(this IHostBuilder builder)
        {
            return builder.ConfigureServices(services =>
            {
                var configuration = services.BuildServiceProvider().GetService<IConfiguration>();
                services.AddOptions();

                services.AddSingleton<IApplicationContext, GenericApplicationContext>();

                services.AddStreamConfiguration(configuration);
                services.AddCoreServices();
                services.AddIntegrationServices(configuration);
                services.AddStreamCoreServices(configuration);

                services.AddBinderServices(configuration);
                services.AddSourceStreamBinding();
                services.AddSinkStreamBinding();
                services.AddHostedService<StreamsLifeCycleService>();

                services.AddEnableBinding<T>();
            });
        }
    }
}
