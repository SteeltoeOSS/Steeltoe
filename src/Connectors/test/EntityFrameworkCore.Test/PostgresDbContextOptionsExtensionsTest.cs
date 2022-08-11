// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Steeltoe.Connector.EntityFrameworkCore.Test;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Xunit;

namespace Steeltoe.Connector.PostgreSql.EntityFrameworkCore.Test;

public class PostgresDbContextOptionsExtensionsTest
{
    public PostgresDbContextOptionsExtensionsTest()
    {
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
    }

    [Fact]
    public void UseNpgsql_ThrowsIfDbContextOptionsBuilderNull()
    {
        const DbContextOptionsBuilder optionsBuilder = null;
        const DbContextOptionsBuilder<GoodDbContext> goodBuilder = null;
        const IConfigurationRoot config = null;

        var ex = Assert.Throws<ArgumentNullException>(() => optionsBuilder.UseNpgsql(config));
        Assert.Contains(nameof(optionsBuilder), ex.Message);

        var ex2 = Assert.Throws<ArgumentNullException>(() => optionsBuilder.UseNpgsql(config, "foobar"));
        Assert.Contains(nameof(optionsBuilder), ex2.Message);

        var ex3 = Assert.Throws<ArgumentNullException>(() => goodBuilder.UseNpgsql(config));
        Assert.Contains(nameof(optionsBuilder), ex3.Message);

        var ex4 = Assert.Throws<ArgumentNullException>(() => goodBuilder.UseNpgsql(config, "foobar"));
        Assert.Contains(nameof(optionsBuilder), ex4.Message);
    }

    [Fact]
    public void UseNpgsql_ThrowsIfConfigurationNull()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        var goodBuilder = new DbContextOptionsBuilder<GoodDbContext>();
        const IConfigurationRoot config = null;

        var ex = Assert.Throws<ArgumentNullException>(() => optionsBuilder.UseNpgsql(config));
        Assert.Contains(nameof(config), ex.Message);

        var ex2 = Assert.Throws<ArgumentNullException>(() => optionsBuilder.UseNpgsql(config, "foobar"));
        Assert.Contains(nameof(config), ex2.Message);

        var ex3 = Assert.Throws<ArgumentNullException>(() => goodBuilder.UseNpgsql(config));
        Assert.Contains(nameof(config), ex3.Message);

        var ex4 = Assert.Throws<ArgumentNullException>(() => goodBuilder.UseNpgsql(config, "foobar"));
        Assert.Contains(nameof(config), ex4.Message);
    }

    [Fact]
    public void UseNpgsql_ThrowsIfServiceNameNull()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        var goodBuilder = new DbContextOptionsBuilder<GoodDbContext>();
        IConfigurationRoot config = new ConfigurationBuilder().Build();
        const string serviceName = null;

        var ex2 = Assert.Throws<ArgumentException>(() => optionsBuilder.UseNpgsql(config, serviceName));
        Assert.Contains(nameof(serviceName), ex2.Message);

        var ex4 = Assert.Throws<ArgumentException>(() => goodBuilder.UseNpgsql(config, serviceName));
        Assert.Contains(nameof(serviceName), ex4.Message);
    }

    [Fact]
    public void AddDbContext_NoVCAPs_AddsDbContext_WithPostgresConnection()
    {
        IServiceCollection services = new ServiceCollection();
        IConfigurationRoot config = new ConfigurationBuilder().Build();

        services.AddDbContext<GoodDbContext>(options => options.UseNpgsql(config));

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
        IConfigurationRoot config = new ConfigurationBuilder().Build();

        services.AddDbContext<GoodDbContext>(options => options.UseNpgsql(config, "foobar"));

        var ex = Assert.Throws<ConnectorException>(() => services.BuildServiceProvider().GetService<GoodDbContext>());
        Assert.Contains("foobar", ex.Message);
    }

    [Fact]
    public void AddDbContext_MultiplePostgresServices_ThrowsConnectorException()
    {
        IServiceCollection services = new ServiceCollection();

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", PostgresTestHelpers.TwoServerVcapEdb);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot config = builder.Build();

        services.AddDbContext<GoodDbContext>(options => options.UseNpgsql(config));

        var ex = Assert.Throws<ConnectorException>(() => services.BuildServiceProvider().GetService<GoodDbContext>());
        Assert.Contains("Multiple", ex.Message);
    }

    [Fact]
    public void AddDbContexts_WithEDbVCaps_AddsDbContexts()
    {
        IServiceCollection services = new ServiceCollection();

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", PostgresTestHelpers.SingleServerVcapEdb);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot config = builder.Build();

        services.AddDbContext<GoodDbContext>(options => options.UseNpgsql(config));

        ServiceProvider built = services.BuildServiceProvider();
        var service = built.GetService<GoodDbContext>();
        Assert.NotNull(service);

        DbConnection con = service.Database.GetDbConnection();
        Assert.NotNull(con);
        var postCon = con as NpgsqlConnection;
        Assert.NotNull(postCon);

        string connString = con.ConnectionString;
        Assert.NotNull(connString);

        Assert.Contains("1e9e5dae-ed26-43e7-abb4-169b4c3beaff", connString);
        Assert.Contains("5432", connString);
        Assert.Contains("postgres.testcloud.com", connString);
        Assert.Contains("lmu7c96mgl99b2t1hvdgd5q94v", connString);
        Assert.Contains("1e9e5dae-ed26-43e7-abb4-169b4c3beaff", connString);
    }

    [Fact]
    public void AddDbContexts_WithCrunchyVCAPs_AddsDbContexts()
    {
        IServiceCollection services = new ServiceCollection();

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", PostgresTestHelpers.SingleServerVcapCrunchy);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot config = builder.Build();

        services.AddDbContext<GoodDbContext>(options => options.UseNpgsql(config));

        ServiceProvider built = services.BuildServiceProvider();
        var service = built.GetService<GoodDbContext>();
        Assert.NotNull(service);

        DbConnection con = service.Database.GetDbConnection();
        Assert.NotNull(con);
        var postCon = con as NpgsqlConnection;
        Assert.NotNull(postCon);

        string connString = con.ConnectionString;
        Assert.NotNull(connString);

        Assert.Contains("Host=10.194.59.205", connString);
        Assert.Contains("Port=5432", connString);
        Assert.Contains("Username=testrolee93ccf859894dc60dcd53218492b37b4", connString);
        Assert.Contains("Password=Qp!1mB1$Zk2T!$!D85_E", connString);
        Assert.Contains("Database=steeltoe", connString);
    }

    [Fact]
    public void AddDbContexts_WithEncodedCrunchyVCAPs_AddsDbContexts()
    {
        IServiceCollection services = new ServiceCollection();

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", PostgresTestHelpers.SingleServerEncodedVcapCrunchy);

        var appsettings = new Dictionary<string, string>();

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appsettings);
        builder.AddCloudFoundry();
        IConfigurationRoot config = builder.Build();

        services.AddDbContext<GoodDbContext>(options => options.UseNpgsql(config));

        ServiceProvider built = services.BuildServiceProvider();
        var service = built.GetService<GoodDbContext>();
        Assert.NotNull(service);

        DbConnection con = service.Database.GetDbConnection();
        Assert.NotNull(con);
        var postCon = con as NpgsqlConnection;
        Assert.NotNull(postCon);

        string connString = con.ConnectionString;
        Assert.NotNull(connString);

        Assert.Contains("Host=10.194.59.205", connString);
        Assert.Contains("Port=5432", connString);
        Assert.Contains("Username=testrolee93ccf859894dc60dcd53218492b37b4", connString);
        Assert.Contains("Password=Qp!1mB1$Zk2T!$!D85_E", connString);
        Assert.Contains("Database=steeltoe", connString);
    }
}
