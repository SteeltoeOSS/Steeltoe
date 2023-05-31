// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MySqlConnector;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding;
using Steeltoe.Connectors.MySql;
using Steeltoe.Connectors.MySql.DynamicTypeAccess;
using Xunit;

namespace Steeltoe.Connectors.Test.MySql.MySqlConnector;

public sealed class MySqlConnectorTests
{
    private const string MultiVcapServicesJson = @"{
  ""p.mysql"": [
    {
      ""label"": ""p.mysql"",
      ""provider"": null,
      ""plan"": ""db-small"",
      ""name"": ""myMySqlServiceOne"",
      ""tags"": [
        ""mysql""
      ],
      ""instance_guid"": ""566ad428-5747-4b76-89db-bae25c70adae"",
      ""instance_name"": ""myMySqlServiceOne"",
      ""binding_guid"": ""6862f371-181d-4aee-91c4-995015fb2973"",
      ""binding_name"": null,
      ""credentials"": {
        ""hostname"": ""566ad428-5747-4b76-89db-bae25c70adae.mysql.service.internal"",
        ""jdbcUrl"": ""jdbc:mysql://566ad428-5747-4b76-89db-bae25c70adae.mysql.service.internal:3306/service_instance_db?user=6862f371181d4aee91c4995015fb2973&password=q3o5o3o88dyc8os5&useSSL=false"",
        ""name"": ""service_instance_db"",
        ""password"": ""q3o5o3o88dyc8os5"",
        ""port"": 3306,
        ""uri"": ""mysql://6862f371181d4aee91c4995015fb2973:q3o5o3o88dyc8os5@566ad428-5747-4b76-89db-bae25c70adae.mysql.service.internal:3306/service_instance_db?reconnect=true"",
        ""username"": ""6862f371181d4aee91c4995015fb2973""
      },
      ""syslog_drain_url"": null,
      ""volume_mounts"": []
    },
    {
      ""label"": ""p.mysql"",
      ""provider"": null,
      ""plan"": ""db-small"",
      ""name"": ""myMySqlServiceTwo"",
      ""tags"": [
        ""mysql""
      ],
      ""instance_guid"": ""43adf261-6658-4b36-98a5-144ad3cf5ae6"",
      ""instance_name"": ""myMySqlServiceTwo"",
      ""binding_guid"": ""f2537d98-484c-4877-9a68-11b62852b38b"",
      ""binding_name"": null,
      ""credentials"": {
        ""hostname"": ""43adf261-6658-4b36-98a5-144ad3cf5ae6.mysql.service.internal"",
        ""jdbcUrl"": ""jdbc:mysql://43adf261-6658-4b36-98a5-144ad3cf5ae6.mysql.service.internal:3306/service_instance_db?user=f2537d98484c48779a6811b62852b38b&password=rr7t44xnbvvto8b8&useSSL=false"",
        ""name"": ""service_instance_db"",
        ""password"": ""rr7t44xnbvvto8b8"",
        ""port"": 3306,
        ""uri"": ""mysql://f2537d98484c48779a6811b62852b38b:rr7t44xnbvvto8b8@43adf261-6658-4b36-98a5-144ad3cf5ae6.mysql.service.internal:3306/service_instance_db?reconnect=true"",
        ""username"": ""f2537d98484c48779a6811b62852b38b""
      },
      ""syslog_drain_url"": null,
      ""volume_mounts"": []
    }
  ]
}";

    private const string SingleVcapServicesJson = @"{
  ""p.mysql"": [
    {
      ""label"": ""p.mysql"",
      ""provider"": null,
      ""plan"": ""db-small"",
      ""name"": ""myMySqlServiceOne"",
      ""tags"": [
        ""mysql""
      ],
      ""instance_guid"": ""566ad428-5747-4b76-89db-bae25c70adae"",
      ""instance_name"": ""myMySqlServiceOne"",
      ""binding_guid"": ""6862f371-181d-4aee-91c4-995015fb2973"",
      ""binding_name"": null,
      ""credentials"": {
        ""hostname"": ""566ad428-5747-4b76-89db-bae25c70adae.mysql.service.internal"",
        ""jdbcUrl"": ""jdbc:mysql://566ad428-5747-4b76-89db-bae25c70adae.mysql.service.internal:3306/service_instance_db?user=6862f371181d4aee91c4995015fb2973&password=q3o5o3o88dyc8os5&useSSL=false"",
        ""name"": ""service_instance_db"",
        ""password"": ""q3o5o3o88dyc8os5"",
        ""port"": 3306,
        ""uri"": ""mysql://6862f371181d4aee91c4995015fb2973:q3o5o3o88dyc8os5@566ad428-5747-4b76-89db-bae25c70adae.mysql.service.internal:3306/service_instance_db?reconnect=true"",
        ""username"": ""6862f371181d4aee91c4995015fb2973""
      },
      ""syslog_drain_url"": null,
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
            ["Steeltoe:Client:MySql:myMySqlServiceOne:ConnectionString"] = "SERVER=localhost;Database=db1;UID=user1;PWD=pass1",
            ["Steeltoe:Client:MySql:myMySqlServiceTwo:ConnectionString"] = "SERVER=localhost;Database=db2;UID=user2;PWD=pass2"
        });

        builder.AddMySql(MySqlPackageResolver.CreateForOnlyMySqlConnector());
        builder.Services.Configure<MySqlOptions>("myMySqlServiceOne", options => options.ConnectionString += ";Use Compression=false");

        await using WebApplication app = builder.Build();
        var optionsSnapshot = app.Services.GetRequiredService<IOptionsSnapshot<MySqlOptions>>();

        MySqlOptions optionsOne = optionsSnapshot.Get("myMySqlServiceOne");

        ExtractConnectionStringParameters(optionsOne.ConnectionString).Should().BeEquivalentTo(new List<string>
        {
            "Server=localhost",
            "Database=db1",
            "User ID=user1",
            "Use Compression=false",
            "Password=pass1"
        }, options => options.WithoutStrictOrdering());

        MySqlOptions optionsTwo = optionsSnapshot.Get("myMySqlServiceTwo");

        ExtractConnectionStringParameters(optionsTwo.ConnectionString).Should().BeEquivalentTo(new List<string>
        {
            "Server=localhost",
            "Database=db2",
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
            ["Steeltoe:Client:MySql:myMySqlServiceOne:ConnectionString"] = "Connection Timeout=15;host=localhost"
        });

        builder.AddMySql(MySqlPackageResolver.CreateForOnlyMySqlConnector());

        await using WebApplication app = builder.Build();
        var optionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<MySqlOptions>>();

        MySqlOptions optionsOne = optionsMonitor.Get("myMySqlServiceOne");

        ExtractConnectionStringParameters(optionsOne.ConnectionString).Should().BeEquivalentTo(new List<string>
        {
            "Connection Timeout=15",
            "Server=566ad428-5747-4b76-89db-bae25c70adae.mysql.service.internal",
            "Port=3306",
            "Database=service_instance_db",
            "User ID=6862f371181d4aee91c4995015fb2973",
            "Password=q3o5o3o88dyc8os5"
        }, options => options.WithoutStrictOrdering());

        MySqlOptions optionsTwo = optionsMonitor.Get("myMySqlServiceTwo");

        ExtractConnectionStringParameters(optionsTwo.ConnectionString).Should().BeEquivalentTo(new List<string>
        {
            "Server=43adf261-6658-4b36-98a5-144ad3cf5ae6.mysql.service.internal",
            "Port=3306",
            "Database=service_instance_db",
            "User ID=f2537d98484c48779a6811b62852b38b",
            "Password=rr7t44xnbvvto8b8"
        }, options => options.WithoutStrictOrdering());
    }

    [Fact]
    public async Task Registers_ConnectorFactory()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Steeltoe:Client:MySql:myMySqlServiceOne:ConnectionString"] = "SERVER=localhost;Database=db1;UID=user1;PWD=pass1",
            ["Steeltoe:Client:MySql:myMySqlServiceTwo:ConnectionString"] = "SERVER=localhost;Database=db2;UID=user2;PWD=pass2"
        });

        builder.AddMySql(MySqlPackageResolver.CreateForOnlyMySqlConnector());

        await using WebApplication app = builder.Build();

        var connectorFactory = app.Services.GetRequiredService<ConnectorFactory<MySqlOptions, MySqlConnection>>();

        connectorFactory.ServiceBindingNames.Should().HaveCount(2);
        connectorFactory.ServiceBindingNames.Should().Contain("myMySqlServiceOne");
        connectorFactory.ServiceBindingNames.Should().Contain("myMySqlServiceTwo");

        await using MySqlConnection connectionOne = connectorFactory.Get("myMySqlServiceOne").GetConnection();
        connectionOne.ConnectionString.Should().Be("Server=localhost;Database=db1;User ID=user1;Password=pass1");

        await using MySqlConnection connectionTwo = connectorFactory.Get("myMySqlServiceTwo").GetConnection();
        connectionTwo.ConnectionString.Should().Be("Server=localhost;Database=db2;User ID=user2;Password=pass2");
    }

    [Fact]
    public async Task Registers_HealthContributors()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Steeltoe:Client:MySql:myMySqlServiceOne:ConnectionString"] = "SERVER=localhost;Database=db1;UID=user1;PWD=pass1",
            ["Steeltoe:Client:MySql:myMySqlServiceTwo:ConnectionString"] = "SERVER=localhost;Database=db2;UID=user2;PWD=pass2"
        });

        builder.AddMySql(MySqlPackageResolver.CreateForOnlyMySqlConnector());

        await using WebApplication app = builder.Build();

        IHealthContributor[] healthContributors = app.Services.GetServices<IHealthContributor>().ToArray();
        healthContributors.Should().AllBeOfType<RelationalDbHealthContributor>();

        healthContributors.Should().HaveCount(2);
        healthContributors[0].Id.Should().Be("MySQL-myMySqlServiceOne");
        healthContributors[1].Id.Should().Be("MySQL-myMySqlServiceTwo");
    }

    [Fact]
    public async Task Registers_default_connection_string_when_only_single_server_binding_found()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddCloudFoundryServiceBindings(new StringServiceBindingsReader(SingleVcapServicesJson));

        builder.AddMySql(MySqlPackageResolver.CreateForOnlyMySqlConnector());

        await using WebApplication app = builder.Build();

        var connectorFactory = app.Services.GetRequiredService<ConnectorFactory<MySqlOptions, MySqlConnection>>();

        connectorFactory.ServiceBindingNames.Should().HaveCount(2);
        connectorFactory.ServiceBindingNames.Should().Contain(string.Empty);
        connectorFactory.ServiceBindingNames.Should().Contain("myMySqlServiceOne");

        string? defaultConnectionString = connectorFactory.Get().Options.ConnectionString;
        defaultConnectionString.Should().NotBeNullOrEmpty();

        string? namedConnectionString = connectorFactory.Get("myMySqlServiceOne").Options.ConnectionString;
        namedConnectionString.Should().Be(defaultConnectionString);

        app.Services.GetServices<IHealthContributor>().Should().HaveCount(1);
    }

    [Fact]
    public async Task Registers_default_connection_string_when_only_default_client_binding_found()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Steeltoe:Client:MySql:Default:ConnectionString"] = "SERVER=localhost;Database=myDb;UID=myUser;PWD=myPass"
        });

        builder.AddMySql(MySqlPackageResolver.CreateForOnlyMySqlConnector());

        await using WebApplication app = builder.Build();

        var connectorFactory = app.Services.GetRequiredService<ConnectorFactory<MySqlOptions, MySqlConnection>>();

        connectorFactory.ServiceBindingNames.Should().HaveCount(1);
        connectorFactory.ServiceBindingNames.Should().Contain(string.Empty);

        string? defaultConnectionString = connectorFactory.Get().Options.ConnectionString;
        defaultConnectionString.Should().NotBeNullOrEmpty();

        app.Services.GetServices<IHealthContributor>().Should().HaveCount(1);
    }

    private static IEnumerable<string> ExtractConnectionStringParameters(string? connectionString)
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
