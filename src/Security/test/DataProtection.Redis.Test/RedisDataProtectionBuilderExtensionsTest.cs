// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Net.Http.Headers;
using StackExchange.Redis;
using Steeltoe.Common.TestResources;
using Steeltoe.Connectors.Redis;

namespace Steeltoe.Security.DataProtection.Redis.Test;

public sealed partial class RedisDataProtectionBuilderExtensionsTest
{
    private static readonly bool UseSteeltoe = bool.Parse(bool.FalseString);
    private static readonly XmlEncryptionMode EncryptionMode = Enum.Parse<XmlEncryptionMode>(XmlEncryptionMode.Certificate.ToString());

    // How to distribute Data Protection keys with an ASP.NET Core web app
    // https://medium.com/swlh/how-to-distribute-data-protection-keys-with-an-asp-net-core-web-app-8b2b5d52851b

    [Fact]
    [Trait("Category", "SkipOnMacOS")]
    public async Task Stores_session_state_in_Redis()
    {
        const string appName = "SHARED-APP-NAME";
        const string connectionString = $"localhost,keepAlive=30,name={appName}";

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Steeltoe:Client:Redis:Default:ConnectionString"] = connectionString
        });

        builder.Logging.SetMinimumLevel(LogLevel.Trace);
        builder.Logging.AddProvider(new XunitLoggerProvider(TestContext.Current.TestOutputHelper!));

        if (UseSteeltoe)
        {
            builder.AddRedis();
        }
        else
        {
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = connectionString;
                options.InstanceName = appName;
            });
        }

        IDataProtectionBuilder dataProtectionBuilder = builder.Services.AddDataProtection().SetApplicationName(appName);

        if (UseSteeltoe)
        {
            dataProtectionBuilder.PersistKeysToRedis();
        }
        else
        {
            // ReSharper disable once MethodHasAsyncOverload
#pragma warning disable S6966 // Awaitable method should be used
            dataProtectionBuilder.PersistKeysToStackExchangeRedis(ConnectionMultiplexer.Connect(connectionString));
#pragma warning restore S6966 // Awaitable method should be used
        }

        if (EncryptionMode == XmlEncryptionMode.None)
        {
            // Legitimate warning is logged: No XML encryptor is configured.
        }
        else
        {
            // No warning is logged, because we're setting up encryption for the list-key, which indicates the set of non-expired keyring keys available.

            dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(@"c:\Temp\DP-KEYS")).AddKeyManagementOptions(options =>
            {
                options.NewKeyLifetime = new TimeSpan(365, 0, 0, 0);
                options.AutoGenerateKeys = true;

                if (EncryptionMode == XmlEncryptionMode.Null)
                {
                    // Produced xml file contains line: <!-- Warning: the key below is in an unencrypted form. -->
                    options.XmlEncryptor = new NullXmlEncryptor(null);
                }
                else if (EncryptionMode == XmlEncryptionMode.Certificate)
                {
                    // No warning. Key in produced xml file is encrypted with the certificate.
                    options.XmlEncryptor = new CertificateXmlEncryptor(
                        X509Certificate2.CreateFromPemFile(@"..\..\..\..\..\..\Common\test\Certificates.Test\instance2.crt",
                            @"..\..\..\..\..\..\Common\test\Certificates.Test\instance2.key"), NullLoggerFactory.Instance);
                }
            });
        }

        builder.Services.AddSession(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });

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

    private enum XmlEncryptionMode
    {
        None,
        Certificate,
        Null
    }
}
