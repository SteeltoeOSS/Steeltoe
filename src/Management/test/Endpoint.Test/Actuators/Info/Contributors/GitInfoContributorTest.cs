// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.Info;
using Steeltoe.Management.Endpoint.Actuators.Info.Contributors;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Info.Contributors;

public sealed class GitInfoContributorTest
{
    [Fact]
    public async Task Logs_warning_when_git_properties_file_not_found()
    {
        using var loggerProvider = new CapturingLoggerProvider();
        using var loggerFactory = new LoggerFactory([loggerProvider]);
        ILogger<GitInfoContributor> logger = loggerFactory.CreateLogger<GitInfoContributor>();

        var contributor = new GitInfoContributor("/path/to/missing-file", logger);
        var infoBuilder = new InfoBuilder();

        await contributor.ContributeAsync(infoBuilder, TestContext.Current.CancellationToken);

        IDictionary<string, object?> data = infoBuilder.Build();
        data.Should().BeEmpty();

        string logText = loggerProvider.GetAsText();
        logText.Should().Be($"WARN {typeof(GitInfoContributor)}: File '/path/to/missing-file' does not exist.");
    }

    [Fact]
    public async Task Can_read_empty_git_properties_file()
    {
        using var loggerProvider = new CapturingLoggerProvider();
        using var loggerFactory = new LoggerFactory([loggerProvider]);
        ILogger<GitInfoContributor> logger = loggerFactory.CreateLogger<GitInfoContributor>();

        string path = Path.Combine(System.Environment.CurrentDirectory, "empty.git.properties");

        var contributor = new GitInfoContributor(path, logger);
        var infoBuilder = new InfoBuilder();

        await contributor.ContributeAsync(infoBuilder, TestContext.Current.CancellationToken);

        IDictionary<string, object?> data = infoBuilder.Build();
        data.Should().BeEmpty();

        loggerProvider.GetAsText().Should().BeEmpty();
    }

    [Fact]
    public async Task Skips_malformed_lines_in_git_properties_file()
    {
        using var loggerProvider = new CapturingLoggerProvider();
        using var loggerFactory = new LoggerFactory([loggerProvider]);
        ILogger<GitInfoContributor> logger = loggerFactory.CreateLogger<GitInfoContributor>();

        string path = Path.Combine(System.Environment.CurrentDirectory, "garbage.git.properties");

        var contributor = new GitInfoContributor(path, logger);
        var infoBuilder = new InfoBuilder();

        await contributor.ContributeAsync(infoBuilder, TestContext.Current.CancellationToken);

        IDictionary<string, object?> data = infoBuilder.Build();

        string json = JsonSerializer.Serialize(data);

        json.Should().BeJson("""
            {
              "git": {
                "build": {
                  "user": {
                    "name": "John Doe"
                  }
                },
                "commit": {
                  "id": "",
                  "message": {
                    "short": "Changed A=B=C"
                  }
                }
              }
            }
            """);

        loggerProvider.GetAsText().Should().BeEmpty();
    }
}
