// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER
using Microsoft.Extensions.Configuration;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Xunit;

namespace Steeltoe.Extensions.Configuration.CloudFoundryCore.Test;

public partial class CloudFoundryHostBuilderExtensionsTest
{
    [Fact]
    public void WebApplicationAddCloudFoundryConfiguration_Adds()
    {
        var hostbuilder = TestHelpers.GetTestWebApplicationBuilder();
        hostbuilder.AddCloudFoundryConfiguration();
        var host = hostbuilder.Build();

        var config = host.Services.GetService(typeof(IConfiguration)) as IConfigurationRoot;
        Assert.Contains(config.Providers, provider => provider is CloudFoundryConfigurationProvider);
    }
}
#endif
