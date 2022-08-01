// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Connector.MySql.Test;
using Steeltoe.Connector.Redis.Test;
using Steeltoe.Connector.Services;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Xunit;

namespace Steeltoe.Connector.Test;

public class ConfigurationExtensionsTest
{
    [Fact]
    public void GetServiceInfos_GetsCFRedisServiceInfos()
    {
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", RedisCacheTestHelpers.SingleServerVcap);

        var config = new ConfigurationBuilder().AddCloudFoundry().Build();

        var infos = config.GetServiceInfos(typeof(RedisServiceInfo));

        Assert.NotEmpty(infos);
    }

    [Fact]
    public void GetServiceInfos_GetsRedisServiceInfos()
    {
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", string.Empty);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", string.Empty);
        var config = new ConfigurationBuilder().AddInMemoryCollection(RedisCacheTestHelpers.SingleServerAsDictionary).Build();

        var infos = config.GetServiceInfos(typeof(RedisServiceInfo));

        Assert.NotEmpty(infos);
        var si = infos.First() as RedisServiceInfo;
        Assert.Equal(RedisCacheTestHelpers.SingleServerAsDictionary["services:p-redis:0:credentials:host"], si.Host);
        Assert.Equal(RedisCacheTestHelpers.SingleServerAsDictionary["services:p-redis:0:credentials:password"], si.Password);
        Assert.Equal(RedisCacheTestHelpers.SingleServerAsDictionary["services:p-redis:0:credentials:port"], si.Port.ToString());
    }

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
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", MySqlTestHelpers.SingleServerVcap);
        var config = new ConfigurationBuilder().AddCloudFoundry().AddConnectionStrings().Build();

        var connStringByName = config.GetConnectionString("spring-cloud-broker-db");
        var connStringByType = config.GetConnectionString("mysql");

        Assert.Equal("Server=192.168.0.90;Port=3306;Username=Dd6O1BPXUHdrmzbP;Password=7E1LxXnlH2hhlPVt;Database=cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355;", connStringByName);
        Assert.Equal("Server=192.168.0.90;Port=3306;Username=Dd6O1BPXUHdrmzbP;Password=7E1LxXnlH2hhlPVt;Database=cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355;", connStringByType);
    }
}
