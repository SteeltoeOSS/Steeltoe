// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Extensions.Configuration.ConfigServer.Test;

public partial class ConfigServerConfigurationProviderTest
{
    [Fact]
    public async Task RemoteLoadAsync_DoesNotFollowRedirect_WhenConfigServerEndpointRedirects()
    {
        var logMessages = new List<string>();
        using var loggerFactory =
            LoggerFactory.Create(builder => builder.AddProvider(new CapturingLoggerProvider(logMessages)));
        var redirectRouteAccessed = false;

        var serverBuilder = WebApplication.CreateBuilder();
        serverBuilder.Logging.ClearProviders();
        await using var server = serverBuilder.Build();
        server.Urls.Add("http://127.0.0.1:0");

        server.MapGet("/myName/Production", (HttpContext httpContext) =>
            httpContext.Response.Redirect(
                $"http://127.0.0.1:{httpContext.Connection.LocalPort}/redirect-target",
                permanent: true));

        server.MapGet("/redirect-target", () =>
        {
            redirectRouteAccessed = true;
            return Results.Json(new ConfigEnvironment { Name = "redirected" });
        });

        await server.StartAsync();
        var addressFeature = server.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
        var port = addressFeature.Addresses.Select(address => new Uri(address).Port).First();

        var settings = new ConfigServerClientSettings
        {
            Name = "myName",
            Environment = "Production",
            Uri = $"http://127.0.0.1:{port}",
            Token = "vault-secret"
        };

        using var provider = new ConfigServerConfigurationProvider(settings, loggerFactory);
        var result = await provider.RemoteLoadAsync(settings.GetRawUris(), null);

        Assert.Null(result);
        Assert.False(redirectRouteAccessed);
        Assert.Equal(
            $"Warning: Config Server returned a 301 redirect to 'http://127.0.0.1:{port}/redirect-target'. " +
            "Redirects are not followed to prevent credential leaks. Update 'spring:cloud:config:uri' to point directly to the target.",
            Assert.Single(logMessages));
    }

    [Fact]
#pragma warning disable CS0618 // Obsolete RemoteLoadAsync(string) overload
    public async Task ObsoleteRemoteLoadAsync_DoesNotFollowRedirect_WhenConfigServerEndpointRedirects()
#pragma warning restore CS0618
    {
        var logMessages = new List<string>();
        using var loggerFactory =
            LoggerFactory.Create(builder => builder.AddProvider(new CapturingLoggerProvider(logMessages)));
        var redirectRouteAccessed = false;

        var serverBuilder = WebApplication.CreateBuilder();
        serverBuilder.Logging.ClearProviders();
        await using var server = serverBuilder.Build();
        server.Urls.Add("http://127.0.0.1:0");

        server.MapGet("/myName/Production", (HttpContext httpContext) =>
            httpContext.Response.Redirect(
                $"http://127.0.0.1:{httpContext.Connection.LocalPort}/redirect-target",
                permanent: true));

        server.MapGet("/redirect-target", () =>
        {
            redirectRouteAccessed = true;
            return Results.Json(new ConfigEnvironment { Name = "redirected" });
        });

        await server.StartAsync();
        var addressFeature = server.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
        var port = addressFeature.Addresses.Select(address => new Uri(address).Port).First();

        var settings = new ConfigServerClientSettings
        {
            Name = "myName",
            Environment = "Production",
            Uri = $"http://127.0.0.1:{port}",
            Token = "vault-secret"
        };

        using var provider = new ConfigServerConfigurationProvider(settings, loggerFactory);

#pragma warning disable CS0618
        var result = await provider.RemoteLoadAsync($"http://127.0.0.1:{port}/myName/Production");
#pragma warning restore CS0618

        Assert.Null(result);
        Assert.False(redirectRouteAccessed);
        Assert.Equal(
            $"Warning: Config Server returned a 301 redirect to 'http://127.0.0.1:{port}/redirect-target'. " +
            "Redirects are not followed to prevent credential leaks. Update 'spring:cloud:config:uri' to point directly to the target.",
            Assert.Single(logMessages));
    }

    [Fact]
    public async Task RemoteLoadAsync_DoesNotFollowRedirect_WhenAccessTokenEndpointRedirects()
    {
        var logMessages = new List<string>();
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(new CapturingLoggerProvider(logMessages)));
        var redirectRouteAccessed = false;

        var serverBuilder = WebApplication.CreateBuilder();
        serverBuilder.Logging.ClearProviders();
        await using var server = serverBuilder.Build();
        server.Urls.Add("http://127.0.0.1:0");

        server.MapPost("/token", (HttpContext httpContext) =>
            httpContext.Response.Redirect($"http://127.0.0.1:{httpContext.Connection.LocalPort}/token-redirect", true));

        server.MapGet("/token-redirect", () =>
        {
            redirectRouteAccessed = true;
            return Results.Ok();
        });

        await server.StartAsync();
        var addressFeature = server.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
        var port = addressFeature.Addresses.Select(address => new Uri(address).Port).First();

        var settings = new ConfigServerClientSettings
        {
            Name = "myName",
            Environment = "Production",
            Uri = $"http://127.0.0.1:{port}",
            Token = "vault-secret",
            AccessTokenUri = $"http://127.0.0.1:{port}/token",
            ClientId = "some-client",
            ClientSecret = "some-secret"
        };

        using var provider = new ConfigServerConfigurationProvider(settings, loggerFactory);

        var result = await provider.RemoteLoadAsync(settings.GetRawUris(), null);

        Assert.Null(result);
        Assert.False(redirectRouteAccessed);
        Assert.Equal(
            $"Warning: Failed to fetch access token from 'http://127.0.0.1:{port}/token'.",
            Assert.Single(logMessages));
    }

    [Fact]
    public async Task RefreshVaultTokenAsync_DoesNotFollowRedirect_WhenVaultRenewEndpointRedirects()
    {
        var logMessages = new List<string>();
        using var loggerFactory =
            LoggerFactory.Create(builder => builder.AddProvider(new CapturingLoggerProvider(logMessages)));

        var redirectRouteAccessed = false;

        var serverBuilder = WebApplication.CreateBuilder();
        serverBuilder.Logging.ClearProviders();
        await using var server = serverBuilder.Build();
        server.Urls.Add("http://127.0.0.1:0");

        server.MapPost("/vault/v1/auth/token/renew-self", (HttpContext httpContext) =>
            httpContext.Response.Redirect($"http://127.0.0.1:{httpContext.Connection.LocalPort}/vault-redirect", true));

        server.MapGet("/vault-redirect", () =>
        {
            redirectRouteAccessed = true;
            return Results.Ok();
        });

        await server.StartAsync();
        var addressFeature = server.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
        var port = addressFeature.Addresses.Select(address => new Uri(address).Port).First();

        var settings = new ConfigServerClientSettings
        {
            Name = "myName",
            Environment = "Staging",
            Uri = $"http://127.0.0.1:{port}",
            Token = "MyVaultToken"
        };

        using var provider = new ConfigServerConfigurationProvider(settings, loggerFactory);

        provider.RefreshVaultTokenAsync(null);
        await Task.Delay(TimeSpan.FromSeconds(1));

        Assert.False(redirectRouteAccessed);
        Assert.Equal(
            $"Warning: Renewing Vault token MyVa[*]oken returned a 301 redirect to 'http://127.0.0.1:{port}/vault-redirect'. " +
            "Redirects are not followed to prevent credential leaks. Update 'spring:cloud:config:uri' to point directly to the target.",
            Assert.Single(logMessages));
    }

    [Fact]
    public async Task RefreshVaultTokenAsync_DoesNotFollowRedirect_WhenAccessTokenEndpointRedirects()
    {
        var logMessages = new List<string>();
        using var loggerFactory =
            LoggerFactory.Create(builder => builder.AddProvider(new CapturingLoggerProvider(logMessages)));

        var redirectRouteAccessed = false;
        var vaultRenewCalled = new TaskCompletionSource<bool>();

        var serverBuilder = WebApplication.CreateBuilder();
        serverBuilder.Logging.ClearProviders();
        await using var server = serverBuilder.Build();
        server.Urls.Add("http://127.0.0.1:0");

        server.MapPost("/token", (HttpContext httpContext) =>
            httpContext.Response.Redirect($"http://127.0.0.1:{httpContext.Connection.LocalPort}/token-redirect", true));

        server.MapGet("/token-redirect", () =>
        {
            redirectRouteAccessed = true;
            return Results.Ok();
        });

        server.MapPost("/vault/v1/auth/token/renew-self", async httpContext =>
        {
            httpContext.Response.StatusCode = 500;
            await httpContext.Response.CompleteAsync();
            vaultRenewCalled.TrySetResult(true);
        });

        await server.StartAsync();
        var addressFeature = server.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
        var port = addressFeature.Addresses.Select(address => new Uri(address).Port).First();

        var settings = new ConfigServerClientSettings
        {
            Name = "myName",
            Environment = "Staging",
            Uri = $"http://127.0.0.1:{port}",
            Token = "MyVaultToken",
            AccessTokenUri = $"http://127.0.0.1:{port}/token",
            ClientId = "some-client",
            ClientSecret = "some-secret"
        };

        using var provider = new ConfigServerConfigurationProvider(settings, loggerFactory);

        provider.RefreshVaultTokenAsync(null);
        await vaultRenewCalled.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(TimeSpan.FromMilliseconds(200));

        Assert.False(redirectRouteAccessed);
        Assert.Equal(2, logMessages.Count);
        Assert.Contains($"Warning: Failed to fetch access token from 'http://127.0.0.1:{port}/token'.", logMessages);
        Assert.Contains("Warning: Renewing Vault token MyVa[*]oken returned status: InternalServerError", logMessages);
    }
}