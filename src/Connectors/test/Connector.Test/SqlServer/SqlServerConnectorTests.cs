// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding;
using Steeltoe.Connector.SqlServer;
using Xunit;

namespace Steeltoe.Connector.Test.SqlServer;

public sealed class SqlServerConnectorTests
{
    private const string MultiVcapServicesJson = @"{
  ""csb-azure-mssql"": [
    {
      ""binding_guid"": ""d7e63309-8bd7-4ad9-ac47-eb7cffe1199b"",
      ""binding_name"": null,
      ""credentials"": {
        ""databaseLogin"": ""uSrZkkINKEVkzPAi"",
        ""databaseLoginPassword"": ""SWiGP6NtY~b_lDNUeiNtUjpcxTDgURVwVw~NdNnK_o1Zj4Sebt.Xap8xtBPOd8iR"",
        ""hostname"": ""csb-azsql-a3bbb8e8-7012-42c8-b2a9-fdf937a4fd65.database.windows.net"",
        ""jdbcUrl"": ""jdbc:sqlserver://csb-azsql-a3bbb8e8-7012-42c8-b2a9-fdf937a4fd65.database.windows.net:1433;database=csb-db;user=uSrZkkINKEVkzPAi;password=SWiGP6NtY~b_lDNUeiNtUjpcxTDgURVwVw~NdNnK_o1Zj4Sebt.Xap8xtBPOd8iR;Encrypt=true;TrustServerCertificate=false;HostNameInCertificate=*.database.windows.net;loginTimeout=30"",
        ""jdbcUrlForAuditingEnabled"": ""jdbc:sqlserver://csb-azsql-a3bbb8e8-7012-42c8-b2a9-fdf937a4fd65.database.windows.net:1433;database=csb-db;user=uSrZkkINKEVkzPAi;password=SWiGP6NtY~b_lDNUeiNtUjpcxTDgURVwVw~NdNnK_o1Zj4Sebt.Xap8xtBPOd8iR;Encrypt=true;TrustServerCertificate=false;HostNameInCertificate=*.database.windows.net;loginTimeout=30"",
        ""name"": ""csb-db"",
        ""password"": ""SWiGP6NtY~b_lDNUeiNtUjpcxTDgURVwVw~NdNnK_o1Zj4Sebt.Xap8xtBPOd8iR"",
        ""port"": 1433,
        ""sqlServerFullyQualifiedDomainName"": ""csb-azsql-a3bbb8e8-7012-42c8-b2a9-fdf937a4fd65.database.windows.net"",
        ""sqlServerName"": ""csb-azsql-a3bbb8e8-7012-42c8-b2a9-fdf937a4fd65"",
        ""sqldbName"": ""csb-db"",
        ""sqldbResourceGroup"": ""cotati"",
        ""status"": ""created db csb-db (id: /subscriptions/86fb0197-be70-4ceb-88e3-855615bc1b34/resourceGroups/cotati/providers/Microsoft.Sql/servers/csb-azsql-a3bbb8e8-7012-42c8-b2a9-fdf937a4fd65/databases/csb-db) on server csb-azsql-a3bbb8e8-7012-42c8-b2a9-fdf937a4fd65 (id: /subscriptions/86fb0197-be70-4ceb-88e3-855615bc1b34/resourceGroups/cotati/providers/Microsoft.Sql/servers/csb-azsql-a3bbb8e8-7012-42c8-b2a9-fdf937a4fd65) URL: https://portal.Po/#@b39138ca-3cee-4b4a-a4d6-cd83d9dd62f0/resource/subscriptions/86fb0197-be70-4ceb-88e3-855615bc1b34/resourceGroups/cotati/providers/Microsoft.Sql/servers/csb-azsql-a3bbb8e8-7012-42c8-b2a9-fdf937a4fd65/databases/csb-db"",
        ""uri"": ""mssql://csb-azsql-a3bbb8e8-7012-42c8-b2a9-fdf937a4fd65.database.windows.net:1433/csb-db?encrypt=true&TrustServerCertificate=false&HostNameInCertificate=*.database.windows.net"",
        ""username"": ""uSrZkkINKEVkzPAi""
      },
      ""instance_guid"": ""a3bbb8e8-7012-42c8-b2a9-fdf937a4fd65"",
      ""instance_name"": ""mySqlServerServiceOne"",
      ""label"": ""csb-azure-mssql"",
      ""name"": ""mySqlServerServiceOne"",
      ""plan"": ""small-v2"",
      ""provider"": null,
      ""syslog_drain_url"": null,
      ""tags"": [
        ""azure"",
        ""mssql"",
        ""sqlserver"",
        ""preview""
      ],
      ""volume_mounts"": []
    },
    {
      ""binding_guid"": ""7ac9fdbc-f38a-48f9-831e-138ac1f3b551"",
      ""binding_name"": null,
      ""credentials"": {
        ""databaseLogin"": ""JuMEaXdTcTLZKniX"",
        ""databaseLoginPassword"": ""z_Bs8.VXkYhFxWa_qbxcc~YoJn0FB-.AxK2AOQyU3~ZzlE6PmxGDhVZTmI7jVAn2"",
        ""hostname"": ""csb-azsql-f982a580-5226-4cea-b8d1-6febbc5550f9.database.windows.net"",
        ""jdbcUrl"": ""jdbc:sqlserver://csb-azsql-f982a580-5226-4cea-b8d1-6febbc5550f9.database.windows.net:1433;database=csb-db;user=JuMEaXdTcTLZKniX;password=z_Bs8.VXkYhFxWa_qbxcc~YoJn0FB-.AxK2AOQyU3~ZzlE6PmxGDhVZTmI7jVAn2;Encrypt=true;TrustServerCertificate=false;HostNameInCertificate=*.database.windows.net;loginTimeout=30"",
        ""jdbcUrlForAuditingEnabled"": ""jdbc:sqlserver://csb-azsql-f982a580-5226-4cea-b8d1-6febbc5550f9.database.windows.net:1433;database=csb-db;user=JuMEaXdTcTLZKniX;password=z_Bs8.VXkYhFxWa_qbxcc~YoJn0FB-.AxK2AOQyU3~ZzlE6PmxGDhVZTmI7jVAn2;Encrypt=true;TrustServerCertificate=false;HostNameInCertificate=*.database.windows.net;loginTimeout=30"",
        ""name"": ""csb-db"",
        ""password"": ""z_Bs8.VXkYhFxWa_qbxcc~YoJn0FB-.AxK2AOQyU3~ZzlE6PmxGDhVZTmI7jVAn2"",
        ""port"": 1433,
        ""sqlServerFullyQualifiedDomainName"": ""csb-azsql-f982a580-5226-4cea-b8d1-6febbc5550f9.database.windows.net"",
        ""sqlServerName"": ""csb-azsql-f982a580-5226-4cea-b8d1-6febbc5550f9"",
        ""sqldbName"": ""csb-db"",
        ""sqldbResourceGroup"": ""cotati"",
        ""status"": ""created db csb-db (id: /subscriptions/86fb0197-be70-4ceb-88e3-855615bc1b34/resourceGroups/cotati/providers/Microsoft.Sql/servers/csb-azsql-f982a580-5226-4cea-b8d1-6febbc5550f9/databases/csb-db) on server csb-azsql-f982a580-5226-4cea-b8d1-6febbc5550f9 (id: /subscriptions/86fb0197-be70-4ceb-88e3-855615bc1b34/resourceGroups/cotati/providers/Microsoft.Sql/servers/csb-azsql-f982a580-5226-4cea-b8d1-6febbc5550f9) URL: https://portal.cloud-hostname.com/#@b39138ca-3cee-4b4a-a4d6-cd83d9dd62f0/resource/subscriptions/86fb0197-be70-4ceb-88e3-855615bc1b34/resourceGroups/cotati/providers/Microsoft.Sql/servers/csb-azsql-f982a580-5226-4cea-b8d1-6febbc5550f9/databases/csb-db"",
        ""uri"": ""mssql://csb-azsql-f982a580-5226-4cea-b8d1-6febbc5550f9.database.windows.net:1433/csb-db?encrypt=true&TrustServerCertificate=false&HostNameInCertificate=*.database.windows.net"",
        ""username"": ""JuMEaXdTcTLZKniX""
      },
      ""instance_guid"": ""f982a580-5226-4cea-b8d1-6febbc5550f9"",
      ""instance_name"": ""mySqlServerServiceTwo"",
      ""label"": ""csb-azure-mssql"",
      ""name"": ""mySqlServerServiceTwo"",
      ""plan"": ""small-v2"",
      ""provider"": null,
      ""syslog_drain_url"": null,
      ""tags"": [
        ""azure"",
        ""mssql"",
        ""sqlserver"",
        ""preview""
      ],
      ""volume_mounts"": []
    }
  ]
}";

    private const string SingleVcapServicesJson = @"{
  ""csb-azure-mssql"": [
    {
      ""binding_guid"": ""6b7ee54f-f359-47b7-8392-54169267c5eb"",
      ""binding_name"": null,
      ""credentials"": {
        ""databaseLogin"": ""ESgJXmOLPcPeyiZi"",
        ""databaseLoginPassword"": ""lnaj6xo0cevNSeG1vG3nA71nyU2r9_-R4DNmvB~IpC86UpjiFt3kKTLmbhg5HGje"",
        ""hostname"": ""csb-azsql-3807acc4-9189-421c-937a-f2d329be3f40.database.windows.net"",
        ""jdbcUrl"": ""jdbc:sqlserver://csb-azsql-3807acc4-9189-421c-937a-f2d329be3f40.database.windows.net:1433;database=csb-db;user=ESgJXmOLPcPeyiZi;password=lnaj6xo0cevNSeG1vG3nA71nyU2r9_-R4DNmvB~IpC86UpjiFt3kKTLmbhg5HGje;Encrypt=true;TrustServerCertificate=false;HostNameInCertificate=*.database.windows.net;loginTimeout=30"",
        ""jdbcUrlForAuditingEnabled"": ""jdbc:sqlserver://csb-azsql-3807acc4-9189-421c-937a-f2d329be3f40.database.windows.net:1433;database=csb-db;user=ESgJXmOLPcPeyiZi;password=lnaj6xo0cevNSeG1vG3nA71nyU2r9_-R4DNmvB~IpC86UpjiFt3kKTLmbhg5HGje;Encrypt=true;TrustServerCertificate=false;HostNameInCertificate=*.database.windows.net;loginTimeout=30"",
        ""name"": ""csb-db"",
        ""password"": ""lnaj6xo0cevNSeG1vG3nA71nyU2r9_-R4DNmvB~IpC86UpjiFt3kKTLmbhg5HGje"",
        ""port"": 1433,
        ""sqlServerFullyQualifiedDomainName"": ""csb-azsql-3807acc4-9189-421c-937a-f2d329be3f40.database.windows.net"",
        ""sqlServerName"": ""csb-azsql-3807acc4-9189-421c-937a-f2d329be3f40"",
        ""sqldbName"": ""csb-db"",
        ""sqldbResourceGroup"": ""cotati"",
        ""status"": ""created db csb-db (id: /subscriptions/86fb0197-be70-4ceb-88e3-855615bc1b34/resourceGroups/cotati/providers/Microsoft.Sql/servers/csb-azsql-3807acc4-9189-421c-937a-f2d329be3f40/databases/csb-db) on server csb-azsql-3807acc4-9189-421c-937a-f2d329be3f40 (id: /subscriptions/86fb0197-be70-4ceb-88e3-855615bc1b34/resourceGroups/cotati/providers/Microsoft.Sql/servers/csb-azsql-3807acc4-9189-421c-937a-f2d329be3f40) URL: https://portal.cloud-hostname.com/#@b39138ca-3cee-4b4a-a4d6-cd83d9dd62f0/resource/subscriptions/86fb0197-be70-4ceb-88e3-855615bc1b34/resourceGroups/cotati/providers/Microsoft.Sql/servers/csb-azsql-3807acc4-9189-421c-937a-f2d329be3f40/databases/csb-db"",
        ""uri"": ""mssql://csb-azsql-3807acc4-9189-421c-937a-f2d329be3f40.database.windows.net:1433/csb-db?encrypt=true&TrustServerCertificate=false&HostNameInCertificate=*.database.windows.net"",
        ""username"": ""ESgJXmOLPcPeyiZi""
      },
      ""instance_guid"": ""3807acc4-9189-421c-937a-f2d329be3f40"",
      ""instance_name"": ""mySqlServerService"",
      ""label"": ""csb-azure-mssql"",
      ""name"": ""mySqlServerService"",
      ""plan"": ""small-v2"",
      ""provider"": null,
      ""syslog_drain_url"": null,
      ""tags"": [
        ""azure"",
        ""mssql"",
        ""sqlserver"",
        ""preview""
      ],
      ""volume_mounts"": []
    }
  ]
}";

    [Fact]
    public async Task Binds_options_without_service_bindings()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Steeltoe:Client:SqlServer:mySqlServerServiceOne:ConnectionString"] = "Data Source=localhost;Initial Catalog=db1;UID=user1;PWD=pass1",
            ["Steeltoe:Client:SqlServer:mySqlServerServiceTwo:ConnectionString"] = "Server=localhost;Database=db2;UID=user2;PWD=pass2"
        });

        builder.AddSqlServer();
        builder.Services.Configure<SqlServerOptions>("mySqlServerServiceOne", options => options.ConnectionString += ";Encrypt=false");

        await using WebApplication app = builder.Build();
        var optionsSnapshot = app.Services.GetRequiredService<IOptionsSnapshot<SqlServerOptions>>();

        SqlServerOptions optionsOne = optionsSnapshot.Get("mySqlServerServiceOne");
        optionsOne.ConnectionString.Should().NotBeNull();

        ExtractConnectionStringParameters(optionsOne.ConnectionString).Should().BeEquivalentTo(new List<string>
        {
            "Data Source=localhost",
            "Initial Catalog=db1",
            "User ID=user1",
            "Password=pass1",
            "Encrypt=false"
        }, options => options.WithoutStrictOrdering());

        SqlServerOptions optionsTwo = optionsSnapshot.Get("mySqlServerServiceTwo");
        optionsTwo.ConnectionString.Should().NotBeNull();

        ExtractConnectionStringParameters(optionsTwo.ConnectionString).Should().BeEquivalentTo(new List<string>
        {
            "Data Source=localhost",
            "Initial Catalog=db2",
            "User ID=user2",
            "Password=pass2"
        }, options => options.WithoutStrictOrdering());
    }

    [Fact]
    public async Task Binds_options_with_CloudFoundry_service_bindings()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddCloudFoundryServiceBindings(new StringServiceBindingsReader(MultiVcapServicesJson));

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Steeltoe:Client:SqlServer:mySqlServerServiceOne:ConnectionString"] = "Data Source=localhost;Max Pool Size=50"
        });

        builder.AddSqlServer();

        await using WebApplication app = builder.Build();
        var optionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<SqlServerOptions>>();

        SqlServerOptions optionsOne = optionsMonitor.Get("mySqlServerServiceOne");
        optionsOne.Should().NotBeNull();

        ExtractConnectionStringParameters(optionsOne.ConnectionString).Should().BeEquivalentTo(new List<string>
        {
            "Data Source=csb-azsql-a3bbb8e8-7012-42c8-b2a9-fdf937a4fd65.database.windows.net,1433",
            "Initial Catalog=csb-db",
            "User ID=uSrZkkINKEVkzPAi",
            "Password=SWiGP6NtY~b_lDNUeiNtUjpcxTDgURVwVw~NdNnK_o1Zj4Sebt.Xap8xtBPOd8iR",
            "Max Pool Size=50"
        }, options => options.WithoutStrictOrdering());

        SqlServerOptions optionsTwo = optionsMonitor.Get("mySqlServerServiceTwo");
        optionsTwo.Should().NotBeNull();

        ExtractConnectionStringParameters(optionsTwo.ConnectionString).Should().BeEquivalentTo(new List<string>
        {
            "Data Source=csb-azsql-f982a580-5226-4cea-b8d1-6febbc5550f9.database.windows.net,1433",
            "Initial Catalog=csb-db",
            "User ID=JuMEaXdTcTLZKniX",
            "Password=z_Bs8.VXkYhFxWa_qbxcc~YoJn0FB-.AxK2AOQyU3~ZzlE6PmxGDhVZTmI7jVAn2"
        }, options => options.WithoutStrictOrdering());
    }

    [Fact]
    public async Task Registers_ConnectionFactory()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Steeltoe:Client:SqlServer:mySqlServerServiceOne:ConnectionString"] = "SERVER=localhost;Database=db1;UID=user1;PWD=pass1",
            ["Steeltoe:Client:SqlServer:mySqlServerServiceTwo:ConnectionString"] = "SERVER=localhost;Database=db2;UID=user2;PWD=pass2"
        });

        builder.AddSqlServer();

        await using WebApplication app = builder.Build();

        var connectionFactory = app.Services.GetRequiredService<ConnectionFactory<SqlServerOptions, SqlConnection>>();

        await using SqlConnection connectionOne = connectionFactory.GetNamed("mySqlServerServiceOne").CreateConnection();
        connectionOne.ConnectionString.Should().Be("Data Source=localhost;Initial Catalog=db1;User ID=user1;Password=pass1");

        await using SqlConnection connectionTwo = connectionFactory.GetNamed("mySqlServerServiceTwo").CreateConnection();
        connectionTwo.ConnectionString.Should().Be("Data Source=localhost;Initial Catalog=db2;User ID=user2;Password=pass2");
    }

    [Fact]
    public async Task Registers_HealthContributors()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Steeltoe:Client:SqlServer:mySqlServerServiceOne:ConnectionString"] = "SERVER=localhost;Database=db1;UID=user1;PWD=pass1",
            ["Steeltoe:Client:SqlServer:mySqlServerServiceTwo:ConnectionString"] = "SERVER=localhost;Database=db2;UID=user2;PWD=pass2"
        });

        builder.AddSqlServer();

        await using WebApplication app = builder.Build();

        IHealthContributor[] healthContributors = app.Services.GetServices<IHealthContributor>().ToArray();

        healthContributors.Should().HaveCount(2);
        healthContributors[0].Id.Should().Be("SqlServer-mySqlServerServiceOne");
        healthContributors[1].Id.Should().Be("SqlServer-mySqlServerServiceTwo");
    }

    [Fact]
    public async Task Registers_default_connection_string_when_only_single_server_binding_found()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddCloudFoundryServiceBindings(new StringServiceBindingsReader(SingleVcapServicesJson));

        builder.AddSqlServer();

        await using WebApplication app = builder.Build();

        var connectionFactory = app.Services.GetRequiredService<ConnectionFactory<SqlServerOptions, SqlConnection>>();

        string defaultConnectionString = connectionFactory.GetDefault().Options.ConnectionString;
        defaultConnectionString.Should().NotBeNull();

        string namedConnectionString = connectionFactory.GetNamed("mySqlServerService").Options.ConnectionString;
        namedConnectionString.Should().Be(defaultConnectionString);

        app.Services.GetServices<IHealthContributor>().Should().HaveCount(1);
    }

    [Fact]
    public async Task Registers_default_connection_string_when_only_default_client_binding_found()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Steeltoe:Client:SqlServer:Default:ConnectionString"] = "SERVER=localhost;Database=myDb;UID=myUser;PWD=myPass"
        });

        builder.AddSqlServer();

        await using WebApplication app = builder.Build();

        var connectionFactory = app.Services.GetRequiredService<ConnectionFactory<SqlServerOptions, SqlConnection>>();

        string defaultConnectionString = connectionFactory.GetDefault().Options.ConnectionString;
        defaultConnectionString.Should().NotBeNull();

        app.Services.GetServices<IHealthContributor>().Should().HaveCount(1);
    }

    private static IEnumerable<string> ExtractConnectionStringParameters(string connectionString)
    {
        List<string> entries = new();

        if (connectionString != null)
        {
            foreach (string parameter in connectionString.Split(';'))
            {
                string[] nameValuePair = parameter.Split('=', 2);

                if (nameValuePair.Length == 2)
                {
                    string name = nameValuePair[0];
                    string value = nameValuePair[1];

                    entries.Add($"{name}={value}");
                }
            }
        }

        return entries;
    }
}
