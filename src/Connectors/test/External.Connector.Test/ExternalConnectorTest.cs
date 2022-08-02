// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Connector;
using Steeltoe.Connector.Services;
using Xunit;

namespace External.Connector.Test;

public class ExternalConnectorTest
{
    [Fact]
    public void CustomCreatorIsRetrieved()
    {
        IConfigurationRoot config = new ConfigurationBuilder().Build();

        ServiceInfoCreator creator = ServiceInfoCreatorFactory.GetServiceInfoCreator(config);

        Assert.IsType<TestServiceInfoCreator>(creator);
        Assert.Single(creator.ServiceInfos);
    }

    [Fact]
    public void CustomCreatorCanBePresentAndDisabled()
    {
        IConfigurationRoot config = new ConfigurationBuilder().Build();
        Environment.SetEnvironmentVariable("TestServiceInfoCreator", "false");

        ServiceInfoCreator creator = ServiceInfoCreatorFactory.GetServiceInfoCreator(config);

        Assert.IsType<ServiceInfoCreator>(creator);
        Assert.Equal(13, creator.Factories.Count);
        Environment.SetEnvironmentVariable("TestServiceInfoCreator", null);
    }

    [Fact]
    public void CustomCreatorCanUseOwnServiceInfos()
    {
        IConfigurationRoot config = new ConfigurationBuilder().Build();

        IEnumerable<Db2ServiceInfo> serviceInfos = config.GetServiceInfos<Db2ServiceInfo>();

        Assert.Single(serviceInfos);
        Db2ServiceInfo serviceInfo = serviceInfos.First();
        Assert.Equal("test", serviceInfo.Scheme);
        Assert.Equal("test", serviceInfo.Host);
        Assert.Equal("test", serviceInfo.Path);
    }
}
