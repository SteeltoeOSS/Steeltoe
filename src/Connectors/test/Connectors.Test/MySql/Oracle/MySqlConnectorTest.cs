// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding;
using Steeltoe.Configuration.Kubernetes.ServiceBinding;
using Steeltoe.Connectors.MySql;
using Steeltoe.Connectors.MySql.DynamicTypeAccess;

namespace Steeltoe.Connectors.Test.MySql.Oracle;

public sealed class MySqlConnectorTest
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
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Steeltoe:Client:MySql:myMySqlServiceOne:ConnectionString"] = "SERVER=localhost;Database=db1;UID=user1;PWD=pass1",
            ["Steeltoe:Client:MySql:myMySqlServiceTwo:ConnectionString"] = "SERVER=localhost;Database=db2;UID=user2;PWD=pass2"
        });

        builder.AddMySql(MySqlPackageResolver.OracleOnly);
        builder.Services.Configure<MySqlOptions>("myMySqlServiceOne", options => options.ConnectionString += ";Use Compression=false");

        await using WebApplication app = builder.Build();
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        var optionsSnapshot = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<MySqlOptions>>();

        MySqlOptions optionsOne = optionsSnapshot.Get("myMySqlServiceOne");

        ExtractConnectionStringParameters(optionsOne.ConnectionString).Should().BeEquivalentTo(new List<string>
        {
            "server=localhost",
            "database=db1",
            "user id=user1",
            "Use Compression=false",
            "password=pass1"
        }, options => options.WithoutStrictOrdering());

        MySqlOptions optionsTwo = optionsSnapshot.Get("myMySqlServiceTwo");

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
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddCloudFoundryServiceBindings(new StringServiceBindingsReader(MultiVcapServicesJson));

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Steeltoe:Client:MySql:myMySqlServiceOne:ConnectionString"] = "Connection Timeout=15;host=localhost"
        });

        builder.AddMySql(MySqlPackageResolver.OracleOnly);

        await using WebApplication app = builder.Build();
        var optionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<MySqlOptions>>();

        MySqlOptions optionsOne = optionsMonitor.Get("myMySqlServiceOne");

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
    public async Task Binds_options_with_Kubernetes_service_bindings()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();

        var fileProvider = new MemoryFileProvider();
        fileProvider.IncludeDirectory("db");
        fileProvider.IncludeFile("db/provider", "bitnami");
        fileProvider.IncludeFile("db/type", "mysql");
        fileProvider.IncludeFile("db/host", "10.0.219.125");
        fileProvider.IncludeFile("db/port", "3306");
        fileProvider.IncludeFile("db/username", "mysql");
        fileProvider.IncludeFile("db/password", "12TsdezjbRuskoH12v4KcrBkWlVjoxtU");
        fileProvider.IncludeFile("db/database", "my-mysql-service-4q5nt");

        var reader = new KubernetesMemoryServiceBindingsReader(fileProvider);
        builder.Configuration.AddKubernetesServiceBindings(reader);

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Steeltoe:Client:MySql:db:ConnectionString"] = "Connection Timeout=15;host=localhost"
        });

        builder.AddMySql();

        await using WebApplication app = builder.Build();
        var optionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<MySqlOptions>>();

        MySqlOptions dbOptions = optionsMonitor.Get("db");

        ExtractConnectionStringParameters(dbOptions.ConnectionString).Should().BeEquivalentTo(new List<string>
        {
            "Connection Timeout=15",
            "Server=10.0.219.125",
            "Port=3306",
            "Database=my-mysql-service-4q5nt",
            "User ID=mysql",
            "Password=12TsdezjbRuskoH12v4KcrBkWlVjoxtU"
        }, options => options.WithoutStrictOrdering());
    }

    [Fact]
    public async Task Registers_ConnectorFactory()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Steeltoe:Client:MySql:myMySqlServiceOne:ConnectionString"] = "SERVER=localhost;Database=db1;UID=user1;PWD=pass1",
            ["Steeltoe:Client:MySql:myMySqlServiceTwo:ConnectionString"] = "SERVER=localhost;Database=db2;UID=user2;PWD=pass2"
        });

        builder.AddMySql(MySqlPackageResolver.OracleOnly);

        await using WebApplication app = builder.Build();

        var connectorFactory = app.Services.GetRequiredService<ConnectorFactory<MySqlOptions, MySqlConnection>>();

        connectorFactory.ServiceBindingNames.Should().HaveCount(2);
        connectorFactory.ServiceBindingNames.Should().Contain("myMySqlServiceOne");
        connectorFactory.ServiceBindingNames.Should().Contain("myMySqlServiceTwo");

        await using MySqlConnection connectionOne = connectorFactory.Get("myMySqlServiceOne").GetConnection();
        connectionOne.ConnectionString.Should().Be("server=localhost;database=db1;user id=user1;password=pass1");

        await using MySqlConnection connectionTwo = connectorFactory.Get("myMySqlServiceTwo").GetConnection();
        connectionTwo.ConnectionString.Should().Be("server=localhost;database=db2;user id=user2;password=pass2");
    }

    [Fact]
    public async Task Registers_HealthContributors()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Steeltoe:Client:MySql:myMySqlServiceOne:ConnectionString"] = "SERVER=localhost;Database=db1;UID=user1;PWD=pass1",
            ["Steeltoe:Client:MySql:myMySqlServiceTwo:ConnectionString"] = "SERVER=localhost;Database=db2;UID=user2;PWD=pass2"
        });

        builder.AddMySql(MySqlPackageResolver.OracleOnly);

        await using WebApplication app = builder.Build();

        IHealthContributor[] healthContributors = app.Services.GetServices<IHealthContributor>().ToArray();
        RelationalDatabaseHealthContributor[] contributors = healthContributors.Should().AllBeOfType<RelationalDatabaseHealthContributor>().Subject.ToArray();
        contributors.Should().HaveCount(2);

        contributors[0].Id.Should().Be("MySQL");
        contributors[0].ServiceName.Should().Be("myMySqlServiceOne");
        contributors[0].Host.Should().Be("localhost");

        contributors[1].Id.Should().Be("MySQL");
        contributors[1].ServiceName.Should().Be("myMySqlServiceTwo");
        contributors[1].Host.Should().Be("localhost");
    }

    [Fact]
    public async Task Registers_default_connection_string_when_only_single_server_binding_found()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddCloudFoundryServiceBindings(new StringServiceBindingsReader(SingleVcapServicesJson));

        builder.AddMySql(MySqlPackageResolver.OracleOnly);

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
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Steeltoe:Client:MySql:Default:ConnectionString"] = "SERVER=localhost;Database=myDb;UID=myUser;PWD=myPass"
        });

        builder.AddMySql(MySqlPackageResolver.OracleOnly);

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
