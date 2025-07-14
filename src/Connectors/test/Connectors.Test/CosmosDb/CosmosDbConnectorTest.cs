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
using Steeltoe.Connectors.CosmosDb;

namespace Steeltoe.Connectors.Test.CosmosDb;

public sealed class CosmosDbConnectorTest
{
    [Fact]
    public async Task Binds_options_without_service_bindings()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Steeltoe:Client:CosmosDb:myCosmosDbServiceOne:ConnectionString"] =
                "AccountEndpoint=https://host-1:8081;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
            ["Steeltoe:Client:CosmosDb:myCosmosDbServiceOne:Database"] = "db1",
            ["Steeltoe:Client:CosmosDb:myCosmosDbServiceTwo:ConnectionString"] =
                "AccountEndpoint=https://host-2:8081;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
            ["Steeltoe:Client:CosmosDb:myCosmosDbServiceTwo:Database"] = "db2"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
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
    public async Task Registers_ConnectorFactory()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Steeltoe:Client:CosmosDb:myCosmosDbServiceOne:ConnectionString"] =
                "AccountEndpoint=https://host-1:8081;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
            ["Steeltoe:Client:CosmosDb:myCosmosDbServiceTwo:ConnectionString"] =
                "AccountEndpoint=https://host-2:8081;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);

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
        var appSettings = new Dictionary<string, string?>
        {
            ["Steeltoe:Client:CosmosDb:myCosmosDbServiceOne:ConnectionString"] =
                "AccountEndpoint=https://host-1:8081;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
            ["Steeltoe:Client:CosmosDb:myCosmosDbServiceTwo:ConnectionString"] =
                "AccountEndpoint=https://host-2:8081;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.AddCosmosDb();
        await using WebApplication app = builder.Build();

        CosmosDbHealthContributor[] cosmosDbHealthContributors =
        [
            .. app.Services.GetServices<IHealthContributor>().Should().HaveCount(2).And.AllBeOfType<CosmosDbHealthContributor>().Subject
        ];

        cosmosDbHealthContributors[0].Id.Should().Be("CosmosDB");
        cosmosDbHealthContributors[0].ServiceName.Should().Be("myCosmosDbServiceOne");
        cosmosDbHealthContributors[0].Host.Should().Be("host-1");

        cosmosDbHealthContributors[1].Id.Should().Be("CosmosDB");
        cosmosDbHealthContributors[1].ServiceName.Should().Be("myCosmosDbServiceTwo");
        cosmosDbHealthContributors[1].Host.Should().Be("host-2");
    }

    [Fact]
    public async Task Registers_default_connection_string_when_only_default_client_binding_found()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Steeltoe:Client:CosmosDb:Default:ConnectionString"] =
                "AccountEndpoint=https://localhost:8081;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
            ["Steeltoe:Client:CosmosDb:Default:Database"] = "db"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.AddCosmosDb();
        await using WebApplication app = builder.Build();

        var connectorFactory = app.Services.GetRequiredService<ConnectorFactory<CosmosDbOptions, CosmosClient>>();

        connectorFactory.ServiceBindingNames.Should().ContainSingle().Which.Should().BeEmpty();

        CosmosDbOptions defaultOptions = connectorFactory.Get().Options;
        defaultOptions.ConnectionString.Should().NotBeNullOrEmpty();
        defaultOptions.Database.Should().NotBeNullOrEmpty();

        app.Services.GetServices<IHealthContributor>().Should().ContainSingle();
    }
}
