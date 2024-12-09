// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common;
using Steeltoe.Logging;

namespace Steeltoe.Management.Tracing.Test;

public sealed class TracingServiceCollectionExtensionsTest
{
    [Fact]
    public async Task AddTracingLogProcessor_RegistersTracingLogProcessor()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationManager());
        services.AddTracingLogProcessor();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        serviceProvider.GetService<IApplicationInstanceInfo>().Should().NotBeNull().And.BeOfType<ApplicationInstanceInfo>();

        IEnumerable<IDynamicMessageProcessor> messageProcessors = serviceProvider.GetServices<IDynamicMessageProcessor>();
        messageProcessors.Should().ContainSingle(messageProcessor => messageProcessor is TracingLogProcessor);
    }
}
