// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Configuration.Kubernetes.ServiceBinding;
using Steeltoe.Connector.PostgreSql;
using Xunit;

namespace Steeltoe.Connector.Test.PostgreSQL;

public class PostgreSqlProviderConnectorOptionsTest
{
    [Fact]
    public void Constructor_ThrowsIfConfigNull()
    {
        const IConfiguration configuration = null;

        var ex = Assert.Throws<ArgumentNullException>(() => new PostgreSqlProviderConnectorOptions(configuration));
        Assert.Contains(nameof(configuration), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_BindsValues()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["postgres:client:host"] = "localhost",
            ["postgres:client:port"] = "1234",
            ["postgres:client:password"] = "password",
            ["postgres:client:username"] = "username",
            ["postgres:client:searchpath"] = "searchpath"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        var options = new PostgreSqlProviderConnectorOptions(configurationRoot);
        Assert.Equal("localhost", options.Host);
        Assert.Equal(1234, options.Port);
        Assert.Equal("password", options.Password);
        Assert.Equal("username", options.Username);
        Assert.Equal("searchpath", options.SearchPath);
        Assert.Null(options.ConnectionString);
    }

    [Fact]
    public void ConnectionString_Returned_AsConfigured()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["postgres:client:ConnectionString"] = "Server=fake;Database=test;User Id=steeltoe;Password=password;"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        var options = new PostgreSqlProviderConnectorOptions(configurationRoot);

        Assert.StartsWith(appsettings["postgres:client:ConnectionString"], options.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ConnectionString_Returned_BuildFromConfig()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["postgres:client:Host"] = "fake-db.host",
            ["postgres:client:Port"] = "3000",
            ["postgres:client:Username"] = "fakeUsername",
            ["postgres:client:Password"] = "fakePassword",
            ["postgres:client:Database"] = "fakeDB",
            ["postgres:client:SearchPath"] = "fakeSchema"
        };

        const string expected =
            "Host=fake-db.host;Port=3000;Username=fakeUsername;Password=fakePassword;Database=fakeDB;Timeout=15;Command Timeout=30;Search Path=fakeSchema";

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        var options = new PostgreSqlProviderConnectorOptions(configurationRoot);

        Assert.Equal(expected, options.ToString());
    }

    [Fact]
    public void ConnectionString_Overridden_By_CloudFoundryConfig()
    {
        // simulate an appsettings file
        var appsettings = new Dictionary<string, string>
        {
            ["postgres:client:ConnectionString"] = "Server=fake;Database=test;User Id=steeltoe;Password=password;"
        };

        // add environment variables as Cloud Foundry would
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", PostgreSqlTestHelpers.SingleServerVcapEdb);

        // add settings to configuration
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        configurationBuilder.AddEnvironmentVariables();
        configurationBuilder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        var options = new PostgreSqlProviderConnectorOptions(configurationRoot);

        Assert.NotEqual(appsettings["postgres:client:ConnectionString"], options.ToString());
    }

    [Fact]
    public void ConnectionStringIgnoredWhenCloudNativeBindingsExist()
    {
        // simulate an appsettings file
        var appsettings = new Dictionary<string, string>
        {
            { "postgres:client:ConnectionString", "Server=fake;Database=test;User Id=steeltoe;Password=password;" }
        };

        // add environment variables as Cloud Foundry would
        string rootDir = Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "resources", "bindings");
        Environment.SetEnvironmentVariable("SERVICE_BINDING_ROOT", rootDir);

        // add settings to configuration
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        configurationBuilder.AddEnvironmentVariables();
        configurationBuilder.AddKubernetesServiceBindings();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        var options = new PostgreSqlProviderConnectorOptions(configurationRoot);
        string connString = options.ToString();
        Assert.NotEqual(appsettings["postgres:client:ConnectionString"], connString);
        Assert.Contains("Host=10.194.59.205", connString, StringComparison.Ordinal);
        Assert.Contains("Port=5432", connString, StringComparison.Ordinal);
        Assert.Contains("Username=testrolee93ccf859894dc60dcd53218492b37b4", connString, StringComparison.Ordinal);
        Assert.Contains("Password=Qp!1mB1$Zk2T!$!D85_E", connString, StringComparison.Ordinal);
        Assert.Contains("Database=steeltoe", connString, StringComparison.Ordinal);
    }

    [Fact]
    public void ConnectionString_Overridden_By_CloudFoundryConfig_Use_SearchPath()
    {
        // simulate an appsettings file
        var appsettings = new Dictionary<string, string>
        {
            ["postgres:client:ConnectionString"] = "Server=fake;Database=test;User Id=steeltoe;Password=password;",
            ["postgres:client:SearchPath"] = "SomeSchema"
        };

        // add environment variables as Cloud Foundry would
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", PostgreSqlTestHelpers.SingleServerVcapEdb);

        // add settings to configuration
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        configurationBuilder.AddEnvironmentVariables();
        configurationBuilder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        var options = new PostgreSqlProviderConnectorOptions(configurationRoot);

        Assert.DoesNotContain(appsettings["postgres:client:ConnectionString"], options.ToString(), StringComparison.Ordinal);
        Assert.EndsWith($"Search Path={options.SearchPath}", options.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ConnectionStringWithoutTrailingSemicolonResultsInValidConnection()
    {
        var appSettings = new Dictionary<string, string>
        {
            ["postgres:client:ConnectionString"] = "Server=fake;Database=test;User Id=steeltoe;Password=password",
            ["postgres:client:SearchPath"] = "SomeSchema"
        };

        // add settings to config
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appSettings);
        configurationBuilder.AddEnvironmentVariables();
        IConfigurationRoot config = configurationBuilder.Build();

        var connectorOptions = new PostgreSqlProviderConnectorOptions(config);

        string expectedConnection =
            $"{appSettings["postgres:client:ConnectionString"]};Timeout={connectorOptions.Timeout};Command Timeout={connectorOptions.CommandTimeout};Search Path={connectorOptions.SearchPath}";

        Assert.Equal(expectedConnection, connectorOptions.ToString());
    }
}
