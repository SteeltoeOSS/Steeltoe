// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using k8s;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Steeltoe.Common.Kubernetes.Test;

public class ServiceCollectionExtensionsTest
{
    [Fact]
    public void AddKubernetesApplicationInstanceInfo_ThrowsOnNull()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => ServiceCollectionExtensions.AddKubernetesApplicationInstanceInfo(null));
        Assert.Equal("serviceCollection", ex.ParamName);
    }

    [Fact]
    public void AddKubernetesApplicationInstanceInfo_ReplacesExistingAppInfo()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        serviceCollection.RegisterDefaultApplicationInstanceInfo();
        Assert.NotNull(serviceCollection.BuildServiceProvider().GetService<IApplicationInstanceInfo>());

        serviceCollection.AddKubernetesApplicationInstanceInfo();
        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

        IEnumerable<IApplicationInstanceInfo> appInfos = serviceProvider.GetServices<IApplicationInstanceInfo>();
        Assert.Single(appInfos);
        Assert.NotNull(appInfos.FirstOrDefault());
        Assert.IsType<KubernetesApplicationOptions>(appInfos.FirstOrDefault());
    }

    [Fact]
    public void AddKubernetesClient_ThrowsOnNulls()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => ServiceCollectionExtensions.AddKubernetesClient(null));
        Assert.Equal("serviceCollection", ex.ParamName);
    }

    [Fact]
    public void AddKubernetesClient_AddsKubernetesOptionsAndClient()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

        serviceCollection.AddKubernetesClient();
        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        var client = serviceProvider.GetService<IKubernetes>();
        IEnumerable<IApplicationInstanceInfo> appInfos = serviceProvider.GetServices<IApplicationInstanceInfo>();

        Assert.NotNull(client);
        Assert.Single(appInfos);
        Assert.IsType<KubernetesApplicationOptions>(appInfos.First());
        Assert.Equal(Assembly.GetEntryAssembly().GetName().Name, appInfos.First().ApplicationName);
    }
}
