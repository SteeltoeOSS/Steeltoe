// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Connector.EntityFrameworkCore.SqlServer;
using Xunit;

namespace Steeltoe.Connector.EntityFrameworkCore.Test;

public class SqlServerDbContextOptionsExtensionsTest
{
    public SqlServerDbContextOptionsExtensionsTest()
    {
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
    }

    [Fact]
    public void UseSqlServer_ThrowsIfDbContextOptionsBuilderNull()
    {
        const DbContextOptionsBuilder optionsBuilder = null;
        const DbContextOptionsBuilder<GoodDbContext> goodBuilder = null;
        const IConfigurationRoot configurationRoot = null;

        var ex = Assert.Throws<ArgumentNullException>(() => optionsBuilder.UseSqlServer(configurationRoot));
        Assert.Contains(nameof(optionsBuilder), ex.Message, StringComparison.Ordinal);

        var ex2 = Assert.Throws<ArgumentNullException>(() => optionsBuilder.UseSqlServer(configurationRoot, "foobar"));
        Assert.Contains(nameof(optionsBuilder), ex2.Message, StringComparison.Ordinal);

        var ex3 = Assert.Throws<ArgumentNullException>(() => goodBuilder.UseSqlServer(configurationRoot));
        Assert.Contains(nameof(optionsBuilder), ex3.Message, StringComparison.Ordinal);

        var ex4 = Assert.Throws<ArgumentNullException>(() => goodBuilder.UseSqlServer(configurationRoot, "foobar"));
        Assert.Contains(nameof(optionsBuilder), ex4.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UseSqlServer_ThrowsIfConfigurationNull()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        var goodBuilder = new DbContextOptionsBuilder<GoodDbContext>();
        const IConfigurationRoot configuration = null;

        var ex = Assert.Throws<ArgumentNullException>(() => optionsBuilder.UseSqlServer(configuration));
        Assert.Contains(nameof(configuration), ex.Message, StringComparison.Ordinal);

        var ex2 = Assert.Throws<ArgumentNullException>(() => optionsBuilder.UseSqlServer(configuration, "foobar"));
        Assert.Contains(nameof(configuration), ex2.Message, StringComparison.Ordinal);

        var ex3 = Assert.Throws<ArgumentNullException>(() => goodBuilder.UseSqlServer(configuration));
        Assert.Contains(nameof(configuration), ex3.Message, StringComparison.Ordinal);

        var ex4 = Assert.Throws<ArgumentNullException>(() => goodBuilder.UseSqlServer(configuration, "foobar"));
        Assert.Contains(nameof(configuration), ex4.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UseSqlServer_ThrowsIfServiceNameNull()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        var goodBuilder = new DbContextOptionsBuilder<GoodDbContext>();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();
        const string serviceName = null;

        var ex2 = Assert.Throws<ArgumentNullException>(() => optionsBuilder.UseSqlServer(configurationRoot, serviceName));
        Assert.Contains(nameof(serviceName), ex2.Message, StringComparison.Ordinal);

        var ex4 = Assert.Throws<ArgumentNullException>(() => goodBuilder.UseSqlServer(configurationRoot, serviceName));
        Assert.Contains(nameof(serviceName), ex4.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddDbContext_NoVCAPs_AddsDbContext_WithSqlServerConnection()
    {
        IServiceCollection services = new ServiceCollection();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        services.AddDbContext<GoodDbContext>(options => options.UseSqlServer(configurationRoot));

        var service = services.BuildServiceProvider().GetService<GoodDbContext>();
        Assert.NotNull(service);
        DbConnection con = service.Database.GetDbConnection();
        Assert.NotNull(con);
        Assert.IsType<SqlConnection>(con);
    }

    [Fact]
    public void AddDbContext_WithServiceName_NoVCAPs_ThrowsConnectorException()
    {
        IServiceCollection services = new ServiceCollection();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        services.AddDbContext<GoodDbContext>(options => options.UseSqlServer(configurationRoot, "foobar"));

        var ex = Assert.Throws<ConnectorException>(() => services.BuildServiceProvider().GetService<GoodDbContext>());
        Assert.Contains("foobar", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddDbContext_MultipleSqlServerServices_ThrowsConnectorException()
    {
        IServiceCollection services = new ServiceCollection();

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", SqlServerTestHelpers.TwoServerVcap);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        services.AddDbContext<GoodDbContext>(options => options.UseSqlServer(configurationRoot));

        var ex = Assert.Throws<ConnectorException>(() => services.BuildServiceProvider().GetService<GoodDbContext>());
        Assert.Contains("Multiple", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddDbContexts_WithVCAPs_AddsDbContexts()
    {
        IServiceCollection services = new ServiceCollection();

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", SqlServerTestHelpers.SingleServerVcap);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        services.AddDbContext<GoodDbContext>(options => options.UseSqlServer(configurationRoot));

        ServiceProvider built = services.BuildServiceProvider();
        var service = built.GetService<GoodDbContext>();
        Assert.NotNull(service);

        DbConnection con = service.Database.GetDbConnection();
        Assert.NotNull(con);
        Assert.IsType<SqlConnection>(con);

        string connString = con.ConnectionString;
        Assert.NotNull(connString);
        Assert.Contains("Initial Catalog=de5aa3a747c134b3d8780f8cc80be519e", connString, StringComparison.Ordinal);
        Assert.Contains("Data Source=192.168.0.80", connString, StringComparison.Ordinal);
        Assert.Contains("User Id=uf33b2b30783a4087948c30f6c3b0c90f", connString, StringComparison.Ordinal);
        Assert.Contains("Password=Pefbb929c1e0945b5bab5b8f0d110c503", connString, StringComparison.Ordinal);
    }

    [Fact]
    public void AddDbContexts_WithAzureVCAPs_AddsDbContexts()
    {
        IServiceCollection services = new ServiceCollection();

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", SqlServerTestHelpers.SingleServerAzureVcap);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        services.AddDbContext<GoodDbContext>(options => options.UseSqlServer(configurationRoot));

        ServiceProvider built = services.BuildServiceProvider();
        var service = built.GetService<GoodDbContext>();
        Assert.NotNull(service);

        DbConnection con = service.Database.GetDbConnection();
        Assert.NotNull(con);
        Assert.IsType<SqlConnection>(con);

        string connString = con.ConnectionString;
        Assert.NotNull(connString);
        Assert.Contains("Initial Catalog=u9e44b3e8e31", connString, StringComparison.Ordinal);
        Assert.Contains("Data Source=ud6893c77875.database.windows.net", connString, StringComparison.Ordinal);
        Assert.Contains("User Id=ud61c2c9ed2a", connString, StringComparison.Ordinal);
        Assert.Contains("Password=yNOaMnbsjGT3qk5eW6BXcbHE6b2Da8sLcao7SdIFFqA2q345jQ9RSw==", connString, StringComparison.Ordinal);
    }
}
