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
        var jsonpath = sandbox.CreateFile("appsettings.json", appsettingsJson);
        var jsonfileName = Path.GetFileName(jsonpath);
        var xmlpath = sandbox.CreateFile("appsettings.xml", appsettingsXml);
        var xmlfileName = Path.GetFileName(xmlpath);
        var directory = Path.GetDirectoryName(jsonpath);

        var hostBuilder = WebApplication.CreateBuilder();
        hostBuilder.Configuration.SetBasePath(directory);
        hostBuilder.Configuration.AddJsonFile(jsonfileName);
        hostBuilder.Configuration.AddXmlFile(xmlfileName);
        hostBuilder.AddPlaceholderResolver();

        using var server = hostBuilder.Build();
        var config = server.Services.GetServices<IConfiguration>().First();
        Assert.Equal("myName", config["spring:cloud:config:name"]);
    }
}
#endif
