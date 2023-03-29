// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding;
using Steeltoe.Connector.MySql;
using Xunit;

namespace Steeltoe.Connector.Test.MySql;

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

        builder.AddMySql();
        builder.Services.Configure<MySqlOptions>("myMySqlServiceOne", options => options.ConnectionString += ";Use Compression=false");

        await using WebApplication app = builder.Build();
        var optionsSnapshot = app.Services.GetRequiredService<IOptionsSnapshot<MySqlOptions>>();

        MySqlOptions optionsOne = optionsSnapshot.Get("myMySqlServiceOne");
        optionsOne.ConnectionString.Should().NotBeNull();

        ExtractConnectionStringParameters(optionsOne.ConnectionString).Should().BeEquivalentTo(new List<string>
        {
            "server=localhost",
            "database=db1",
            "user id=user1",
            "Use Compression=false",
            "password=pass1"
        }, options => options.WithoutStrictOrdering());

        MySqlOptions optionsTwo = optionsSnapshot.Get("myMySqlServiceTwo");
        optionsTwo.ConnectionString.Should().NotBeNull();

        ExtractConnectionStringParameters(optionsTwo.ConnectionString).Should().BeEquivalentTo(new List<string>
        {
            "server=localhost",
            "database=db2",
            "user id=user2",
            "password=pass2"
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

        builder.AddMySql();

        await using WebApplication app = builder.Build();
        var optionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<MySqlOptions>>();

        MySqlOptions optionsOne = optionsMonitor.Get("myMySqlServiceOne");
        optionsOne.Should().NotBeNull();

        ExtractConnectionStringParameters(optionsOne.ConnectionString).Should().BeEquivalentTo(new List<string>
        {
            "connectiontimeout=15",
            "server=566ad428-5747-4b76-89db-bae25c70adae.mysql.service.internal",
            "port=3306",
            "database=service_instance_db",
            "user id=6862f371181d4aee91c4995015fb2973",
            "password=q3o5o3o88dyc8os5"
        }, options => options.WithoutStrictOrdering());

        MySqlOptions optionsTwo = optionsMonitor.Get("myMySqlServiceTwo");
        optionsTwo.Should().NotBeNull();

        ExtractConnectionStringParameters(optionsTwo.ConnectionString).Should().BeEquivalentTo(new List<string>
        {
            "server=43adf261-6658-4b36-98a5-144ad3cf5ae6.mysql.service.internal",
            "port=3306",
            "database=service_instance_db",
            "user id=f2537d98484c48779a6811b62852b38b",
            "password=rr7t44xnbvvto8b8"
        }, options => options.WithoutStrictOrdering());
    }

    [Fact]
    public async Task Registers_ConnectionFactory()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Steeltoe:Client:MySql:myMySqlServiceOne:ConnectionString"] = "SERVER=localhost;Database=db1;UID=user1;PWD=pass1",
            ["Steeltoe:Client:MySql:myMySqlServiceTwo:ConnectionString"] = "SERVER=localhost;Database=db2;UID=user2;PWD=pass2"
        });

        builder.AddMySql();

        await using WebApplication app = builder.Build();

        var connectionFactory = app.Services.GetRequiredService<ConnectionFactory<MySqlOptions, MySqlConnection>>();

        await using MySqlConnection connectionOne = connectionFactory.GetNamed("myMySqlServiceOne").CreateConnection();
        connectionOne.ConnectionString.Should().Be("server=localhost;database=db1;user id=user1;password=pass1");

        await using MySqlConnection connectionTwo = connectionFactory.GetNamed("myMySqlServiceTwo").CreateConnection();
        connectionTwo.ConnectionString.Should().Be("server=localhost;database=db2;user id=user2;password=pass2");
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

        builder.AddMySql();

        await using WebApplication app = builder.Build();

        IHealthContributor[] healthContributors = app.Services.GetServices<IHealthContributor>().ToArray();

        healthContributors.Should().HaveCount(2);
        healthContributors[0].Id.Should().Be("MySQL-myMySqlServiceOne");
        healthContributors[1].Id.Should().Be("MySQL-myMySqlServiceTwo");
    }

    [Fact]
    public async Task Registers_default_connection_string_when_only_single_server_binding_found()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddCloudFoundryServiceBindings(new StringServiceBindingsReader(SingleVcapServicesJson));

        builder.AddMySql();

        await using WebApplication app = builder.Build();

        var connectionFactory = app.Services.GetRequiredService<ConnectionFactory<MySqlOptions, MySqlConnection>>();

        string defaultConnectionString = connectionFactory.GetDefault().Options.ConnectionString;
        defaultConnectionString.Should().NotBeNull();

        string namedConnectionString = connectionFactory.GetNamed("myMySqlServiceOne").Options.ConnectionString;
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

        builder.AddMySql();

        await using WebApplication app = builder.Build();

        var connectionFactory = app.Services.GetRequiredService<ConnectionFactory<MySqlOptions, MySqlConnection>>();

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
