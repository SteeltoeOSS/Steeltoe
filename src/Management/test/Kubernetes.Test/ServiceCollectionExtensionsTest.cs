// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Info;
using Xunit;

namespace Steeltoe.Management.Kubernetes.Test;

public sealed class ServiceCollectionExtensionsTest
{
    [Fact]
    public void AddKubernetesInfoContributorAddsContributor()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging();
        services.AddKubernetesInfoContributor();

        using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var contributor = serviceProvider.GetService<IInfoContributor>();
        Assert.NotNull(contributor);
    }

    [Fact]
    public void AddKubernetesActuators()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging();
        services.AddKubernetesActuators();

        using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        IInfoContributor[] contributors = serviceProvider.GetServices<IInfoContributor>().ToArray();
        Assert.Equal(4, contributors.Length);
        Assert.Equal(1, contributors.Count(contributor => contributor.GetType().IsAssignableFrom(typeof(KubernetesInfoContributor))));
    }
}
