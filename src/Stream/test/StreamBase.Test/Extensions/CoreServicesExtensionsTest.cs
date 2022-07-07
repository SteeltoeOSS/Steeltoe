// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Converter;
using Steeltoe.Common.Lifecycle;
using Xunit;

namespace Steeltoe.Stream.Extensions;

public class CoreServicesExtensionsTest
{
    [Fact]
    public void AddCoreServices_AddsServices()
    {
        var container = new ServiceCollection();
        container.AddOptions();
        container.AddLogging(b => b.AddConsole());
        var config = new ConfigurationBuilder().Build();
        container.AddSingleton<IConfiguration>(config);
        container.AddCoreServices();
        var serviceProvider = container.BuildServiceProvider();
        Assert.NotNull(serviceProvider.GetService<IApplicationContext>());
        Assert.NotNull(serviceProvider.GetService<IConversionService>());
        Assert.NotNull(serviceProvider.GetService<ILifecycleProcessor>());
    }
}
