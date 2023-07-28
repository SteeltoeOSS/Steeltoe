// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.SpringBootAdminClient;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.SpringBootAdminClient;

public sealed class SpringBootAdminClientOptionsTest
{
    [Fact]
    public void ConstructorFailsWithoutBaseAppUrl()
    {
        var exception = Assert.Throws<NullReferenceException>(() =>
            new SpringBootAdminClientOptions(new ConfigurationBuilder().Build(), new ApplicationInstanceInfo()));

        Assert.Contains(":BasePath in order to register with Spring Boot Admin", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ConstructorUsesAppInfo()
    {
        var appsettings = new Dictionary<string, string>
        {
            { "application:Uris:0", "http://somehost" }
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();
        var appInfo = new ApplicationInstanceInfo(configurationRoot, string.Empty);

        var options = new SpringBootAdminClientOptions(configurationRoot, appInfo);

        Assert.NotNull(options);
        Assert.Equal("http://somehost", options.BasePath);
    }

    [Fact]
    public void Constructor_BindsConfiguration()
    {
        var appsettings = new Dictionary<string, string>
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

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();

        var options = new SpringBootAdminClientOptions(configurationRoot, new ApplicationInstanceInfo(configurationRoot));

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
        var appsettings = new Dictionary<string, string>
        {
            { "spring:boot:admin:client:basepath", "http://somehost" }
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();

        var options = new SpringBootAdminClientOptions(configurationRoot, new ApplicationInstanceInfo(configurationRoot));

        Assert.NotNull(options);
        Assert.NotEmpty(options.ApplicationName);
    }
}
