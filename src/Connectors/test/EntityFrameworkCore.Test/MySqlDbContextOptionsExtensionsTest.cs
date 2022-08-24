// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Connector.EntityFrameworkCore;
using Steeltoe.Connector.EntityFrameworkCore.Test;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Xunit;
using OfficialMySqlConnection = MySql.Data.MySqlClient.MySqlConnection;
using PomeloMySqlConnection = MySqlConnector.MySqlConnection;

namespace Steeltoe.Connector.MySql.EntityFrameworkCore.Test;

public class MySqlDbContextOptionsExtensionsTest
{
    public MySqlDbContextOptionsExtensionsTest()
    {
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
    }

    [Fact]
    public void UseMySql_ThrowsIfDbContextOptionsBuilderNull()
    {
        const DbContextOptionsBuilder optionsBuilder = null;
        const DbContextOptionsBuilder<GoodDbContext> goodBuilder = null;
        const IConfigurationRoot configurationRoot = null;

        var ex = Assert.Throws<ArgumentNullException>(() => optionsBuilder.UseMySql(configurationRoot));
        Assert.Contains(nameof(optionsBuilder), ex.Message);

        var ex2 = Assert.Throws<ArgumentNullException>(() => optionsBuilder.UseMySql(configurationRoot, "foobar"));
        Assert.Contains(nameof(optionsBuilder), ex2.Message);

        var ex3 = Assert.Throws<ArgumentNullException>(() => goodBuilder.UseMySql(configurationRoot));
        Assert.Contains(nameof(optionsBuilder), ex3.Message);

        var ex4 = Assert.Throws<ArgumentNullException>(() => goodBuilder.UseMySql(configurationRoot, "foobar"));
        Assert.Contains(nameof(optionsBuilder), ex4.Message);
    }

    [Fact]
    public void UseMySql_ThrowsIfConfigurationNull()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        var goodBuilder = new DbContextOptionsBuilder<GoodDbContext>();
        const IConfigurationRoot configuration = null;

        var ex = Assert.Throws<ArgumentNullException>(() => optionsBuilder.UseMySql(configuration));
        Assert.Contains(nameof(configuration), ex.Message);

        var ex2 = Assert.Throws<ArgumentNullException>(() => optionsBuilder.UseMySql(configuration, "foobar"));
        Assert.Contains(nameof(configuration), ex2.Message);

        var ex3 = Assert.Throws<ArgumentNullException>(() => goodBuilder.UseMySql(configuration));
        Assert.Contains(nameof(configuration), ex3.Message);

        var ex4 = Assert.Throws<ArgumentNullException>(() => goodBuilder.UseMySql(configuration, "foobar"));
        Assert.Contains(nameof(configuration), ex4.Message);
    }

    [Fact]
    public void UseMySql_ThrowsIfServiceNameNull()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        var goodBuilder = new DbContextOptionsBuilder<GoodDbContext>();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();
        const string serviceName = null;

        var ex2 = Assert.Throws<ArgumentNullException>(() => optionsBuilder.UseMySql(configurationRoot, serviceName));
        Assert.Contains(nameof(serviceName), ex2.Message);

        var ex4 = Assert.Throws<ArgumentNullException>(() => goodBuilder.UseMySql(configurationRoot, serviceName));
        Assert.Contains(nameof(serviceName), ex4.Message);
    }

    [Theory]
    [InlineData(new[]
    {
        "Pomelo.EntityFrameworkCore.MySql"
    }, typeof(PomeloMySqlConnection))]
    [InlineData(new[]
    {
        "MySql.EntityFrameworkCore",
        "MySql.Data.EntityFrameworkCore"
    }, typeof(OfficialMySqlConnection))]
    public void AddDbContext_NoVCAPs_AddsDbContext_WithMySqlConnection(string[] efAssemblies, Type mySqlConnectionType)
    {
        using var scope = new AlternateTypeLocatorScope(efAssemblies, mySqlConnectionType);
        IServiceCollection services = new ServiceCollection();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        AddMySqlDbContext(services, configurationRoot);

        var service = services.BuildServiceProvider().GetService<GoodDbContext>();
        Assert.NotNull(service);
        DbConnection con = service.Database.GetDbConnection();
        Assert.NotNull(con);
        Assert.True(con.GetType() == mySqlConnectionType);
    }

    [Fact]
    public void AddDbContext_WithServiceName_NoVCAPs_ThrowsConnectorException()
    {
        IServiceCollection services = new ServiceCollection();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        services.AddDbContext<GoodDbContext>(options => options.UseMySql(configurationRoot, "foobar"));

        var ex = Assert.Throws<ConnectorException>(() => services.BuildServiceProvider().GetService<GoodDbContext>());
        Assert.Contains("foobar", ex.Message);
    }

    [Fact]
    public void AddDbContext_MultipleMySqlServices_ThrowsConnectorException()
    {
        IServiceCollection services = new ServiceCollection();

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", MySqlTestHelpers.TwoServerVcap);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        services.AddDbContext<GoodDbContext>(options => options.UseMySql(configurationRoot));

        var ex = Assert.Throws<ConnectorException>(() => services.BuildServiceProvider().GetService<GoodDbContext>());
        Assert.Contains("Multiple", ex.Message);
    }

    [Theory]
    [InlineData(new[]
    {
        "Pomelo.EntityFrameworkCore.MySql"
    }, typeof(PomeloMySqlConnection))]
    [InlineData(new[]
    {
        "MySql.EntityFrameworkCore",
        "MySql.Data.EntityFrameworkCore"
    }, typeof(OfficialMySqlConnection))]
    public void AddDbContext_MultipleMySqlServices_AddWithName_Adds(string[] efAssemblies, Type mySqlConnectionType)
    {
        using var scope = new AlternateTypeLocatorScope(efAssemblies, mySqlConnectionType);
        IServiceCollection services = new ServiceCollection();

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", MySqlTestHelpers.TwoServerVcap);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        AddMySqlDbContext(services, configurationRoot, "spring-cloud-broker-db2");

        ServiceProvider built = services.BuildServiceProvider();
        var service = built.GetService<GoodDbContext>();
        Assert.NotNull(service);

        DbConnection con = service.Database.GetDbConnection();
        Assert.NotNull(con);
        Assert.True(con.GetType() == mySqlConnectionType);

        string connString = con.ConnectionString;
        Assert.NotNull(connString);
        Assert.Contains("Server=192.168.0.91", connString, StringComparison.InvariantCultureIgnoreCase);
        Assert.Contains("Port=3306", connString, StringComparison.InvariantCultureIgnoreCase);
        Assert.Contains("Database=cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd0407903550", connString, StringComparison.InvariantCultureIgnoreCase);
        Assert.Contains("User Id=Dd6O1BPXUHdrmzbP0", connString, StringComparison.InvariantCultureIgnoreCase);
        Assert.Contains("Password=7E1LxXnlH2hhlPVt0", connString, StringComparison.InvariantCultureIgnoreCase);
    }

    [Theory]
    [InlineData(new[]
    {
        "Pomelo.EntityFrameworkCore.MySql"
    }, typeof(PomeloMySqlConnection))]
    [InlineData(new[]
    {
        "MySql.EntityFrameworkCore",
        "MySql.Data.EntityFrameworkCore"
    }, typeof(OfficialMySqlConnection))]
    public void AddDbContexts_WithVCAPs_AddsDbContexts(string[] efAssemblies, Type mySqlConnectionType)
    {
        using var scope = new AlternateTypeLocatorScope(efAssemblies, mySqlConnectionType);
        IServiceCollection services = new ServiceCollection();
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", MySqlTestHelpers.SingleServerVcap);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        AddMySqlDbContext(services, configurationRoot);

        ServiceProvider built = services.BuildServiceProvider();
        var service = built.GetService<GoodDbContext>();
        Assert.NotNull(service);

        DbConnection con = service.Database.GetDbConnection();
        Assert.NotNull(con);
        Assert.True(con.GetType() == mySqlConnectionType);

        string connString = con.ConnectionString;
        Assert.NotNull(connString);
        Assert.Contains("Server=192.168.0.90", connString, StringComparison.InvariantCultureIgnoreCase);
        Assert.Contains("Port=3306", connString, StringComparison.InvariantCultureIgnoreCase);
        Assert.Contains("Database=cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355", connString, StringComparison.InvariantCultureIgnoreCase);
        Assert.Contains("User Id=Dd6O1BPXUHdrmzbP", connString, StringComparison.InvariantCultureIgnoreCase);
        Assert.Contains("Password=7E1LxXnlH2hhlPVt", connString, StringComparison.InvariantCultureIgnoreCase);
    }

    // Run a MySQL server with Docker to match credentials below with this command
    // docker run --name steeltoe-mysql -p 3306:3306 -e MYSQL_DATABASE=steeltoe -e MYSQL_ROOT_PASSWORD=steeltoe mysql
    [Theory(Skip = "Requires a running MySQL server to support AutoDetect")]
    [InlineData(new[]
    {
        "Pomelo.EntityFrameworkCore.MySql"
    }, typeof(PomeloMySqlConnection))]
    [InlineData(new[]
    {
        "MySql.EntityFrameworkCore",
        "MySql.Data.EntityFrameworkCore"
    }, typeof(OfficialMySqlConnection))]
    public void AddDbContext_NoVCAPs_AddsDbContext_WithMySqlConnection_AutodetectOn5_0(string[] efAssemblies, Type mySqlConnectionType)
    {
        using var scope = new AlternateTypeLocatorScope(efAssemblies, mySqlConnectionType);
        IServiceCollection services = new ServiceCollection();

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            { "mysql:client:database", "steeltoe2" },
            { "mysql:client:username", "root" },
            { "mysql:client:password", "steeltoe" }
        }).Build();

        services.AddDbContext<GoodDbContext>(options => options.UseMySql(configurationRoot));

        var service = services.BuildServiceProvider().GetService<GoodDbContext>();
        Assert.NotNull(service);
        DbConnection con = service.Database.GetDbConnection();
        Assert.NotNull(con);
        Assert.True(con.GetType() == mySqlConnectionType);
    }

    private static void AddMySqlDbContext(IServiceCollection services, IConfigurationRoot configuration, string serviceName = null)
    {
        if (serviceName == null)
        {
            services.AddDbContext<GoodDbContext>(options => options.UseMySql(configuration, serverVersion: MySqlServerVersion.LatestSupportedServerVersion));
        }
        else
        {
            services.AddDbContext<GoodDbContext>(options =>
                options.UseMySql(configuration, serviceName, serverVersion: MySqlServerVersion.LatestSupportedServerVersion));
        }
    }

    private sealed class AlternateTypeLocatorScope : IDisposable
    {
        private readonly string[] _mySqlEntityAssembliesBackup;
        private readonly string[] _mySqlAssembliesBackup;

        public AlternateTypeLocatorScope(string[] mySqlEntityAssemblies, Type mySqlConnectionType)
        {
            _mySqlEntityAssembliesBackup = EntityFrameworkCoreTypeLocator.MySqlEntityAssemblies;
            EntityFrameworkCoreTypeLocator.MySqlEntityAssemblies = mySqlEntityAssemblies;

            _mySqlAssembliesBackup = MySqlTypeLocator.Assemblies;

            MySqlTypeLocator.Assemblies = new[]
            {
                mySqlConnectionType.Assembly.FullName?.Split(',')[0]
            };
        }

        public void Dispose()
        {
            EntityFrameworkCoreTypeLocator.MySqlEntityAssemblies = _mySqlEntityAssembliesBackup;
            MySqlTypeLocator.Assemblies = _mySqlAssembliesBackup;
        }
    }
}
