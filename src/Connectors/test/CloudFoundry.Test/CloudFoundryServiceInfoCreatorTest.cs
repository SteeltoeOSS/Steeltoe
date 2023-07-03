// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Configuration.CloudFoundry;
using Xunit;

namespace Steeltoe.Connectors.CloudFoundry.Test;

public class CloudFoundryServiceInfoCreatorTest
{
    [Fact]
    public void Constructor_ThrowsIfConfigNull()
    {
        const IConfiguration configuration = null;

        var ex = Assert.Throws<ArgumentNullException>(() => CloudFoundryServiceInfoCreator.Instance(configuration));
        Assert.Contains(nameof(configuration), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_ReturnsInstance()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        var inst = CloudFoundryServiceInfoCreator.Instance(configuration);
        Assert.NotNull(inst);
    }

    [Fact]
    public void Constructor_ReturnsNewInstance()
    {
        IConfiguration configuration1 = new ConfigurationBuilder().Build();
        IConfiguration configuration2 = new ConfigurationBuilder().Build();

        var inst = CloudFoundryServiceInfoCreator.Instance(configuration1);
        Assert.NotNull(inst);

        var inst2 = CloudFoundryServiceInfoCreator.Instance(configuration2);
        Assert.NotSame(inst, inst2);
    }

    [Fact]
    public void BuildServiceInfoFactories_BuildsExpected()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        var inst = CloudFoundryServiceInfoCreator.Instance(configuration);
        Assert.NotNull(inst);
        Assert.NotNull(inst.Factories);
        Assert.NotEmpty(inst.Factories);
    }

    [Fact]
    public void BuildServiceInfos_NoCloudFoundryServices_BuildsExpected()
    {
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        var creator = CloudFoundryServiceInfoCreator.Instance(configurationRoot);

        Assert.NotNull(creator.ServiceInfos);
        Assert.Equal(0, creator.ServiceInfos.Count);
    }
}
