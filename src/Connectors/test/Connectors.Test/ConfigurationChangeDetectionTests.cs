// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Steeltoe.Connectors.PostgreSql;
using Xunit;

namespace Steeltoe.Connectors.Test;

public sealed class ConfigurationChangeDetectionTests
{
    [Fact]
    public async Task Applies_local_configuration_changes_using_WebApplicationBuilder()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        string appSettingsJson = @"{
  ""Steeltoe"": {
    ""Client"": {
      ""PostgreSql"": {
        ""examplePostgreSqlService"": {
            ""ConnectionString"": ""SERVER=one.com;DB=first""
        }
      }
    }
  }
}
";

        var fileInfo = new MemoryFileInfo("appsettings.json", Encoding.UTF8.GetBytes(appSettingsJson));
        var fileProvider = new MemoryFileProvider(fileInfo);

        builder.Configuration.AddJsonFile(fileProvider, fileInfo.Name, false, true);
        builder.AddPostgreSql(configureOptions => configureOptions.DetectConfigurationChanges = true, null);

        await using WebApplication app = builder.Build();

        var connectorFactory = app.Services.GetRequiredService<ConnectorFactory<PostgreSqlOptions, NpgsqlConnection>>();

        connectorFactory.ServiceBindingNames.Should().HaveCount(1);
        connectorFactory.ServiceBindingNames.Should().Contain("examplePostgreSqlService");

        string? connectionString = connectorFactory.Get("examplePostgreSqlService").Options.ConnectionString;
        connectionString.Should().Be("Host=one.com;Database=first");

        appSettingsJson = @"{
  ""Steeltoe"": {
    ""Client"": {
      ""PostgreSql"": {
        ""examplePostgreSqlService"": {
            ""ConnectionString"": ""SERVER=two.com;DB=second""
        }
      }
    }
  }
}
";

        fileInfo.ReplaceContents(Encoding.UTF8.GetBytes(appSettingsJson));
        fileProvider.NotifyChanged();

        connectionString = connectorFactory.Get("examplePostgreSqlService").Options.ConnectionString;
        connectionString.Should().Be("Host=two.com;Database=second");

        appSettingsJson = @"{
  ""Steeltoe"": {
    ""Client"": {
      ""PostgreSql"": {
        ""examplePostgreSqlService"": {
            ""ConnectionString"": ""SERVER=three.com;DB=third""
        }
      }
    }
  }
}
";

        fileInfo.ReplaceContents(Encoding.UTF8.GetBytes(appSettingsJson));
        fileProvider.NotifyChanged();

        connectionString = connectorFactory.Get("examplePostgreSqlService").Options.ConnectionString;
        connectionString.Should().Be("Host=three.com;Database=third");
    }

    [Fact]
    public void Applies_local_configuration_changes_using_WebHostBuilder()
    {
        var builder = new WebHostBuilder();

        builder.Configure(_ =>
        {
        });

        string appSettingsJson = @"{
  ""Steeltoe"": {
    ""Client"": {
      ""PostgreSql"": {
        ""examplePostgreSqlService"": {
            ""ConnectionString"": ""SERVER=one.com;DB=first""
        }
      }
    }
  }
}
";

        var fileInfo = new MemoryFileInfo("appsettings.json", Encoding.UTF8.GetBytes(appSettingsJson));
        var fileProvider = new MemoryFileProvider(fileInfo);

        builder.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.AddJsonFile(fileProvider, fileInfo.Name, false, true);
            configurationBuilder.ConfigurePostgreSql(options => options.DetectConfigurationChanges = true);
        });

        builder.ConfigureServices((context, services) => services.AddPostgreSql(context.Configuration));
        using IWebHost app = builder.Build();

        var connectorFactory = app.Services.GetRequiredService<ConnectorFactory<PostgreSqlOptions, NpgsqlConnection>>();

        connectorFactory.ServiceBindingNames.Should().HaveCount(1);
        connectorFactory.ServiceBindingNames.Should().Contain("examplePostgreSqlService");

        string? connectionString = connectorFactory.Get("examplePostgreSqlService").Options.ConnectionString;
        connectionString.Should().Be("Host=one.com;Database=first");

        appSettingsJson = @"{
  ""Steeltoe"": {
    ""Client"": {
      ""PostgreSql"": {
        ""examplePostgreSqlService"": {
            ""ConnectionString"": ""SERVER=two.com;DB=second""
        }
      }
    }
  }
}
";

        fileInfo.ReplaceContents(Encoding.UTF8.GetBytes(appSettingsJson));
        fileProvider.NotifyChanged();

        connectionString = connectorFactory.Get("examplePostgreSqlService").Options.ConnectionString;
        connectionString.Should().Be("Host=two.com;Database=second");

        appSettingsJson = @"{
  ""Steeltoe"": {
    ""Client"": {
      ""PostgreSql"": {
        ""examplePostgreSqlService"": {
            ""ConnectionString"": ""SERVER=three.com;DB=third""
        }
      }
    }
  }
}
";

        fileInfo.ReplaceContents(Encoding.UTF8.GetBytes(appSettingsJson));
        fileProvider.NotifyChanged();

        connectionString = connectorFactory.Get("examplePostgreSqlService").Options.ConnectionString;
        connectionString.Should().Be("Host=three.com;Database=third");
    }

    [Fact]
    public void Applies_local_configuration_changes_using_HostBuilder()
    {
        var builder = new HostBuilder();

        string appSettingsJson = @"{
  ""Steeltoe"": {
    ""Client"": {
      ""PostgreSql"": {
        ""examplePostgreSqlService"": {
            ""ConnectionString"": ""SERVER=one.com;DB=first""
        }
      }
    }
  }
}
";

        var fileInfo = new MemoryFileInfo("appsettings.json", Encoding.UTF8.GetBytes(appSettingsJson));
        var fileProvider = new MemoryFileProvider(fileInfo);

        builder.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.AddJsonFile(fileProvider, fileInfo.Name, false, true);
            configurationBuilder.ConfigurePostgreSql(options => options.DetectConfigurationChanges = true);
        });

        builder.ConfigureServices((context, services) => services.AddPostgreSql(context.Configuration));
        using IHost app = builder.Build();

        var connectorFactory = app.Services.GetRequiredService<ConnectorFactory<PostgreSqlOptions, NpgsqlConnection>>();

        connectorFactory.ServiceBindingNames.Should().HaveCount(1);
        connectorFactory.ServiceBindingNames.Should().Contain("examplePostgreSqlService");

        string? connectionString = connectorFactory.Get("examplePostgreSqlService").Options.ConnectionString;
        connectionString.Should().Be("Host=one.com;Database=first");

        appSettingsJson = @"{
  ""Steeltoe"": {
    ""Client"": {
      ""PostgreSql"": {
        ""examplePostgreSqlService"": {
            ""ConnectionString"": ""SERVER=two.com;DB=second""
        }
      }
    }
  }
}
";

        fileInfo.ReplaceContents(Encoding.UTF8.GetBytes(appSettingsJson));
        fileProvider.NotifyChanged();

        connectionString = connectorFactory.Get("examplePostgreSqlService").Options.ConnectionString;
        connectionString.Should().Be("Host=two.com;Database=second");

        appSettingsJson = @"{
  ""Steeltoe"": {
    ""Client"": {
      ""PostgreSql"": {
        ""examplePostgreSqlService"": {
            ""ConnectionString"": ""SERVER=three.com;DB=third""
        }
      }
    }
  }
}
";

        fileInfo.ReplaceContents(Encoding.UTF8.GetBytes(appSettingsJson));
        fileProvider.NotifyChanged();

        connectionString = connectorFactory.Get("examplePostgreSqlService").Options.ConnectionString;
        connectionString.Should().Be("Host=three.com;Database=third");
    }
}
