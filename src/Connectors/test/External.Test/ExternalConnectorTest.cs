// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.TestResources;
using Steeltoe.Connectors;
using Steeltoe.Connectors.Services;
using Xunit;

namespace External.Connectors.Test;

public class ExternalConnectorTest
{
    [Fact]
    public void CustomCreatorIsRetrieved()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        ServiceInfoCreator creator = ServiceInfoCreatorFactory.GetServiceInfoCreator(configurationRoot);

        Assert.IsType<TestServiceInfoCreator>(creator);
        Assert.Single(creator.ServiceInfos);
    }

    [Fact]
    public void CustomCreatorCanBePresentAndDisabled()
    {
        using var scope = new EnvironmentVariableScope("TestServiceInfoCreator", "false");

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        ServiceInfoCreator creator = ServiceInfoCreatorFactory.GetServiceInfoCreator(configurationRoot);

        Assert.IsType<ServiceInfoCreator>(creator);
        Assert.Equal(11, creator.Factories.Count);
    }

    [Fact]
    public void CustomCreatorCanUseOwnServiceInfos()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        IEnumerable<Db2ServiceInfo> serviceInfos = configurationRoot.GetServiceInfos<Db2ServiceInfo>();

        Assert.Single(serviceInfos);
        Db2ServiceInfo serviceInfo = serviceInfos.First();
        Assert.Equal("test", serviceInfo.Scheme);
        Assert.Equal("test", serviceInfo.Host);
        Assert.Equal("test", serviceInfo.Path);
    }
}
