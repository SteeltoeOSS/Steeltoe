// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Connectors.Services;
using Steeltoe.Connectors.SqlServer;
using Steeltoe.Connectors.Test.SqlServer;
using Xunit;

namespace Steeltoe.Connectors.Test;

public class ConnectionStringManagerTest
{
    [Fact]
    public void SqlServerConnectionInfo()
    {
        var cm = new ConnectionStringManager(new ConfigurationBuilder().Build());
        Connection connInfo = cm.Get<SqlServerConnectionInfo>();

        Assert.NotNull(connInfo);
        Assert.Equal("Data Source=localhost,1433;", connInfo.ConnectionString);
        Assert.Equal("SqlServer", connInfo.Name);
    }

    [Fact]
    public void SqlServerConnectionInfo_ByName()
    {
        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", TestHelpers.VcapApplication);
        using var servicesScope = new EnvironmentVariableScope("VCAP_SERVICES", SqlServerTestHelpers.TwoServerVcap);

        var cm = new ConnectionStringManager(new ConfigurationBuilder().AddCloudFoundry().Build());
        Connection connInfo = cm.Get<SqlServerConnectionInfo>("mySqlServerService");

        Assert.NotNull(connInfo);
        Assert.Equal("SqlServer-mySqlServerService", connInfo.Name);
    }

    [Theory]
    [InlineData("sqlserver")]
    public void ConnectionInfoTypeFoundByName(string value)
    {
        var manager = new ConnectionStringManager(new ConfigurationBuilder().Build());
        Assert.StartsWith(value, manager.GetByTypeName(value).Name, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("squirrelQL")]
    [InlineData("anyqueue")]
    public void ConnectionTypeLocatorThrowsOnUnknown(string value)
    {
        var manager = new ConnectionStringManager(new ConfigurationBuilder().Build());
        var exception = Assert.Throws<ConnectorException>(() => manager.GetByTypeName(value));
        Assert.Contains(value, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ConnectionTypeLocatorFindsTypeFromServiceInfo()
    {
        var sqlInfo = new SqlServerServiceInfo("id", "sqlserver://host");
        var manager = new ConnectionStringManager(new ConfigurationBuilder().Build());

        Assert.StartsWith("SqlServer", manager.GetFromServiceInfo(sqlInfo).Name, StringComparison.Ordinal);
    }

    [Fact]
    public void ConnectionTypeLocatorFromInfoThrowsOnUnknown()
    {
        var manager = new ConnectionStringManager(new ConfigurationBuilder().Build());
        var exception = Assert.Throws<ConnectorException>(() => manager.GetFromServiceInfo(new Db2ServiceInfo("id", "http://idk")));
        Assert.Contains("Db2ServiceInfo", exception.Message, StringComparison.Ordinal);
    }
}
