// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common;
using Steeltoe.Common.Kubernetes;
using Xunit;

namespace Steeltoe.Extensions.Configuration.Kubernetes.Test;

public partial class KubernetesHostBuilderExtensionsTest
{
    [Fact]
    public void AddKubernetesConfiguration_WebApplicationBuilder_AddsConfig()
    {
        using var server = new MockKubeApiServer();
        var hostBuilder = TestHelpers.GetTestWebApplicationBuilder();
        hostBuilder.AddKubernetesConfiguration(GetFakeClientSetup(server.Uri.ToString()));

        var host = hostBuilder.Build();
        var config = host.Services.GetService<IConfiguration>() as IConfigurationRoot;
        var appInfo = host.Services.GetServices<IApplicationInstanceInfo>().SingleOrDefault();

        Assert.True(config.Providers.Count(ics => ics.GetType().IsAssignableFrom(typeof(KubernetesConfigMapProvider))) == 2);
        Assert.True(config.Providers.Count(ics => ics.GetType().IsAssignableFrom(typeof(KubernetesSecretProvider))) == 2);
        Assert.IsAssignableFrom<KubernetesApplicationOptions>(appInfo);
    }
}
#endif
