// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;

namespace Steeltoe.Management.Tracing.Test;

public sealed class TracingCoreHostBuilderExtensionsTest : TestBase
{
    [Fact]
    public async Task AddDistributedTracingAspNetCore_ConfiguresExpectedDefaults()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddSingleton(GetConfiguration());
        services.AddLogging();
        services.AddDistributedTracingAspNetCore();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        ValidateServiceCollectionCommon(serviceProvider);
        ValidateServiceContainerCore(serviceProvider);
    }
}
