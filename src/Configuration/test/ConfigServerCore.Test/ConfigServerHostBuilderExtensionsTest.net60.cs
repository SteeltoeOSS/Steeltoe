// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Steeltoe.Extensions.Configuration.ConfigServer;
using Xunit;

namespace Steeltoe.Extensions.Configuration.ConfigServerCore.Test;

public partial class ConfigServerHostBuilderExtensionsTest
{
    [Fact]
    public void AddConfigServer_WebApplicationBuilder_AddsConfigServer()
    {
        var hostBuilder = TestHelpers.GetTestWebApplicationBuilder();
        hostBuilder.AddConfigServer();
        var host = hostBuilder.Build();

        var config = host.Services.GetService<IConfiguration>() as IConfigurationRoot;
        Assert.Single(config.Providers.OfType<CloudFoundryConfigurationProvider>());
        Assert.Single(config.Providers.OfType<ConfigServerConfigurationProvider>());
    }
}
#endif
