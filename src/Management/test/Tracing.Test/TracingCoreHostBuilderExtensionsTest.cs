// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Steeltoe.Management.Tracing.Test;

public sealed class TracingCoreHostBuilderExtensionsTest : TestBase
{
    [Fact]
    public void AddDistributedTracingAspNetCore_ConfiguresExpectedDefaults()
    {
        IServiceCollection services = new ServiceCollection().AddSingleton(GetConfiguration()).AddLogging();

        ServiceProvider serviceProvider = services.AddDistributedTracingAspNetCore().BuildServiceProvider();

        ValidateServiceCollectionCommon(serviceProvider);
        ValidateServiceContainerCore(serviceProvider);
    }
}
