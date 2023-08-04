// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.SpringBootAdminClient;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.SpringBootAdminClient;

public sealed class SpringBootAdminClientOptionsTest : BaseTest
{
    [Fact]
    public void ConstructorFailsWithoutBaseAppUrl()
    {
        var appsettings = new Dictionary<string, string?>();

        var exception = Assert.Throws<InvalidOperationException>(() => GetOptionsFromSettings<SpringBootAdminClientOptions>(appsettings));

        Assert.Equal("Please set spring:boot:admin:client:BasePath in order to register with Spring Boot Admin", exception.Message);
    }

    [Fact]
    public void ConstructorUsesAppInfo()
    {
        var appsettings = new Dictionary<string, string?>
        {
            ["application:Uris:0"] = "http://somehost"
        };

        var options = GetOptionsFromSettings<SpringBootAdminClientOptions>(appsettings);

        Assert.NotNull(options);
        Assert.Equal("http://somehost", options.BasePath);
    }

    [Fact]
    public void Constructor_BindsConfiguration()
    {
        var appsettings = new Dictionary<string, string?>
        {
            ["management:endpoints:path"] = "/management",
            ["management:endpoints:health:path"] = "myhealth",
            ["URLS"] = "http://localhost:8080;https://localhost:8082",
            ["spring:boot:admin:client:url"] = "http://springbootadmin:9090",
            ["spring:boot:admin:client:metadata:user.name"] = "userName",
            ["spring:boot:admin:client:metadata:user.password"] = "userPassword",
            ["spring:application:name"] = "MySteeltoeApplication",
            ["ApplicationName"] = "OtherApplicationName"
        };

        var options = GetOptionsFromSettings<SpringBootAdminClientOptions>(appsettings);

        Assert.NotNull(options);
        Assert.Equal("MySteeltoeApplication", options.ApplicationName);
        Assert.Equal("http://localhost:8080", options.BasePath);
        Assert.Equal("http://springbootadmin:9090", options.Url);

        Assert.Contains(new KeyValuePair<string, object>("user.name", "userName"), options.Metadata);
        Assert.Contains(new KeyValuePair<string, object>("user.password", "userPassword"), options.Metadata);
    }

    [Fact]
    public void Constructor_BindsFallBack()
    {
        var appsettings = new Dictionary<string, string?>
        {
            ["spring:boot:admin:client:basepath"] = "http://somehost"
        };

        var options = GetOptionsFromSettings<SpringBootAdminClientOptions>(appsettings);

        Assert.NotNull(options);
        Assert.NotNull(options.ApplicationName);
        Assert.NotEmpty(options.ApplicationName);
    }
}
