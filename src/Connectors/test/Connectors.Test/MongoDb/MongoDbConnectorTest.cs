// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry.ServiceBindings;
using Steeltoe.Configuration.Kubernetes.ServiceBindings;
using Steeltoe.Connectors.MongoDb;

namespace Steeltoe.Connectors.Test.MongoDb;

public sealed class MongoDbConnectorTest
{
    private const string MultiVcapServicesJson = """
        {
          "csb-azure-mongodb": [
            {
              "binding_guid": "63797afe-d49f-4ae0-9e5e-cd0f89e64bd3",
              "binding_name": null,
              "credentials": {
                "status": "created db csb-db0230eada-2354-4c73-b3e4-8a1aaa996894 (id: /subscriptions/86fb0197-be70-4ceb-88e3-855615bc1b34/resourceGroups/cotati/providers/Microsoft.DocumentDB/databaseAccounts/csb0230eada-2354-4c73-b3e4-8a1aaa996894/mongodbDatabases/csb-db0230eada-2354-4c73-b3e4-8a1aaa996894) URL: https://portal.cloud-hostname.com/#@b39138ca-3cee-4b4a-a4d6-cd83d9dd62f0/resource/subscriptions/86fb0197-be70-4ceb-88e3-855615bc1b34/resourceGroups/cotati/providers/Microsoft.DocumentDB/databaseAccounts/csb0230eada-2354-4c73-b3e4-8a1aaa996894/mongodbDatabases/csb-db0230eada-2354-4c73-b3e4-8a1aaa996894",
                "uri": "mongodb://csb0230eada-2354-4c73-b3e4-8a1aaa996894:AiNtEyASbdXR5neJmTStMzKGItX2xvKuyEkcy65rviKD0ggZR19E1iVFIJ5ZAIY1xvvAiS5tOXsmACDbKDJIhQ==@csb0230eada-2354-4c73-b3e4-8a1aaa996894.mongo.cosmos.cloud-hostname.com:10255/csb-db0230eada-2354-4c73-b3e4-8a1aaa996894?ssl=true&replicaSet=globaldb&retrywrites=false&maxIdleTimeMS=120000&appName=@csb0230eada-2354-4c73-b3e4-8a1aaa996894@"
              },
              "instance_guid": "0230eada-2354-4c73-b3e4-8a1aaa996894",
              "instance_name": "myMongoDbServiceOne",
              "label": "csb-azure-mongodb",
              "name": "myMongoDbServiceOne",
              "plan": "small",
              "provider": null,
              "syslog_drain_url": null,
              "tags": [
                "azure",
                "mongodb",
                "preview",
                "cosmosdb-mongo",
                "cosmosdb-mongodb"
              ],
              "volume_mounts": []
            },
            {
              "binding_guid": "fe16c4ed-44b0-45e3-8d72-140e4acac074",
              "binding_name": null,
              "credentials": {
                "status": "created db csb-db3aa12f5f-7530-4ff3-b328-a23a42af18df (id: /subscriptions/86fb0197-be70-4ceb-88e3-855615bc1b34/resourceGroups/cotati/providers/Microsoft.DocumentDB/databaseAccounts/csb3aa12f5f-7530-4ff3-b328-a23a42af18df/mongodbDatabases/csb-db3aa12f5f-7530-4ff3-b328-a23a42af18df) URL: https://portal.cloud-hostname.com/#@b39138ca-3cee-4b4a-a4d6-cd83d9dd62f0/resource/subscriptions/86fb0197-be70-4ceb-88e3-855615bc1b34/resourceGroups/cotati/providers/Microsoft.DocumentDB/databaseAccounts/csb3aa12f5f-7530-4ff3-b328-a23a42af18df/mongodbDatabases/csb-db3aa12f5f-7530-4ff3-b328-a23a42af18df",
                "uri": "mongodb://csb3aa12f5f-7530-4ff3-b328-a23a42af18df:NhCG266clYbNakBniDs8oLTniqTE06XXafhJWcbkNuma8Ie1XntsO2DqvPudYwqgk4le896YZjxbACDb8GiQYg==@csb3aa12f5f-7530-4ff3-b328-a23a42af18df.mongo.cosmos.cloud-hostname.com:10255/csb-db3aa12f5f-7530-4ff3-b328-a23a42af18df?ssl=true&replicaSet=globaldb&retrywrites=false&maxIdleTimeMS=120000&appName=@csb3aa12f5f-7530-4ff3-b328-a23a42af18df@"
              },
              "instance_guid": "3aa12f5f-7530-4ff3-b328-a23a42af18df",
              "instance_name": "myMongoDbServiceTwo",
              "label": "csb-azure-mongodb",
              "name": "myMongoDbServiceTwo",
              "plan": "small",
              "provider": null,
              "syslog_drain_url": null,
              "tags": [
                "azure",
                "mongodb",
                "preview",
                "cosmosdb-mongo",
                "cosmosdb-mongodb"
              ],
              "volume_mounts": []
            }
          ]
        }
        """;

    private const string SingleVcapServicesJson = """
        {
          "csb-azure-mongodb": [
            {
              "binding_guid": "e7b70192-1a7d-4889-b771-2f8fdbee66d9",
              "binding_name": null,
              "credentials": {
                "status": "created db csb-db8fc27e79-7187-4c8f-beb3-349c84362cbc (id: /subscriptions/86fb0197-be70-4ceb-88e3-855615bc1b34/resourceGroups/cotati/providers/Microsoft.DocumentDB/databaseAccounts/csb8fc27e79-7187-4c8f-beb3-349c84362cbc/mongodbDatabases/csb-db8fc27e79-7187-4c8f-beb3-349c84362cbc) URL: https://portal.cloud-hostname.com/#@b39138ca-3cee-4b4a-a4d6-cd83d9dd62f0/resource/subscriptions/86fb0197-be70-4ceb-88e3-855615bc1b34/resourceGroups/cotati/providers/Microsoft.DocumentDB/databaseAccounts/csb8fc27e79-7187-4c8f-beb3-349c84362cbc/mongodbDatabases/csb-db8fc27e79-7187-4c8f-beb3-349c84362cbc",
                "uri": "mongodb://csb8fc27e79-7187-4c8f-beb3-349c84362cbc:Kms9pk1v2nKT8E4bzKucuIgv9IyGbQFfnvlYZ7XJ48wXhEUP28vrjJMCu1mpuBMsyia3VxBWJpmEACDbPlX67Q==@csb8fc27e79-7187-4c8f-beb3-349c84362cbc.mongo.cosmos.cloud-hostname.com:10255/csb-db8fc27e79-7187-4c8f-beb3-349c84362cbc?ssl=true&replicaSet=globaldb&retrywrites=false&maxIdleTimeMS=120000&appName=@csb8fc27e79-7187-4c8f-beb3-349c84362cbc@"
              },
              "instance_guid": "8fc27e79-7187-4c8f-beb3-349c84362cbc",
              "instance_name": "myMongoDbService",
              "label": "csb-azure-mongodb",
              "name": "myMongoDbService",
              "plan": "small",
              "provider": null,
              "syslog_drain_url": null,
              "tags": [
                "azure",
                "mongodb",
                "preview",
                "cosmosdb-mongo",
                "cosmosdb-mongodb"
              ],
              "volume_mounts": []
            }
          ]
        }
        """;

    [Fact]
    public async Task Binds_options_without_service_bindings()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Steeltoe:Client:MongoDb:myMongoDbServiceOne:ConnectionString"] = "mongodb://localhost:27017/auth-db?connectTimeoutMS=5000",
            ["Steeltoe:Client:MongoDb:myMongoDbServiceOne:Database"] = "db1",
            ["Steeltoe:Client:MongoDb:myMongoDbServiceTwo:ConnectionString"] = "mongodb://user:pass@localhost:27018/auth-db",
            ["Steeltoe:Client:MongoDb:myMongoDbServiceTwo:Database"] = "db2"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.AddMongoDb();

        builder.Services.Configure<MongoDbOptions>("myMongoDbServiceOne", options =>
        {
            var urlBuilder = new MongoUrlBuilder(options.ConnectionString)
            {
                ApplicationName = "mongodb-test"
            };

            options.ConnectionString = urlBuilder.ToString();
        });

        await using WebApplication app = builder.Build();
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        var optionsSnapshot = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<MongoDbOptions>>();

        MongoDbOptions optionsOne = optionsSnapshot.Get("myMongoDbServiceOne");
        optionsOne.ConnectionString.Should().Be("mongodb://localhost/auth-db?appname=mongodb-test&connectTimeout=5s");
        optionsOne.Database.Should().Be("db1");

        MongoDbOptions optionsTwo = optionsSnapshot.Get("myMongoDbServiceTwo");
        optionsTwo.ConnectionString.Should().Be("mongodb://user:pass@localhost:27018/auth-db");
        optionsTwo.Database.Should().Be("db2");
    }

    [Fact]
    public async Task Binds_options_with_CloudFoundry_service_bindings()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Steeltoe:Client:MongoDb:myMongoDbServiceOne:ConnectionString"] = "mongodb://localhost:27017/auth-db?connectTimeoutMS=5000",
            ["Steeltoe:Client:MongoDb:myMongoDbServiceOne:Database"] = "db1"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddCloudFoundryServiceBindings(new StringServiceBindingsReader(MultiVcapServicesJson));
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.AddMongoDb();
        await using WebApplication app = builder.Build();

        var optionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<MongoDbOptions>>();

        MongoDbOptions optionsOne = optionsMonitor.Get("myMongoDbServiceOne");

        optionsOne.ConnectionString.Should().Be(
            "mongodb://csb0230eada-2354-4c73-b3e4-8a1aaa996894:AiNtEyASbdXR5neJmTStMzKGItX2xvKuyEkcy65rviKD0ggZR19E1iVFIJ5ZAIY1xvvAiS5tOXsmACDbKDJIhQ%3D%3D@csb0230eada-2354-4c73-b3e4-8a1aaa996894.mongo.cosmos.cloud-hostname.com:10255/csb-db0230eada-2354-4c73-b3e4-8a1aaa996894?connectTimeoutMS=5000&ssl=true&replicaSet=globaldb&retrywrites=false&maxIdleTimeMS=120000&appName=@csb0230eada-2354-4c73-b3e4-8a1aaa996894@");

        optionsOne.Database.Should().Be("csb-db0230eada-2354-4c73-b3e4-8a1aaa996894");

        MongoDbOptions optionsTwo = optionsMonitor.Get("myMongoDbServiceTwo");

        optionsTwo.ConnectionString.Should().Be(
            "mongodb://csb3aa12f5f-7530-4ff3-b328-a23a42af18df:NhCG266clYbNakBniDs8oLTniqTE06XXafhJWcbkNuma8Ie1XntsO2DqvPudYwqgk4le896YZjxbACDb8GiQYg%3D%3D@csb3aa12f5f-7530-4ff3-b328-a23a42af18df.mongo.cosmos.cloud-hostname.com:10255/csb-db3aa12f5f-7530-4ff3-b328-a23a42af18df?ssl=true&replicaSet=globaldb&retrywrites=false&maxIdleTimeMS=120000&appName=@csb3aa12f5f-7530-4ff3-b328-a23a42af18df@");

        optionsTwo.Database.Should().Be("csb-db3aa12f5f-7530-4ff3-b328-a23a42af18df");
    }

    [Fact]
    public async Task Binds_options_with_Kubernetes_service_bindings()
    {
        var fileProvider = new MemoryFileProvider();
        fileProvider.IncludeDirectory("db");
        fileProvider.IncludeFile("db/provider", "bitnami");
        fileProvider.IncludeFile("db/type", "mongodb");
        fileProvider.IncludeFile("db/host", "10.0.13.36");
        fileProvider.IncludeFile("db/port", "27017");
        fileProvider.IncludeFile("db/username", "mongodb");
        fileProvider.IncludeFile("db/password", "SDtUXKTRJspRAtxySqZMixAfWHP3oOGq");
        fileProvider.IncludeFile("db/database", "my-mongodb-service-d8nkz");
        var reader = new KubernetesMemoryServiceBindingsReader(fileProvider);

        var appSettings = new Dictionary<string, string?>
        {
            ["Steeltoe:Client:MongoDb:db:ConnectionString"] = "mongodb://localhost:27017/auth-db?connectTimeoutMS=5000",
            ["Steeltoe:Client:MongoDb:db:Database"] = "db1"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddKubernetesServiceBindings(reader);
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.AddMongoDb();
        await using WebApplication app = builder.Build();

        var optionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<MongoDbOptions>>();

        MongoDbOptions dbOptions = optionsMonitor.Get("db");

        dbOptions.ConnectionString.Should().Be(
            "mongodb://mongodb:SDtUXKTRJspRAtxySqZMixAfWHP3oOGq@10.0.13.36:27017/my-mongodb-service-d8nkz?connectTimeoutMS=5000");

        dbOptions.Database.Should().Be("my-mongodb-service-d8nkz");
    }

    [Fact]
    public async Task Registers_ConnectorFactory()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Steeltoe:Client:MongoDb:myMongoDbServiceOne:ConnectionString"] = "mongodb://localhost:27017",
            ["Steeltoe:Client:MongoDb:myMongoDbServiceTwo:ConnectionString"] = "mongodb://user:pass@localhost:27018"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.AddMongoDb();
        await using WebApplication app = builder.Build();

        var connectorFactory = app.Services.GetRequiredService<ConnectorFactory<MongoDbOptions, IMongoClient>>();

        connectorFactory.ServiceBindingNames.Should().HaveCount(2);
        connectorFactory.ServiceBindingNames.Should().Contain("myMongoDbServiceOne");
        connectorFactory.ServiceBindingNames.Should().Contain("myMongoDbServiceTwo");

        IMongoClient connectionOne = connectorFactory.Get("myMongoDbServiceOne").GetConnection();
        connectionOne.Settings.Credential.Should().BeNull();
        connectionOne.Settings.Server.Host.Should().Be("localhost");
        connectionOne.Settings.Server.Port.Should().Be(27017);

        IMongoClient connectionTwo = connectorFactory.Get("myMongoDbServiceTwo").GetConnection();
        connectionTwo.Settings.Credential.Username.Should().Be("user");
        connectionTwo.Settings.Credential.Evidence.Should().Be(new PasswordEvidence("pass"));
        connectionTwo.Settings.Server.Host.Should().Be("localhost");
        connectionTwo.Settings.Server.Port.Should().Be(27018);
    }

    [Fact]
    public async Task Registers_HealthContributors()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Steeltoe:Client:MongoDb:myMongoDbServiceOne:ConnectionString"] = "mongodb://localhost:27017/auth-db",
            ["Steeltoe:Client:MongoDb:myMongoDbServiceTwo:ConnectionString"] = "mongodb://user:pass@localhost:27018/auth-db"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.AddMongoDb();
        await using WebApplication app = builder.Build();

        MongoDbHealthContributor[] mongoDbHealthContributors =
        [
            .. app.Services.GetServices<IHealthContributor>().Should().HaveCount(2).And.AllBeOfType<MongoDbHealthContributor>().Subject
        ];

        mongoDbHealthContributors[0].Id.Should().Be("MongoDB");
        mongoDbHealthContributors[0].ServiceName.Should().Be("myMongoDbServiceOne");
        mongoDbHealthContributors[0].Host.Should().Be("localhost");

        mongoDbHealthContributors[1].Id.Should().Be("MongoDB");
        mongoDbHealthContributors[1].ServiceName.Should().Be("myMongoDbServiceTwo");
        mongoDbHealthContributors[1].Host.Should().Be("localhost");
    }

    [Fact]
    public async Task Registers_default_connection_string_when_only_single_server_binding_found()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddCloudFoundryServiceBindings(new StringServiceBindingsReader(SingleVcapServicesJson));
        builder.AddMongoDb();
        await using WebApplication app = builder.Build();

        var connectorFactory = app.Services.GetRequiredService<ConnectorFactory<MongoDbOptions, IMongoClient>>();

        connectorFactory.ServiceBindingNames.Should().HaveCount(2);
        connectorFactory.ServiceBindingNames.Should().Contain(string.Empty);
        connectorFactory.ServiceBindingNames.Should().Contain("myMongoDbService");

        MongoDbOptions defaultOptions = connectorFactory.Get().Options;
        defaultOptions.ConnectionString.Should().NotBeNullOrEmpty();
        defaultOptions.Database.Should().NotBeNullOrEmpty();

        MongoDbOptions namedOptions = connectorFactory.Get("myMongoDbService").Options;
        namedOptions.ConnectionString.Should().Be(defaultOptions.ConnectionString);
        namedOptions.Database.Should().Be(defaultOptions.Database);

        app.Services.GetServices<IHealthContributor>().Should().ContainSingle();
    }

    [Fact]
    public async Task Registers_default_connection_string_when_only_default_client_binding_found()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Steeltoe:Client:MongoDb:Default:ConnectionString"] = "mongodb://localhost:27017/auth-db",
            ["Steeltoe:Client:MongoDb:Default:Database"] = "db"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.AddMongoDb();
        await using WebApplication app = builder.Build();

        var connectorFactory = app.Services.GetRequiredService<ConnectorFactory<MongoDbOptions, IMongoClient>>();

        connectorFactory.ServiceBindingNames.Should().ContainSingle().Which.Should().BeEmpty();

        MongoDbOptions defaultOptions = connectorFactory.Get().Options;
        defaultOptions.ConnectionString.Should().NotBeNullOrEmpty();
        defaultOptions.Database.Should().NotBeNullOrEmpty();

        app.Services.GetServices<IHealthContributor>().Should().ContainSingle();
    }
}
