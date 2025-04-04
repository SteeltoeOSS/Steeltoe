// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.Logfile;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Logfile;

public sealed class LogFileEndpointOptionsTest : BaseTest
{
    [Fact]
    public void AppliesDefaults()
    {
        LogFileEndpointOptions options = GetOptionsFromSettings<LogFileEndpointOptions, ConfigureLogFileEndpointOptions>();

        options.Id.Should().Be("logfile");
        options.RequiredPermissions.Should().Be(EndpointPermissions.Restricted);
        options.Path.Should().Be("logfile");
        options.FilePath.Should().BeNull();
        options.AllowedVerbs.Should().Contain("Get");
        options.AllowedVerbs.Should().HaveCount(1);
    }

    [Fact]
    public void CanOverrideDefaults()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:logfile:path"] = "testPath",
            ["management:endpoints:logfile:filePath"] = "logs/application.log"
        };

        LogFileEndpointOptions options = GetOptionsFromSettings<LogFileEndpointOptions, ConfigureLogFileEndpointOptions>(appSettings);

        options.Id.Should().Be("logfile");
        options.RequiredPermissions.Should().Be(EndpointPermissions.Restricted);
        options.Path.Should().Be("testPath");
        options.AllowedVerbs.Should().Contain("Get");
        options.FilePath.Should().Be("logs/application.log");
        options.AllowedVerbs.Should().Contain("Get");
        options.AllowedVerbs.Should().HaveCount(1);
    }
}
