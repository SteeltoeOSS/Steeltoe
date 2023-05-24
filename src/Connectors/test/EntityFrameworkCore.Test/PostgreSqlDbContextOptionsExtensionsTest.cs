// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Connectors.EntityFrameworkCore.PostgreSql;
using Xunit;

namespace Steeltoe.Connectors.EntityFrameworkCore.Test;

public class PostgreSqlDbContextOptionsExtensionsTest
{
    public PostgreSqlDbContextOptionsExtensionsTest()
    {
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
    }

    [Fact]
    public void UseNpgsql_ThrowsIfDbContextOptionsBuilderNull()
    {
        const DbContextOptionsBuilder optionsBuilder = null;
        const DbContextOptionsBuilder<GoodDbContext> goodBuilder = null;
        const IConfigurationRoot configurationRoot = null;

        var ex = Assert.Throws<ArgumentNullException>(() => optionsBuilder.UseNpgsql(configurationRoot));
        Assert.Contains(nameof(optionsBuilder), ex.Message, StringComparison.Ordinal);

        var ex2 = Assert.Throws<ArgumentNullException>(() => optionsBuilder.UseNpgsql(configurationRoot, "foobar"));
        Assert.Contains(nameof(optionsBuilder), ex2.Message, StringComparison.Ordinal);

        var ex3 = Assert.Throws<ArgumentNullException>(() => goodBuilder.UseNpgsql(configurationRoot));
        Assert.Contains(nameof(optionsBuilder), ex3.Message, StringComparison.Ordinal);

        var ex4 = Assert.Throws<ArgumentNullException>(() => goodBuilder.UseNpgsql(configurationRoot, "foobar"));
        Assert.Contains(nameof(optionsBuilder), ex4.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UseNpgsql_ThrowsIfConfigurationNull()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        var goodBuilder = new DbContextOptionsBuilder<GoodDbContext>();
        const IConfigurationRoot configuration = null;

        var ex = Assert.Throws<ArgumentNullException>(() => optionsBuilder.UseNpgsql(configuration));
        Assert.Contains(nameof(configuration), ex.Message, StringComparison.Ordinal);

        var ex2 = Assert.Throws<ArgumentNullException>(() => optionsBuilder.UseNpgsql(configuration, "foobar"));
        Assert.Contains(nameof(configuration), ex2.Message, StringComparison.Ordinal);

        var ex3 = Assert.Throws<ArgumentNullException>(() => goodBuilder.UseNpgsql(configuration));
        Assert.Contains(nameof(configuration), ex3.Message, StringComparison.Ordinal);

        var ex4 = Assert.Throws<ArgumentNullException>(() => goodBuilder.UseNpgsql(configuration, "foobar"));
        Assert.Contains(nameof(configuration), ex4.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UseNpgsql_ThrowsIfServiceNameNull()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        var goodBuilder = new DbContextOptionsBuilder<GoodDbContext>();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();
        const string serviceName = null;

        var ex2 = Assert.Throws<ArgumentNullException>(() => optionsBuilder.UseNpgsql(configurationRoot, serviceName));
        Assert.Contains(nameof(serviceName), ex2.Message, StringComparison.Ordinal);

        var ex4 = Assert.Throws<ArgumentNullException>(() => goodBuilder.UseNpgsql(configurationRoot, serviceName));
        Assert.Contains(nameof(serviceName), ex4.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddDbContext_NoVCAPs_AddsDbContext_WithPostgreSqlConnection()
    {
        IServiceCollection services = new ServiceCollection();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        services.AddDbContext<GoodDbContext>(options => options.UseNpgsql(configurationRoot));

        var service = services.BuildServiceProvider().GetService<GoodDbContext>();
        Assert.NotNull(service);
        DbConnection con = service.Database.GetDbConnection();
        Assert.NotNull(con);
        Assert.NotNull(con as NpgsqlConnection);
    }

    [Fact]
    public void AddDbContext_WithServiceName_NoVCAPs_ThrowsConnectorException()
    {
        IServiceCollection services = new ServiceCollection();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        services.AddDbContext<GoodDbContext>(options => options.UseNpgsql(configurationRoot, "foobar"));

        var ex = Assert.Throws<ConnectorException>(() => services.BuildServiceProvider().GetService<GoodDbContext>());
        Assert.Contains("foobar", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddDbContext_MultiplePostgreSqlServices_ThrowsConnectorException()
    {
        IServiceCollection services = new ServiceCollection();

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", PostgreSqlTestHelpers.TwoServerVcapEdb);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        services.AddDbContext<GoodDbContext>(options => options.UseNpgsql(configurationRoot));

        var ex = Assert.Throws<ConnectorException>(() => services.BuildServiceProvider().GetService<GoodDbContext>());
        Assert.Contains("Multiple", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddDbContexts_WithEDbVCaps_AddsDbContexts()
    {
        IServiceCollection services = new ServiceCollection();

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", PostgreSqlTestHelpers.SingleServerVcapEdb);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        services.AddDbContext<GoodDbContext>(options => options.UseNpgsql(configurationRoot));

        ServiceProvider built = services.BuildServiceProvider();
        var service = built.GetService<GoodDbContext>();
        Assert.NotNull(service);

        DbConnection con = service.Database.GetDbConnection();
        Assert.NotNull(con);
        var postCon = con as NpgsqlConnection;
        Assert.NotNull(postCon);

        string connString = con.ConnectionString;
        Assert.NotNull(connString);

        Assert.Contains("1e9e5dae-ed26-43e7-abb4-169b4c3beaff", connString, StringComparison.Ordinal);
        Assert.Contains("5432", connString, StringComparison.Ordinal);
        Assert.Contains("postgres.testcloud.com", connString, StringComparison.Ordinal);
        Assert.Contains("lmu7c96mgl99b2t1hvdgd5q94v", connString, StringComparison.Ordinal);
        Assert.Contains("1e9e5dae-ed26-43e7-abb4-169b4c3beaff", connString, StringComparison.Ordinal);
    }

    [Fact]
    public void AddDbContexts_WithCrunchyVCAPs_AddsDbContexts()
    {
        IServiceCollection services = new ServiceCollection();

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", PostgreSqlTestHelpers.SingleServerVcapCrunchy);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        services.AddDbContext<GoodDbContext>(options => options.UseNpgsql(configurationRoot));

        ServiceProvider built = services.BuildServiceProvider();
        var service = built.GetService<GoodDbContext>();
        Assert.NotNull(service);

        DbConnection con = service.Database.GetDbConnection();
        Assert.NotNull(con);
        var postCon = con as NpgsqlConnection;
        Assert.NotNull(postCon);

        string connString = con.ConnectionString;
        Assert.NotNull(connString);

        Assert.Contains("Host=10.194.59.205", connString, StringComparison.Ordinal);
        Assert.Contains("Port=5432", connString, StringComparison.Ordinal);
        Assert.Contains("Username=testrolee93ccf859894dc60dcd53218492b37b4", connString, StringComparison.Ordinal);
        Assert.Contains("Password=Qp!1mB1$Zk2T!$!D85_E", connString, StringComparison.Ordinal);
        Assert.Contains("Database=steeltoe", connString, StringComparison.Ordinal);
    }

    [Fact]
    public void AddDbContexts_WithEncodedCrunchyVCAPs_AddsDbContexts()
    {
        IServiceCollection services = new ServiceCollection();

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", PostgreSqlTestHelpers.SingleServerEncodedVcapCrunchy);

        var appsettings = new Dictionary<string, string>();

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appsettings);
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        services.AddDbContext<GoodDbContext>(options => options.UseNpgsql(configurationRoot));

        ServiceProvider built = services.BuildServiceProvider();
        var service = built.GetService<GoodDbContext>();
        Assert.NotNull(service);

        DbConnection con = service.Database.GetDbConnection();
        Assert.NotNull(con);
        var postCon = con as NpgsqlConnection;
        Assert.NotNull(postCon);

        string connString = con.ConnectionString;
        Assert.NotNull(connString);

        Assert.Contains("Host=10.194.59.205", connString, StringComparison.Ordinal);
        Assert.Contains("Port=5432", connString, StringComparison.Ordinal);
        Assert.Contains("Username=testrolee93ccf859894dc60dcd53218492b37b4", connString, StringComparison.Ordinal);
        Assert.Contains("Password=Qp!1mB1$Zk2T!$!D85_E", connString, StringComparison.Ordinal);
        Assert.Contains("Database=steeltoe", connString, StringComparison.Ordinal);
    }
}
