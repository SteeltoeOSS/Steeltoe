// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Configuration.CloudFoundry.ServiceBindings;

namespace Steeltoe.Connectors;

public sealed class ConnectorConfigureOptionsBuilder
{
    internal CloudFoundryServiceBrokerTypes CloudFoundryBrokerTypes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether connection string changes are detected while the application is running. This is <c>false</c> by default to
    /// optimize startup performance. When set to <c>true</c>, existing configuration providers may get reloaded multiple times, potentially resulting in
    /// duplicate expensive calls. Be aware that detecting configuration changes only makes sense when
    /// <see cref="ConnectorAddOptionsBuilder.CacheConnection" /> is <c>false</c>.
    /// </summary>
    public bool DetectConfigurationChanges { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to turn off the built-in service broker support. This is <c>false</c> by default, but should be set to
    /// <c>true</c> when using custom logic to convert platform-based credentials to driver-specific configuration keys.
    /// <para>
    /// <example>
    /// For example, to use a third-party Cloud Foundry service broker that sets the
    /// <c>
    /// VCAP_SERVICES
    /// </c>
    /// environment variable to:
    /// <code>
    /// {
    ///   "custom-postgres-broker": [
    ///     {
    ///       "name": "products-db",
    ///       "credentials": {
    ///         "custom-hostname-key": "example.cloud.com",
    ///         "custom-port-key": 2345,
    ///         "custom-username-key": "products-user",
    ///         "custom-password-key": "products-secret",
    ///         "custom-database-name-key": "product-database"
    ///       }
    ///     },
    ///     {
    ///       "name": "orders-db",
    ///       "credentials": {
    ///         "custom-hostname-key": "example.cloud.com",
    ///         "custom-port-key": 2345,
    ///         "custom-username-key": "orders-user",
    ///         "custom-password-key": "orders-secret",
    ///         "custom-database-name-key": "order-database"
    ///       }
    ///     }
    ///   ]
    /// }
    /// </code>
    /// The following code can be used to map the PostgreSQL credentials to the format that
    /// <see href="https://www.npgsql.org/doc/api/Npgsql.NpgsqlConnectionStringBuilder.html">
    /// NpgsqlConnectionStringBuilder
    /// </see>
    /// expects:
    /// <code><![CDATA[
    /// var builder = WebApplication.CreateBuilder();
    /// builder.AddCloudFoundryConfiguration();
    /// MapCustomServiceBindings("custom-postgres-broker");
    /// builder.AddPostgreSql(configure => configure.SkipDefaultServiceBindings = true, null);
    /// var app = builder.Build();
    /// 
    /// var factory = app.Services.GetRequiredService<ConnectorFactory<PostgreSqlOptions, NpgsqlConnection>>();
    /// 
    /// PostgreSqlOptions productsDbOptions = factory.Get("products-db").Options;
    /// Console.WriteLine(productsDbOptions.ConnectionString);
    /// // Database=product-database;Host=example.cloud.com;Password=products-secret;Port=2345;Username=products-user
    /// 
    /// PostgreSqlOptions ordersDbOptions = factory.Get("orders-db").Options;
    /// Console.WriteLine(ordersDbOptions.ConnectionString);
    /// // Database=order-database;Host=example.cloud.com;Password=orders-secret;Port=2345;Username=orders-user
    /// 
    /// void MapCustomServiceBindings(string brokerName)
    /// {
    ///     var options = builder.Configuration.GetSection("vcap").Get<CloudFoundryServicesOptions>();
    /// 
    ///     foreach (CloudFoundryService service in options?.Services
    ///         .Where(pair => pair.Key == brokerName)
    ///         .SelectMany(pair => pair.Value) ?? [])
    ///     {
    ///         builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    ///         {
    ///             // Map credentials into the property names expected by NpgsqlConnectionStringBuilder.
    ///             [$"steeltoe:service-bindings:postgresql:{service.Name}:host"] = service.Credentials["custom-hostname-key"].Value,
    ///             [$"steeltoe:service-bindings:postgresql:{service.Name}:port"] = service.Credentials["custom-port-key"].Value,
    ///             [$"steeltoe:service-bindings:postgresql:{service.Name}:username"] = service.Credentials["custom-username-key"].Value,
    ///             [$"steeltoe:service-bindings:postgresql:{service.Name}:password"] = service.Credentials["custom-password-key"].Value,
    ///             [$"steeltoe:service-bindings:postgresql:{service.Name}:database"] = service.Credentials["custom-database-name-key"].Value
    ///         });
    ///     }
    /// }
    /// ]]></code>
    /// </example>
    /// </para>
    /// </summary>
    public bool SkipDefaultServiceBindings { get; set; }
}
