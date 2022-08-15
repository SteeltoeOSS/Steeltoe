// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Connector.CloudFoundry;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Xunit;

namespace Steeltoe.Connector.Test;

public class ServiceInfoCreatorFactoryTest
{
    [Fact]
    public void FactoryThrowsOnNullConfig()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => ServiceInfoCreatorFactory.GetServiceInfoCreator(null));
        Assert.Equal("configuration", exception.ParamName);
    }

    [Fact]
    public void FactoryReturnsDefaultType()
    {
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", string.Empty);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", string.Empty);
        ServiceInfoCreator serviceInfoCreator = ServiceInfoCreatorFactory.GetServiceInfoCreator(new ConfigurationBuilder().AddConnectionStrings().Build());
        Assert.IsType<ServiceInfoCreator>(serviceInfoCreator);
    }

    [Fact]
    public void Factory_ReturnsSameInstance()
    {
        IConfigurationRoot config = new ConfigurationBuilder().Build();

        ServiceInfoCreator inst = ServiceInfoCreatorFactory.GetServiceInfoCreator(config);
        Assert.NotNull(inst);
        ServiceInfoCreator inst2 = ServiceInfoCreatorFactory.GetServiceInfoCreator(config);
        Assert.Same(inst, inst2);
    }

    [Fact]
    public void FactoryReturnsCloudFoundryCreatorForCloudFoundry()
    {
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        ServiceInfoCreator serviceInfoCreator = ServiceInfoCreatorFactory.GetServiceInfoCreator(new ConfigurationBuilder().AddCloudFoundry().Build());
        Assert.IsType<CloudFoundryServiceInfoCreator>(serviceInfoCreator);
    }
}
