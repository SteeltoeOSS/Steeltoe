// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

internal static class GitRepositoryBuilder
{
    private static readonly HashSet<string> SimulatedPushExcludedDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        "bin",
        "obj"
    };

    public static async Task InitializeEmptyAsync(string destination)
    {
        Directory.CreateDirectory(destination);
        await ProcessRunner.RunGitAsync(destination, "init", "--quiet", "--initial-branch=main", ".");
    }

    public static async Task InitializeAsync(string destination, int commitCount, bool includeFallbackFileInGitignore)
    {
        await InitializeEmptyAsync(destination);
        await ProcessRunner.RunGitAsync(destination, "config", "user.name", "Test User");
        await ProcessRunner.RunGitAsync(destination, "config", "user.email", "test@example.com");

        string gitignoreContent = includeFallbackFileInGitignore
            ? """
            bin/
            obj/
            git.properties
            """
            : """
            bin/
            obj/
            """;

        await File.WriteAllTextAsync(Path.Combine(destination, ".gitignore"), gitignoreContent, TestContext.Current.CancellationToken);

        for (int commitNumber = 1; commitNumber <= commitCount; commitNumber++)
        {
            await File.WriteAllTextAsync(Path.Combine(destination, $"file{commitNumber}.txt"), $"content {commitNumber}",
                TestContext.Current.CancellationToken);

            await CommitAllAsync(destination, $"Commit {commitNumber}");
        }
    }

    public static async Task CommitAllAsync(string repositoryDirectory, string subject, string? body = null)
    {
        await ProcessRunner.RunGitAsync(repositoryDirectory, "add", "-A");

        if (body == null)
        {
            await ProcessRunner.RunGitAsync(repositoryDirectory, "commit", "--quiet", "-m", subject);
        }
        else
        {
            await ProcessRunner.RunGitAsync(repositoryDirectory, "commit", "--quiet", "-m", subject, "-m", body);
        }
    }

    public static string SimulateSourcePush(string sourceDirectory, string destinationDirectory)
    {
        CopyDirectoryExcluding(new DirectoryInfo(sourceDirectory), destinationDirectory, SimulatedPushExcludedDirectoryNames);
        return destinationDirectory;
    }

    private static void CopyDirectoryExcluding(DirectoryInfo source, string destination, HashSet<string> excludedDirectoryNames)
    {
        Directory.CreateDirectory(destination);

        foreach (FileInfo file in source.GetFiles())
        {
            file.CopyTo(Path.Combine(destination, file.Name), true);
        }

        foreach (DirectoryInfo subDirectory in source.GetDirectories())
        {
            if (excludedDirectoryNames.Contains(subDirectory.Name))
            {
                continue;
            }

            CopyDirectoryExcluding(subDirectory, Path.Combine(destination, subDirectory.Name), excludedDirectoryNames);
        }
    }
}
