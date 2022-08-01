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
        var config = new ConfigurationBuilder().Build();

        var creator = ServiceInfoCreatorFactory.GetServiceInfoCreator(config);

        Assert.IsType<TestServiceInfoCreator>(creator);
        Assert.Single(creator.ServiceInfos);
    }

    [Fact]
    public void CustomCreatorCanBePresentAndDisabled()
    {
        var config = new ConfigurationBuilder().Build();
        Environment.SetEnvironmentVariable("TestServiceInfoCreator", "false");

        var creator = ServiceInfoCreatorFactory.GetServiceInfoCreator(config);

        Assert.IsType<ServiceInfoCreator>(creator);
        Assert.Equal(13, creator.Factories.Count);
        Environment.SetEnvironmentVariable("TestServiceInfoCreator", null);
    }

    [Fact]
    public void CustomCreatorCanUseOwnServiceInfos()
    {
        var config = new ConfigurationBuilder().Build();

        var serviceInfos = config.GetServiceInfos<Db2ServiceInfo>();

        Assert.Single(serviceInfos);
        var serviceInfo = serviceInfos.First();
        Assert.Equal("test", serviceInfo.Scheme);
        Assert.Equal("test", serviceInfo.Host);
        Assert.Equal("test", serviceInfo.Path);
    }
}
