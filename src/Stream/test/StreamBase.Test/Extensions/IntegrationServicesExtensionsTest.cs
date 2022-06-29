// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Integration.Extensions;
using Steeltoe.Integration.Support;
using Steeltoe.Integration.Support.Converter;
using Steeltoe.Messaging;
using System.Linq;
using Xunit;

namespace Steeltoe.Stream.Extensions;

public class IntegrationServicesExtensionsTest
{
    [Fact]
    public void AddIntegrationServices_AddsServices()
    {
        var container = new ServiceCollection();
        container.AddOptions();
        container.AddLogging(b => b.AddConsole());
        container.AddIntegrationServices();
        var serviceProvider = container.BuildServiceProvider();

        Assert.NotNull(serviceProvider.GetService<DefaultDatatypeChannelMessageConverter>());
        Assert.NotNull(serviceProvider.GetService<IMessageBuilderFactory>());

        var chans = serviceProvider.GetServices<IMessageChannel>();
        Assert.Equal(2, chans.Count());
    }
}
