// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.TestResources;
using Steeltoe.Common.TestResources.IO;
using Steeltoe.Management.Endpoint.Actuators.Info;
using Steeltoe.Management.Endpoint.Actuators.Info.Contributors;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Info.Contributors;

public sealed class GitInfoContributorTest
{
    [Fact]
    public void Default_path_prefers_base_directory_over_current_directory()
    {
        using var baseDirectory = new Sandbox();
        using var currentDirectory = new Sandbox();

        string baseDirectoryFile = baseDirectory.CreateFile("git.properties", "git.commit.id=from-base-directory");
        currentDirectory.CreateFile("git.properties", "git.commit.id=from-current-directory");

        string resolvedPath = GitInfoContributor.ResolveDefaultPropertiesPath(baseDirectory.FullPath, currentDirectory.FullPath);

        resolvedPath.Should().Be(baseDirectoryFile);
    }

    [Fact]
    public void Default_path_falls_back_to_current_directory_when_not_found_in_base_directory()
    {
        using var baseDirectory = new Sandbox();
        using var currentDirectory = new Sandbox();

        string currentDirectoryFile = currentDirectory.CreateFile("git.properties", "git.commit.id=from-current-directory");

        string resolvedPath = GitInfoContributor.ResolveDefaultPropertiesPath(baseDirectory.FullPath, currentDirectory.FullPath);

        resolvedPath.Should().Be(currentDirectoryFile);
    }

    [Fact]
    public void Default_path_falls_back_to_current_directory_when_not_found_anywhere()
    {
        using var baseDirectory = new Sandbox();
        using var currentDirectory = new Sandbox();

        string resolvedPath = GitInfoContributor.ResolveDefaultPropertiesPath(baseDirectory.FullPath, currentDirectory.FullPath);

        resolvedPath.Should().Be(Path.Combine(currentDirectory.FullPath, "git.properties"));
    }

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
    public async Task Multi_line_commit_message_keeps_the_escaped_literal_backslash_n()
    {
        using var directory = new Sandbox();

        string path = directory.CreateFile("git.properties", """
            git.commit.message.short=Fix null reference in health check
            git.commit.message.full=Fix null reference in health check\n\nAdds a null check before calling Ping().
            """);

        using var loggerFactory = new LoggerFactory();
        var contributor = new GitInfoContributor(path, loggerFactory.CreateLogger<GitInfoContributor>());
        var infoBuilder = new InfoBuilder();

        await contributor.ContributeAsync(infoBuilder, TestContext.Current.CancellationToken);

        IDictionary<string, object?> data = infoBuilder.Build();
        string json = JsonSerializer.Serialize(data);

        json.Should().BeJson("""
            {
              "git": {
                "commit": {
                  "message": {
                    "full": "Fix null reference in health check\\n\\nAdds a null check before calling Ping().",
                    "short": "Fix null reference in health check"
                  }
                }
              }
            }
            """);
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
