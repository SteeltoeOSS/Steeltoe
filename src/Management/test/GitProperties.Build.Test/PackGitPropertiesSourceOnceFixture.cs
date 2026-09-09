// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

/// <summary>
/// Packs project Steeltoe.Management.GitProperties.Build into an isolated local NuGet feed (once per test run) to speed up running tests.
/// </summary>
internal sealed class PackGitPropertiesSourceOnceFixture : IAsyncLifetime
{
    private string? _sessionDirectory;

    public static string SessionDirectory { get; private set; } = null!;
    public static string TempDirectory { get; private set; } = null!;
    public static NuGetSource Source { get; private set; } = null!;
    public static PackageReference GitPropertiesPackageReference { get; private set; } = null!;
    public static PackageReference FakeEndpointPackageReference { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        string directoryName = $"steeltoe-git-properties-test-session-{$"{Guid.NewGuid():N}"[..8]}";
        string tempPath = await ResolvePhysicalPathAsync(Path.GetTempPath());
        string sessionDirectory = new DirectoryInfo(tempPath).CreateSubdirectory(directoryName).FullName;
        SessionDirectory = sessionDirectory;

        var sessionDirectoryInfo = new DirectoryInfo(sessionDirectory);
        TempDirectory = sessionDirectoryInfo.CreateSubdirectory("temp").FullName;
        Source = CreateNuGetSource(sessionDirectoryInfo);

        GitPropertiesPackageReference = await GitPropertiesSourcePackager.PackAsync(Source.FeedDirectory);
        FakeEndpointPackageReference = await FakeSteeltoeManagementEndpointPackager.PackAsync(sessionDirectoryInfo, Source.FeedDirectory);
        _sessionDirectory = sessionDirectory;
    }

    private static NuGetSource CreateNuGetSource(DirectoryInfo sessionDirectoryInfo)
    {
        string feedDirectory = sessionDirectoryInfo.CreateSubdirectory("feed").FullName;
        string packagesDirectory = sessionDirectoryInfo.CreateSubdirectory("packages").FullName;
        return new NuGetSource(feedDirectory, packagesDirectory);
    }

    private static async Task<string> ResolvePhysicalPathAsync(string path)
    {
        if (OperatingSystem.IsMacOS())
        {
            // On macOS, $TMPDIR resolves through a symlink (/var -> /private/var).
            string output = await ProcessRunner.RunPwdAsync(path);
            return output.Trim();
        }

        return path;
    }

    public ValueTask DisposeAsync()
    {
        if (_sessionDirectory != null)
        {
            try
            {
                Directory.Delete(_sessionDirectory, true);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Best-effort cleanup only: a transiently locked file (e.g. an antivirus scan) must not fail the test run.
            }
        }

        return ValueTask.CompletedTask;
    }
}
