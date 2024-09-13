// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry.ServiceBindings;
using Steeltoe.Connectors.CosmosDb;

namespace Steeltoe.Connectors.Test.CosmosDb;

public sealed class CosmosDbConnectorTest
{
    private const string MultiVcapServicesJson = """
        {
          "csb-azure-cosmosdb-sql": [
            {
              "binding_guid": "46668b92-d985-47cd-8595-65289cb19b44",
              "binding_name": null,
              "credentials": {
                "cosmosdb_database_id": "csb-db05a6b464-4680-472f-8754-7e1afe015fac",
                "cosmosdb_host_endpoint": "https://csb05a6b464-4680-472f-8754-7e1afe015fac.documents.cloud-host.com:443/",
                "cosmosdb_master_key": "ovmolgG4kWHqoP4PaIfi35zXQGaWG04wr4Bh1mS1gckfh99yMsnCFgdlLPNao0M9GYYiReDhDSklACDbxSCpvw==",
                "cosmosdb_readonly_master_key": "L3Z1ehMA1OVtkkI7MCwu3UvnlmrSIG6TbJgvptpEyToWDI2rIjS1GvwjklXizfkT51qSGvoNwOVWACDbU4mAUQ==",
                "status": "created account csb05a6b464-4680-472f-8754-7e1afe015fac (id: /subscriptions/86fb0197-be70-4ceb-88e3-855615bc1b34/resourceGroups/cotati/providers/Microsoft.DocumentDB/databaseAccounts/csb05a6b464-4680-472f-8754-7e1afe015fac) URL: https://portal.cloud-host.com/#@b39138ca-3cee-4b4a-a4d6-cd83d9dd62f0/resource/subscriptions/86fb0197-be70-4ceb-88e3-855615bc1b34/resourceGroups/cotati/providers/Microsoft.DocumentDB/databaseAccounts/csb05a6b464-4680-472f-8754-7e1afe015fac"
              },
              "instance_guid": "05a6b464-4680-472f-8754-7e1afe015fac",
              "instance_name": "myCosmosDbServiceOne",
              "label": "csb-azure-cosmosdb-sql",
              "name": "myCosmosDbServiceOne",
              "plan": "mini",
              "provider": null,
              "syslog_drain_url": null,
              "tags": [
                "azure",
                "cosmos",
                "cosmosdb",
                "cosmos-sql",
                "cosmosdb-sql",
                "preview"
              ],
              "volume_mounts": []
            },
            {
              "binding_guid": "6ad1e327-2b00-4f2b-85a0-e75bdebf93a6",
              "binding_name": null,
              "credentials": {
                "cosmosdb_database_id": "csb-dbf1eeadc1-cab8-436e-b1ac-61bf7c7ebfcc",
                "cosmosdb_host_endpoint": "https://csbf1eeadc1-cab8-436e-b1ac-61bf7c7ebfcc.documents.cloud-host.com:443/",
                "cosmosdb_master_key": "Hr6oIIHBGPt5KXvtIbSj36D8Te7xMYuFZj2L5w7FcmfPRkXd64PA87aXcOwuvxmKkXnsQlOZDCK3ACDbNzQFPw==",
                "cosmosdb_readonly_master_key": "LK6OIFxQh12PLpwlHezp7fmFuw5HsEdqrgZrzgFx4ZQh1uOiLr5r51sgJOmDPEII0uEyJtUwLFAqACDbkYULxQ==",
                "status": "created account csbf1eeadc1-cab8-436e-b1ac-61bf7c7ebfcc (id: /subscriptions/86fb0197-be70-4ceb-88e3-855615bc1b34/resourceGroups/cotati/providers/Microsoft.DocumentDB/databaseAccounts/csbf1eeadc1-cab8-436e-b1ac-61bf7c7ebfcc) URL: https://portal.cloud-host.com/#@b39138ca-3cee-4b4a-a4d6-cd83d9dd62f0/resource/subscriptions/86fb0197-be70-4ceb-88e3-855615bc1b34/resourceGroups/cotati/providers/Microsoft.DocumentDB/databaseAccounts/csbf1eeadc1-cab8-436e-b1ac-61bf7c7ebfcc"
              },
              "instance_guid": "f1eeadc1-cab8-436e-b1ac-61bf7c7ebfcc",
              "instance_name": "myCosmosDbServiceTwo",
              "label": "csb-azure-cosmosdb-sql",
              "name": "myCosmosDbServiceTwo",
              "plan": "mini",
              "provider": null,
              "syslog_drain_url": null,
              "tags": [
                "azure",
                "cosmos",
                "cosmosdb",
                "cosmos-sql",
                "cosmosdb-sql",
                "preview"
              ],
              "volume_mounts": []
            }
          ]
        }
        """;

    private const string SingleVcapServicesJson = """
        {
          "csb-azure-cosmosdb-sql": [
            {
              "binding_guid": "1f3a7dcb-55d1-4227-8b44-cdec2f81c8c4",
              "binding_name": null,
              "credentials": {
                "cosmosdb_database_id": "csb-dbccee8ad6-6179-4e13-b90d-d4ee1d1c30ea",
                "cosmosdb_host_endpoint": "https://csbccee8ad6-6179-4e13-b90d-d4ee1d1c30ea.documents.cloud-host.com:443/",
                "cosmosdb_master_key": "HlZK6jZGBn7x16PVTQwm5xK5BUtXK8Oh9qO3kTcRptoqf2xaNdgjmysr1GZBpq0MHBRZdUN68NjfACDbRiWLXQ==",
                "cosmosdb_readonly_master_key": "lNgchEgj0rvvrpEiy4DnTRKtgEfrO0DbSWQWsZw7hdCFNw10OuBDkqdPxSleiWnWqE6sRXxvE8KxACDb6oWVcA==",
                "status": "created account csbccee8ad6-6179-4e13-b90d-d4ee1d1c30ea (id: /subscriptions/86fb0197-be70-4ceb-88e3-855615bc1b34/resourceGroups/cotati/providers/Microsoft.DocumentDB/databaseAccounts/csbccee8ad6-6179-4e13-b90d-d4ee1d1c30ea) URL: https://portal.cloud-host.com/#@b39138ca-3cee-4b4a-a4d6-cd83d9dd62f0/resource/subscriptions/86fb0197-be70-4ceb-88e3-855615bc1b34/resourceGroups/cotati/providers/Microsoft.DocumentDB/databaseAccounts/csbccee8ad6-6179-4e13-b90d-d4ee1d1c30ea"
              },
              "instance_guid": "ccee8ad6-6179-4e13-b90d-d4ee1d1c30ea",
              "instance_name": "myCosmosDbService",
              "label": "csb-azure-cosmosdb-sql",
              "name": "myCosmosDbService",
              "plan": "mini",
              "provider": null,
              "syslog_drain_url": null,
              "tags": [
                "azure",
                "cosmos",
                "cosmosdb",
                "cosmos-sql",
                "cosmosdb-sql",
                "preview"
              ],
              "volume_mounts": []
            }
          ]
        }
        """;

    [Fact]
    public async Task Binds_options_without_service_bindings()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Steeltoe:Client:CosmosDb:myCosmosDbServiceOne:ConnectionString"] =
                "AccountEndpoint=https://host-1:8081;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
            ["Steeltoe:Client:CosmosDb:myCosmosDbServiceOne:Database"] = "db1",
            ["Steeltoe:Client:CosmosDb:myCosmosDbServiceTwo:ConnectionString"] =
                "AccountEndpoint=https://host-2:8081;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
            ["Steeltoe:Client:CosmosDb:myCosmosDbServiceTwo:Database"] = "db2"
        });

        builder.AddCosmosDb();

        await using WebApplication app = builder.Build();
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        var optionsSnapshot = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<CosmosDbOptions>>();

        CosmosDbOptions optionsOne = optionsSnapshot.Get("myCosmosDbServiceOne");

        optionsOne.ConnectionString.Should()
            .Be("accountendpoint=https://host-1:8081;accountkey=\"C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==\"");

        optionsOne.Database.Should().Be("db1");

        CosmosDbOptions optionsTwo = optionsSnapshot.Get("myCosmosDbServiceTwo");

        optionsTwo.ConnectionString.Should()
            .Be("accountendpoint=https://host-2:8081;accountkey=\"C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==\"");

        optionsTwo.Database.Should().Be("db2");
    }

    [Fact]
    public async Task Binds_options_with_CloudFoundry_service_bindings()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddCloudFoundryServiceBindings(new StringServiceBindingsReader(MultiVcapServicesJson));

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Steeltoe:Client:CosmosDb:myCosmosDbServiceOne:ConnectionString"] =
                "AccountEndpoint=https://localhost:8081;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
            ["Steeltoe:Client:CosmosDb:myCosmosDbServiceOne:Database"] = "db1"
        });

        builder.AddCosmosDb();

        await using WebApplication app = builder.Build();
        var optionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<CosmosDbOptions>>();

        CosmosDbOptions optionsOne = optionsMonitor.Get("myCosmosDbServiceOne");

        optionsOne.ConnectionString.Should().Be(
            "accountendpoint=https://csb05a6b464-4680-472f-8754-7e1afe015fac.documents.cloud-host.com:443/;accountkey=\"ovmolgG4kWHqoP4PaIfi35zXQGaWG04wr4Bh1mS1gckfh99yMsnCFgdlLPNao0M9GYYiReDhDSklACDbxSCpvw==\"");

        optionsOne.Database.Should().Be("csb-db05a6b464-4680-472f-8754-7e1afe015fac");

        CosmosDbOptions optionsTwo = optionsMonitor.Get("myCosmosDbServiceTwo");

        optionsTwo.ConnectionString.Should().Be(
            "accountEndpoint=https://csbf1eeadc1-cab8-436e-b1ac-61bf7c7ebfcc.documents.cloud-host.com:443/;accountKey=\"Hr6oIIHBGPt5KXvtIbSj36D8Te7xMYuFZj2L5w7FcmfPRkXd64PA87aXcOwuvxmKkXnsQlOZDCK3ACDbNzQFPw==\"");

        optionsTwo.Database.Should().Be("csb-dbf1eeadc1-cab8-436e-b1ac-61bf7c7ebfcc");
    }

    [Fact]
    public async Task Registers_ConnectorFactory()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Steeltoe:Client:CosmosDb:myCosmosDbServiceOne:ConnectionString"] =
                "AccountEndpoint=https://host-1:8081;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
            ["Steeltoe:Client:CosmosDb:myCosmosDbServiceTwo:ConnectionString"] =
                "AccountEndpoint=https://host-2:8081;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
        });

        builder.AddCosmosDb(null, addOptions =>
        {
            addOptions.CreateConnection = (serviceProvider, serviceBindingName) =>
            {
                var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<CosmosDbOptions>>();
                CosmosDbOptions options = optionsMonitor.Get(serviceBindingName);

                if (serviceBindingName == "myCosmosDbServiceOne")
                {
                    return new CosmosClient(options.ConnectionString, new CosmosClientOptions
                    {
                        ConsistencyLevel = ConsistencyLevel.Eventual
                    });
                }

                return new CosmosClient(options.ConnectionString);
            };
        });

        await using WebApplication app = builder.Build();

        var connectorFactory = app.Services.GetRequiredService<ConnectorFactory<CosmosDbOptions, CosmosClient>>();

        connectorFactory.ServiceBindingNames.Should().HaveCount(2);
        connectorFactory.ServiceBindingNames.Should().Contain("myCosmosDbServiceOne");
        connectorFactory.ServiceBindingNames.Should().Contain("myCosmosDbServiceTwo");

        CosmosClient connectionOne = connectorFactory.Get("myCosmosDbServiceOne").GetConnection();
        connectionOne.ClientOptions.ConsistencyLevel.Should().Be(ConsistencyLevel.Eventual);

        CosmosClient connectionTwo = connectorFactory.Get("myCosmosDbServiceTwo").GetConnection();
        connectionTwo.ClientOptions.ConsistencyLevel.Should().BeNull();

        CosmosClient connectionOneAgain = connectorFactory.Get("myCosmosDbServiceOne").GetConnection();
        connectionOneAgain.Should().BeSameAs(connectionOne);
    }

    [Fact]
    public async Task Registers_HealthContributors()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Steeltoe:Client:CosmosDb:myCosmosDbServiceOne:ConnectionString"] =
                "AccountEndpoint=https://host-1:8081;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
            ["Steeltoe:Client:CosmosDb:myCosmosDbServiceTwo:ConnectionString"] =
                "AccountEndpoint=https://host-2:8081;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
        });

        builder.AddCosmosDb();

        await using WebApplication app = builder.Build();

        IHealthContributor[] healthContributors = app.Services.GetServices<IHealthContributor>().ToArray();
        CosmosDbHealthContributor[] cosmosDbHealthContributors = healthContributors.Should().AllBeOfType<CosmosDbHealthContributor>().Subject.ToArray();
        cosmosDbHealthContributors.Should().HaveCount(2);

        cosmosDbHealthContributors[0].Id.Should().Be("CosmosDB");
        cosmosDbHealthContributors[0].ServiceName.Should().Be("myCosmosDbServiceOne");
        cosmosDbHealthContributors[0].Host.Should().Be("host-1");

        cosmosDbHealthContributors[1].Id.Should().Be("CosmosDB");
        cosmosDbHealthContributors[1].ServiceName.Should().Be("myCosmosDbServiceTwo");
        cosmosDbHealthContributors[1].Host.Should().Be("host-2");
    }

    [Fact]
    public async Task Registers_default_connection_string_when_only_single_server_binding_found()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddCloudFoundryServiceBindings(new StringServiceBindingsReader(SingleVcapServicesJson));

        builder.AddCosmosDb();

        await using WebApplication app = builder.Build();

        var connectorFactory = app.Services.GetRequiredService<ConnectorFactory<CosmosDbOptions, CosmosClient>>();

        connectorFactory.ServiceBindingNames.Should().HaveCount(2);
        connectorFactory.ServiceBindingNames.Should().Contain(string.Empty);
        connectorFactory.ServiceBindingNames.Should().Contain("myCosmosDbService");

        CosmosDbOptions defaultOptions = connectorFactory.Get().Options;
        defaultOptions.ConnectionString.Should().NotBeNullOrEmpty();
        defaultOptions.Database.Should().NotBeNullOrEmpty();

        CosmosDbOptions namedOptions = connectorFactory.Get("myCosmosDbService").Options;
        namedOptions.ConnectionString.Should().Be(defaultOptions.ConnectionString);
        namedOptions.Database.Should().Be(defaultOptions.Database);

        app.Services.GetServices<IHealthContributor>().Should().HaveCount(1);
    }

    [Fact]
    public async Task Registers_default_connection_string_when_only_default_client_binding_found()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Steeltoe:Client:CosmosDb:Default:ConnectionString"] =
                "AccountEndpoint=https://localhost:8081;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
            ["Steeltoe:Client:CosmosDb:Default:Database"] = "db"
        });

        builder.AddCosmosDb();

        await using WebApplication app = builder.Build();

        var connectorFactory = app.Services.GetRequiredService<ConnectorFactory<CosmosDbOptions, CosmosClient>>();

        connectorFactory.ServiceBindingNames.Should().HaveCount(1);
        connectorFactory.ServiceBindingNames.Should().Contain(string.Empty);

        CosmosDbOptions defaultOptions = connectorFactory.Get().Options;
        defaultOptions.ConnectionString.Should().NotBeNullOrEmpty();
        defaultOptions.Database.Should().NotBeNullOrEmpty();

        app.Services.GetServices<IHealthContributor>().Should().HaveCount(1);
    }
}
