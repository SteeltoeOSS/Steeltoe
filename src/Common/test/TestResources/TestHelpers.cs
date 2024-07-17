// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Steeltoe.Common.TestResources;

public static class TestHelpers
{
    public const string VcapApplication = """
        {
            "limits": {
                "fds": 16384,
                "mem": 1024,
                "disk": 1024
            },
            "application_name": "spring-cloud-broker",
            "application_uris": [
                "spring-cloud-broker.apps.testcloud.com"
            ],
            "name": "spring-cloud-broker",
            "space_name": "p-spring-cloud-services",
            "space_id": "65b73473-94cc-4640-b462-7ad52838b4ae",
            "uris": [
                "spring-cloud-broker.apps.testcloud.com"
            ],
            "users": null,
            "version": "07e112f7-2f71-4f5a-8a34-db51dbed30a3",
            "application_version": "07e112f7-2f71-4f5a-8a34-db51dbed30a3",
            "application_id": "798c2495-fe75-49b1-88da-b81197f2bf06"
        }
        """;

    public static readonly ImmutableDictionary<string, string?> FastTestsConfiguration = new Dictionary<string, string?>
    {
        { "spring:cloud:config:enabled", "false" },
        { "eureka:client:serviceUrl", "http://127.0.0.1" },
        { "eureka:client:enabled", "false" },
        { "consul:discovery:enabled", "false" },
        { "management:endpoints:actuator:exposure:include:0", "*" },
        { "management:metrics:export:wavefront:uri", "proxy://localhost:7828" }
    }.ToImmutableDictionary();

    public static IConfiguration GetConfigurationFromDictionary(IDictionary<string, string?> collection)
    {
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(collection);
        return builder.Build();
    }

    public static WebApplicationBuilder GetTestWebApplicationBuilder()
    {
        return GetTestWebApplicationBuilder([]);
    }

    public static WebApplicationBuilder GetTestWebApplicationBuilder(string[] args)
    {
        WebApplicationBuilder webAppBuilder = WebApplication.CreateBuilder(args);
        webAppBuilder.Host.UseDefaultServiceProvider(options => options.ValidateScopes = true);
        webAppBuilder.Configuration.AddInMemoryCollection(FastTestsConfiguration);
        webAppBuilder.WebHost.UseTestServer();
        webAppBuilder.Services.AddActionDescriptorCollectionProvider();
        return webAppBuilder;
    }

    public static Stream StringToStream(string text)
    {
        var stream = new MemoryStream();

        using (var writer = new StreamWriter(stream, leaveOpen: true))
        {
            writer.Write(text);
        }

        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }
}
