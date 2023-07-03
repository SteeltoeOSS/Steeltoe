// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Connectors.Test.MySql;
using Xunit;

namespace Steeltoe.Connectors.Test;

public class ConfigurationExtensionsTest
{
    [Fact]
    public void AddConnectionStrings_AddsProvider()
    {
        var configuration = new ConfigurationBuilder();
        configuration.AddConnectionStrings();
        Assert.Contains(configuration.Sources, source => source is ConnectionStringConfigurationSource);
    }

    [Fact]
    public void AddConnectionStrings_GetConnectionString()
    {
        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", TestHelpers.VcapApplication);
        using var servicesScope = new EnvironmentVariableScope("VCAP_SERVICES", MySqlTestHelpers.SingleServerVcap);

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddCloudFoundry().AddConnectionStrings().Build();

        string connStringByName = configurationRoot.GetConnectionString("spring-cloud-broker-db");
        string connStringByType = configurationRoot.GetConnectionString("mysql");

        Assert.Equal("Server=192.168.0.90;Port=3306;Username=Dd6O1BPXUHdrmzbP;Password=7E1LxXnlH2hhlPVt;Database=cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355",
            connStringByName);

        Assert.Equal("Server=192.168.0.90;Port=3306;Username=Dd6O1BPXUHdrmzbP;Password=7E1LxXnlH2hhlPVt;Database=cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355",
            connStringByType);
    }
}
