// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Utils.IO;
using Xunit;

namespace Steeltoe.Extensions.Configuration.Placeholder.Test;

public partial class PlaceholderResolverExtensionsTest
{
    [Fact]
    public void AddPlaceholderResolverViaWebApplicationBuilderWorks()
    {
        var appsettingsJson = @"
            {
                ""spring"": {
                    ""json"": {
                        ""name"": ""myName""
                },
                  ""cloud"": {
                    ""config"": {
                        ""name"" : ""${spring:xml:name?noname}"",
                    }
                  }
                }
            }";
        var appsettingsXml = @"
            <settings>
                <spring>
                    <xml>
                        <name>${spring:json:name?noName}</name>
                    </xml>
                </spring>
            </settings>";
        using var sandbox = new Sandbox();
        var jsonPath = sandbox.CreateFile("appsettings.json", appsettingsJson);
        var jsonFileName = Path.GetFileName(jsonPath);
        var xmlPath = sandbox.CreateFile("appsettings.xml", appsettingsXml);
        var xmlFileName = Path.GetFileName(xmlPath);
        var directory = Path.GetDirectoryName(jsonPath);

        var hostBuilder = WebApplication.CreateBuilder();
        hostBuilder.Configuration.SetBasePath(directory);
        hostBuilder.Configuration.AddJsonFile(jsonFileName);
        hostBuilder.Configuration.AddXmlFile(xmlFileName);
        hostBuilder.AddPlaceholderResolver();

        using var server = hostBuilder.Build();
        var config = server.Services.GetServices<IConfiguration>().First();
        Assert.Equal("myName", config["spring:cloud:config:name"]);
    }
}
#endif
