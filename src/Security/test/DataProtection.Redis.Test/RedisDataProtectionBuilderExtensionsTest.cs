// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Steeltoe.Common.TestResources;
using Steeltoe.Connectors.Redis;

namespace Steeltoe.Security.DataProtection.Redis.Test;

public sealed partial class RedisDataProtectionBuilderExtensionsTest
{
    [Fact]
    [Trait("Category", "SkipOnMacOS")]
    public async Task Stores_session_state_in_Redis()
    {
        const string appName = "SHARED-APP-NAME";

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault(false);

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Steeltoe:Client:Redis:Default:ConnectionString"] = $"localhost,keepAlive=30,name={appName}"
        });

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
            httpContext.Session.Set("example-key", "example-value"u8.ToArray());
            httpContext.Response.StatusCode = 200;
            return Task.CompletedTask;
        });

        app.MapGet("/get-session", httpContext =>
        {
            if (httpContext.Session.TryGetValue("example-key", out byte[]? bytes))
            {
                string sessionValue = Encoding.UTF8.GetString(bytes);
                httpContext.Response.StatusCode = (int)HttpStatusCode.OK;
                httpContext.Response.WriteAsync(sessionValue);
            }
            else
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }

            return Task.CompletedTask;
        });

        await app.StartAsync();

        using var httpClient = new HttpClient();
        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost:5000/set-session"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string setCookieHeaderText = response.Headers.Single(header => header.Key == "Set-Cookie").Value.Single();
        SetCookieHeaderValue setCookieHeaderValue = SetCookieHeaderValue.Parse(setCookieHeaderText);
        var cookie = new Cookie(setCookieHeaderValue.Name.Value!, setCookieHeaderValue.Value.Value);

        var request = new HttpRequestMessage(HttpMethod.Get, new Uri("http://localhost:5000/get-session"));
        request.Headers.Add("Cookie", cookie.ToString());
        response = await httpClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Be("example-value");
    }
}
