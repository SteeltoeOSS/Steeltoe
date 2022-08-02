// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient.Test;

public class SpringBootAdminAppBuilderExtensionsTest
{
    [Fact]
    [Obsolete("To be removed in the next major version.")]
    public void SpringBootAdminClient_EndToEnd()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["management:endpoints:path"] = "/management",
            ["management:endpoints:health:path"] = "myhealth",
            ["URLS"] = "http://localhost:8080;https://localhost:8082",
            ["spring:boot:admin:client:url"] = "http://springbootadmin:9090",
            ["spring:application:name"] = "MySteeltoeApplication"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        IConfigurationRoot config = configurationBuilder.Build();
        var services = new ServiceCollection();
        var appLifeTime = new MyAppLifeTime();
        services.TryAddSingleton<IHostApplicationLifetime>(appLifeTime);
        services.TryAddSingleton<IConfiguration>(config);
        ServiceProvider provider = services.BuildServiceProvider();
        var appBuilder = new ApplicationBuilder(provider);

        var builder = new WebHostBuilder();
        builder.UseStartup<TestStartup>();

        using var server = new TestServer(builder);
        HttpClient client = server.CreateClient();
        appBuilder.RegisterWithSpringBootAdmin(config, client);

        appLifeTime.AppStartTokenSource.Cancel(); // Trigger application lifetime start

        Assert.NotNull(SpringBootAdminApplicationBuilderExtensions.RegistrationResult);
        Assert.Equal("1234567", SpringBootAdminApplicationBuilderExtensions.RegistrationResult.Id);

        appLifeTime.AppStopTokenSource.Cancel(); // Trigger application lifetime stop
    }

    private sealed class MyAppLifeTime : IHostApplicationLifetime
    {
        public readonly CancellationTokenSource AppStartTokenSource = new();
        public readonly CancellationTokenSource AppStopTokenSource = new();

        public CancellationToken ApplicationStarted => AppStartTokenSource.Token;

        public CancellationToken ApplicationStopped => AppStopTokenSource.Token;

        public CancellationToken ApplicationStopping => throw new NotImplementedException();

        public void StopApplication()
        {
            throw new NotImplementedException();
        }
    }
}
