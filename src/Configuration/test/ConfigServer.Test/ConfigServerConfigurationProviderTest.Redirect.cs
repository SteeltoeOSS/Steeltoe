// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Configuration.ConfigServer.Test;

public sealed partial class ConfigServerConfigurationProviderTest
{
    [Fact]
    public async Task RemoteLoadAsync_DoesNotFollowRedirect_WhenConfigServerEndpointRedirects()
    {
        using var loggerProvider = new CapturingLoggerProvider((_, level) => level == LogLevel.Warning);
        using var loggerFactory = new LoggerFactory([loggerProvider]);
        bool redirectRouteAccessed = false;

        WebApplicationBuilder serverBuilder = WebApplication.CreateBuilder();
        serverBuilder.Logging.ClearProviders();
        await using WebApplication server = serverBuilder.Build();
        server.Urls.Add("http://127.0.0.1:0");

        server.MapGet("/myName/Staging",
            (HttpContext httpContext) => httpContext.Response.Redirect($"http://127.0.0.1:{httpContext.Connection.LocalPort}/redirect-target", true));

        server.MapGet("/redirect-target", () =>
        {
            redirectRouteAccessed = true;

            return Results.Json(new ConfigEnvironment
            {
                Name = "redirected"
            });
        });

        await server.StartAsync(TestContext.Current.CancellationToken);
        int port = server.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Select(a => new Uri(a).Port).First();

        var options = new ConfigServerClientOptions
        {
            Name = "myName",
            Environment = "Staging",
            Uri = $"http://127.0.0.1:{port}",
            Token = "vault-secret"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, null, loggerFactory);

        ConfigEnvironment? result = await provider.RemoteLoadAsync(provider.ClientOptions, options.GetUris(), null, TestContext.Current.CancellationToken);

        result.Should().BeNull();
        redirectRouteAccessed.Should().BeFalse();

        IList<string> logMessages = loggerProvider.GetAll();
        logMessages.Should().ContainSingle().Which.Should().Contain("Redirects are not followed to prevent credential leaks.");
    }

    [Fact]
    public async Task RemoteLoadAsync_DoesNotFollowRedirect_WhenAccessTokenEndpointRedirects()
    {
        using var loggerProvider = new CapturingLoggerProvider((_, level) => level == LogLevel.Warning);
        using var loggerFactory = new LoggerFactory([loggerProvider]);

        bool redirectRouteAccessed = false;

        WebApplicationBuilder serverBuilder = WebApplication.CreateBuilder();
        serverBuilder.Logging.ClearProviders();
        await using WebApplication server = serverBuilder.Build();
        server.Urls.Add("http://127.0.0.1:0");

        server.MapPost("/token",
            (HttpContext httpContext) => httpContext.Response.Redirect($"http://127.0.0.1:{httpContext.Connection.LocalPort}/token-redirect", true));

        server.MapGet("/token-redirect", () =>
        {
            redirectRouteAccessed = true;
            return Results.Ok();
        });

        await server.StartAsync(TestContext.Current.CancellationToken);
        int port = server.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Select(a => new Uri(a).Port).First();

        var options = new ConfigServerClientOptions
        {
            Name = "myName",
            Environment = "Staging",
            Token = "vault-secret",
            AccessTokenUri = $"http://127.0.0.1:{port}/token",
            ClientId = "some-client",
            ClientSecret = "some-secret"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, null, loggerFactory);

        ConfigEnvironment? result = await provider.RemoteLoadAsync(provider.ClientOptions, options.GetUris(), null, TestContext.Current.CancellationToken);

        result.Should().BeNull();
        redirectRouteAccessed.Should().BeFalse();

        IList<string> logMessages = loggerProvider.GetAll();
        logMessages.Should().ContainSingle().Which.Should().Contain("Failed to fetch access token from");
    }

    [Fact]
    public async Task RefreshVaultTokenAsync_DoesNotFollowRedirect_WhenVaultRenewEndpointRedirects()
    {
        using var loggerProvider = new CapturingLoggerProvider((_, level) => level == LogLevel.Warning);
        using var loggerFactory = new LoggerFactory([loggerProvider]);

        bool redirectRouteAccessed = false;

        WebApplicationBuilder serverBuilder = WebApplication.CreateBuilder();
        serverBuilder.Logging.ClearProviders();
        await using WebApplication server = serverBuilder.Build();
        server.Urls.Add("http://127.0.0.1:0");

        server.MapPost("/vault/v1/auth/token/renew-self",
            (HttpContext httpContext) => httpContext.Response.Redirect($"http://127.0.0.1:{httpContext.Connection.LocalPort}/vault-redirect", true));

        server.MapGet("/vault-redirect", () =>
        {
            redirectRouteAccessed = true;
            return Results.Ok();
        });

        await server.StartAsync(TestContext.Current.CancellationToken);
        int port = server.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Select(a => new Uri(a).Port).First();

        var options = new ConfigServerClientOptions
        {
            Name = "myName",
            Environment = "Staging",
            Uri = $"http://127.0.0.1:{port}",
            Token = "MyVaultToken"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, null, loggerFactory);

        await provider.RefreshVaultTokenAsync(provider.ClientOptions, TestContext.Current.CancellationToken);

        redirectRouteAccessed.Should().BeFalse();

        IList<string> logMessages = loggerProvider.GetAll();
        logMessages.Should().ContainSingle().Which.Should().Contain("returned status");
    }

    [Fact]
    public async Task RefreshVaultTokenAsync_DoesNotFollowRedirect_WhenAccessTokenEndpointRedirects()
    {
        using var loggerProvider = new CapturingLoggerProvider((_, level) => level >= LogLevel.Warning);
        using var loggerFactory = new LoggerFactory([loggerProvider]);

        bool redirectRouteAccessed = false;

        WebApplicationBuilder serverBuilder = WebApplication.CreateBuilder();
        serverBuilder.Logging.ClearProviders();
        await using WebApplication server = serverBuilder.Build();
        server.Urls.Add("http://127.0.0.1:0");

        server.MapPost("/token",
            (HttpContext httpContext) => httpContext.Response.Redirect($"http://127.0.0.1:{httpContext.Connection.LocalPort}/token-redirect", true));

        server.MapGet("/token-redirect", () =>
        {
            redirectRouteAccessed = true;
            return Results.Ok();
        });

        await server.StartAsync(TestContext.Current.CancellationToken);
        int port = server.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Select(a => new Uri(a).Port).First();

        var options = new ConfigServerClientOptions
        {
            Name = "myName",
            Environment = "Staging",
            Token = "MyVaultToken",
            AccessTokenUri = $"http://127.0.0.1:{port}/token",
            ClientId = "some-client",
            ClientSecret = "some-secret"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, null, loggerFactory);

        await provider.RefreshVaultTokenAsync(provider.ClientOptions, TestContext.Current.CancellationToken);

        redirectRouteAccessed.Should().BeFalse();

        IList<string> logMessages = loggerProvider.GetAll();
        logMessages.Should().ContainSingle().Which.Should().Contain("Unable to renew Vault token");
    }
}
