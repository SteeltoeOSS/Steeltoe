// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using NSubstitute;
using StackExchange.Redis;
using Steeltoe.Common.TestResources;
using Steeltoe.Connectors.Redis;

namespace Steeltoe.Security.DataProtection.Redis.Test;

public sealed class RedisDataProtectionBuilderExtensionsTest
{
    [Fact]
    public async Task Stores_session_state_in_Redis()
    {
        const string appName = "SHARED-APP-NAME";

        var appSettings = new Dictionary<string, string?>
        {
            ["Steeltoe:Client:Redis:Default:ConnectionString"] = $"localhost,keepAlive=30,name={appName}"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault();
        builder.Configuration.AddInMemoryCollection(appSettings);

        builder.AddRedis(null, addOptions =>
        {
            addOptions.CreateConnection = (serviceProvider, serviceBindingName) =>
            {
                var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<RedisOptions>>();
                RedisOptions options = optionsMonitor.Get(serviceBindingName);

                return GetMockedConnectionMultiplexer(options.ConnectionString);
            };
        });

        builder.Services.AddSession(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });

        builder.Services.AddDataProtection().PersistKeysToRedis().SetApplicationName(appName);

        await using WebApplication app = builder.Build();

        app.UseSession();

        app.MapGet("/set-session", httpContext =>
        {
            httpContext.Session.Set("example-key", [.. "example-value"u8]);
            httpContext.Response.StatusCode = 200;
            return Task.CompletedTask;
        });

        app.MapGet("/get-session", httpContext =>
        {
            if (httpContext.Session.TryGetValue("example-key", out byte[]? bytes))
            {
                string sessionValue = Encoding.UTF8.GetString(bytes);
                httpContext.Response.StatusCode = (int)HttpStatusCode.OK;
                httpContext.Response.WriteAsync(sessionValue, httpContext.RequestAborted);
            }
            else
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }

            return Task.CompletedTask;
        });

        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient httpClient = app.GetTestClient();
        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/set-session"), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string setCookieHeaderText = response.Headers.Single(header => header.Key == "Set-Cookie").Value.Single();
        SetCookieHeaderValue setCookieHeaderValue = SetCookieHeaderValue.Parse(setCookieHeaderText);
        var cookie = new Cookie(setCookieHeaderValue.Name.Value!, setCookieHeaderValue.Value.Value);

        var request = new HttpRequestMessage(HttpMethod.Get, new Uri("http://localhost/get-session"));
        request.Headers.Add("Cookie", cookie.ToString());
        response = await httpClient.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseContent.Should().Be("example-value");
    }

    private static object GetMockedConnectionMultiplexer(string? connectionString)
    {
        Dictionary<string, byte[]> innerStore = [];

        var database = Substitute.For<IDatabase>();
        database.HashGet(Arg.Any<RedisKey>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>()).Returns(info => GetRedisValues(info.Arg<RedisKey>()));

        database.HashGetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>())
            .Returns(info => Task.FromResult(GetRedisValues(info.Arg<RedisKey>())));

        database.HashSetAsync(Arg.Any<RedisKey>(), Arg.Any<HashEntry[]>(), Arg.Any<CommandFlags>()).Returns(info =>
        {
            var key = info.Arg<RedisKey>();
            HashEntry[] hashFields = info.Arg<HashEntry[]>();

            byte[] data = hashFields[2].Value!;
            innerStore[key!] = data;
            return Task.CompletedTask;
        });

        var connectionMultiplexer = Substitute.For<IConnectionMultiplexer>();
        connectionMultiplexer.Configuration.Returns(connectionString);
        connectionMultiplexer.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(database);

        database.Multiplexer.Returns(connectionMultiplexer);

        return connectionMultiplexer;

        RedisValue[] GetRedisValues(RedisKey key)
        {
            return innerStore.TryGetValue(key!, out byte[]? data)
                ?
                [
                    default,
                    default,
                    data
                ]
                :
                [
                    default,
                    default,
                    default
                ];
        }
    }
}
