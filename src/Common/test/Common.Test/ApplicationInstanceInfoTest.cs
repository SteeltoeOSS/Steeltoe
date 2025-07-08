// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Extensions;
using Steeltoe.Common.TestResources;
using Steeltoe.Common.TestResources.IO;

namespace Steeltoe.Common.Test;

public sealed class ApplicationInstanceInfoTest
{
    [Fact]
    public void ConstructorSetsDefaults()
    {
        var options = new ApplicationInstanceInfo();

        options.ApplicationName.Should().BeNull();
    }

    [Fact]
    public async Task ReadsApplicationConfiguration()
    {
        const string configJson = """
            {
              "Spring": {
                "Application": {
                  "Name": "my-app"
                }
              }
            }
            """;

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile(MemoryFileProvider.DefaultAppSettingsFileName, configJson);
        string directory = Path.GetDirectoryName(path)!;
        string fileName = Path.GetFileName(path);
        var builder = new ConfigurationBuilder();
        builder.SetBasePath(directory);
        builder.AddJsonFile(fileName);
        IConfiguration configuration = builder.Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddApplicationInstanceInfo();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var options = serviceProvider.GetRequiredService<IApplicationInstanceInfo>();

        options.ApplicationName.Should().Be("my-app");
    }

    [Fact]
    public void Resolves_application_name_from_configuration()
    {
        var appSettings = new Dictionary<string, string?>();

        IApplicationInstanceInfo info = FromConfiguration(appSettings);
        info.ApplicationName.Should().Be(Assembly.GetEntryAssembly()!.GetName().Name);

        appSettings.Add("spring:application:name", "SpringAppName");
        info = FromConfiguration(appSettings);
        info.ApplicationName.Should().Be("SpringAppName");
    }

    private IApplicationInstanceInfo FromConfiguration(IDictionary<string, string?> appSettings)
    {
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appSettings);
        IConfiguration configuration = builder.Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddApplicationInstanceInfo();
        using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        return serviceProvider.GetRequiredService<IApplicationInstanceInfo>();
    }
}
