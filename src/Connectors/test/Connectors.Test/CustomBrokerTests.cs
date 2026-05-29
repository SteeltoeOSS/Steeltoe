// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Configuration.CloudFoundry.ServiceBindings;
using Steeltoe.Connectors.PostgreSql;
using Steeltoe.Connectors.RabbitMQ;

namespace Steeltoe.Connectors.Test;

public sealed class CustomBrokerTests
{
    [Fact]
    public async Task Binds_options_with_third_party_service_bindings()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Steeltoe:Client:PostgreSql:products-db:ConnectionString"] = "Include Error Detail=true;host=localhost",
            ["Steeltoe:Client:PostgreSql:orders-db:ConnectionString"] = "Log Parameters=true;port=9999"
        };

        var reader = new CloudFoundryMemorySettingsReader
        {
            ServicesJson = """
                {
                  "custom-postgres-broker": [
                    {
                      "name": "products-db",
                      "credentials": {
                        "custom-hostname-key": "example.cloud.com",
                        "custom-port-key": 2345,
                        "custom-username-key": "products-user",
                        "custom-password-key": "products-secret",
                        "custom-database-name-key": "product-database",
                        "host": "IGNORED"
                      }
                    },
                    {
                      "name": "orders-db",
                      "credentials": {
                        "custom-hostname-key": "example.cloud.com",
                        "custom-port-key": 2345,
                        "custom-username-key": "orders-user",
                        "custom-password-key": "orders-secret",
                        "custom-database-name-key": "order-database"
                      }
                    }
                  ]
                }
                """
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Configuration.AddCloudFoundry(reader);
        MapServiceBindingsForCustomPostgreSqlBroker("custom-postgres-broker");
        builder.AddPostgreSql(options => options.SkipDefaultServiceBindings = true, null);
        await using WebApplication app = builder.Build();

        var optionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<PostgreSqlOptions>>();
        PostgreSqlOptions productsDbOptions = optionsMonitor.Get("products-db");
        PostgreSqlOptions ordersDbOptions = optionsMonitor.Get("orders-db");

        ExtractConnectionStringParameters(productsDbOptions.ConnectionString).Should().BeEquivalentTo(new List<string>
        {
            "Include Error Detail=True",
            "Host=example.cloud.com",
            "Port=2345",
            "Database=product-database",
            "Username=products-user",
            "Password=products-secret"
        }, options => options.WithoutStrictOrdering());

        ExtractConnectionStringParameters(ordersDbOptions.ConnectionString).Should().BeEquivalentTo(new List<string>
        {
            "Log Parameters=True",
            "Host=example.cloud.com",
            "Port=2345",
            "Database=order-database",
            "Username=orders-user",
            "Password=orders-secret"
        }, options => options.WithoutStrictOrdering());

        void MapServiceBindingsForCustomPostgreSqlBroker(string brokerName)
        {
            var options = builder.Configuration.GetSection("vcap").Get<CloudFoundryServicesOptions>();

            foreach (CloudFoundryService service in options?.Services.Where(pair => pair.Key == brokerName).SelectMany(pair => pair.Value) ?? [])
            {
                builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    // Map credentials into the property names expected by NpgsqlConnectionStringBuilder.
                    [$"steeltoe:service-bindings:postgresql:{service.Name}:host"] = service.Credentials["custom-hostname-key"].Value,
                    [$"steeltoe:service-bindings:postgresql:{service.Name}:port"] = service.Credentials["custom-port-key"].Value,
                    [$"steeltoe:service-bindings:postgresql:{service.Name}:username"] = service.Credentials["custom-username-key"].Value,
                    [$"steeltoe:service-bindings:postgresql:{service.Name}:password"] = service.Credentials["custom-password-key"].Value,
                    [$"steeltoe:service-bindings:postgresql:{service.Name}:database"] = service.Credentials["custom-database-name-key"].Value
                });
            }
        }
    }

    [Fact]
    public async Task Third_party_service_bindings_can_be_combined_with_builtin()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Steeltoe:Client:PostgreSql:products-db:ConnectionString"] = "Include Error Detail=true;host=localhost",
            ["Steeltoe:Client:PostgreSql:orders-db:ConnectionString"] = "Log Parameters=true;port=9999",
            ["Steeltoe:Client:RabbitMQ:transaction-queue:ConnectionString"] = "amqp://localhost?connection_timeout=5000&heartbeat=5&unknown=local"
        };

        const string vcapServicesJson = """
            {
              "postgres": [
                {
                  "name": "products-db",
                  "credentials": {
                    "custom-hostname-key": "example.cloud.com",
                    "custom-port-key": 2345,
                    "custom-username-key": "products-user",
                    "custom-password-key": "products-secret",
                    "custom-database-name-key": "product-database"
                  }
                },
                {
                  "name": "orders-db",
                  "credentials": {
                    "custom-hostname-key": "example.cloud.com",
                    "custom-port-key": 2345,
                    "custom-username-key": "orders-user",
                    "custom-password-key": "orders-secret",
                    "custom-database-name-key": "order-database"
                  }
                }
              ],
              "p.rabbitmq": [
                {
                  "name": "transaction-queue",
                  "tags": [
                    "rabbitmq"
                  ],
                  "credentials": {
                    "protocols": {
                      "amqp+ssl": {
                        "host": "messaging.cloud.com",
                        "port": 2765,
                        "username": "app-user",
                        "password": "secret",
                        "vhost": "transactions"
                      }
                    },
                    "ssl": true,
                    "unknown": "remote"
                  }
                }
              ]
            }
            """;

        var reader = new CloudFoundryMemorySettingsReader
        {
            ServicesJson = vcapServicesJson
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Configuration.AddCloudFoundry(reader);
        MapServiceBindingsForCustomPostgreSqlBroker("postgres");
        builder.AddPostgreSql(options => options.SkipDefaultServiceBindings = true, null);
        MapExtraRabbitMQServiceBindingsUsingDefaultBroker("p.rabbitmq");
        builder.AddRabbitMQ(null, null, new StringServiceBindingsReader(vcapServicesJson));
        await using WebApplication app = builder.Build();

        var postgreSqlOptionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<PostgreSqlOptions>>();
        PostgreSqlOptions productsDbOptions = postgreSqlOptionsMonitor.Get("products-db");
        PostgreSqlOptions ordersDbOptions = postgreSqlOptionsMonitor.Get("orders-db");

        var rabbitMQOptionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<RabbitMQOptions>>();
        RabbitMQOptions transactionQueueOptions = rabbitMQOptionsMonitor.Get("transaction-queue");

        ExtractConnectionStringParameters(productsDbOptions.ConnectionString).Should().BeEquivalentTo(new List<string>
        {
            "Include Error Detail=True",
            "Host=example.cloud.com",
            "Port=2345",
            "Database=product-database",
            "Username=products-user",
            "Password=products-secret"
        }, options => options.WithoutStrictOrdering());

        ExtractConnectionStringParameters(ordersDbOptions.ConnectionString).Should().BeEquivalentTo(new List<string>
        {
            "Log Parameters=True",
            "Host=example.cloud.com",
            "Port=2345",
            "Database=order-database",
            "Username=orders-user",
            "Password=orders-secret"
        }, options => options.WithoutStrictOrdering());

        transactionQueueOptions.ConnectionString.Should().Be(
            "amqps://app-user:secret@messaging.cloud.com:2765/transactions?connection_timeout=5000&heartbeat=5&unknown=remote");

        void MapServiceBindingsForCustomPostgreSqlBroker(string brokerName)
        {
            var options = builder.Configuration.GetSection("vcap").Get<CloudFoundryServicesOptions>();

            foreach (CloudFoundryService service in options?.Services.Where(pair => pair.Key == brokerName).SelectMany(pair => pair.Value) ?? [])
            {
                builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    // Map third-party credentials into the property names expected by NpgsqlConnectionStringBuilder.
                    [$"steeltoe:service-bindings:postgresql:{service.Name}:host"] = service.Credentials["custom-hostname-key"].Value,
                    [$"steeltoe:service-bindings:postgresql:{service.Name}:port"] = service.Credentials["custom-port-key"].Value,
                    [$"steeltoe:service-bindings:postgresql:{service.Name}:username"] = service.Credentials["custom-username-key"].Value,
                    [$"steeltoe:service-bindings:postgresql:{service.Name}:password"] = service.Credentials["custom-password-key"].Value,
                    [$"steeltoe:service-bindings:postgresql:{service.Name}:database"] = service.Credentials["custom-database-name-key"].Value
                });
            }
        }

        void MapExtraRabbitMQServiceBindingsUsingDefaultBroker(string brokerName)
        {
            var options = builder.Configuration.GetSection("vcap").Get<CloudFoundryServicesOptions>();

            foreach (CloudFoundryService service in options?.Services.Where(pair => pair.Key == brokerName).SelectMany(pair => pair.Value) ?? [])
            {
                builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    // Map third-party 'unknown' credential, in addition to built-in broker parameters.
                    [$"steeltoe:service-bindings:rabbitmq:{service.Name}:unknown"] = service.Credentials["unknown"].Value
                });
            }
        }
    }

    private static List<string> ExtractConnectionStringParameters(string? connectionString)
    {
        List<string> entries = [];

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
