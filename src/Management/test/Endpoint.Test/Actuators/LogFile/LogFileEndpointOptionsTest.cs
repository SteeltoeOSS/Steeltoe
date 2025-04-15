// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.LogFile;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Logfile;

public sealed class LogFileEndpointOptionsTest : BaseTest
{
    private readonly string[] _expectedAllowedVerbs = ["Get", "Head"];

    [Fact]
    public void AppliesDefaults()
    {
        LogFileEndpointOptions options = GetOptionsFromSettings<LogFileEndpointOptions, ConfigureLogFileEndpointOptions>();

        options.Id.Should().Be("logfile");
        options.RequiredPermissions.Should().Be(EndpointPermissions.Restricted);
        options.Path.Should().Be("logfile");
        options.FilePath.Should().BeNull();
        options.AllowedVerbs.Should().Contain(_expectedAllowedVerbs);
        options.AllowedVerbs.Should().HaveCount(2);
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
        options.FilePath.Should().Be("logs/application.log");
        options.AllowedVerbs.Should().Contain(_expectedAllowedVerbs);
        options.AllowedVerbs.Should().HaveCount(2);
    }
}
