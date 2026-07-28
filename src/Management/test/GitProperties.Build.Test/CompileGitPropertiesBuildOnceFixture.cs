// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

/// <summary>
/// Compiles project Steeltoe.Management.GitProperties.Build in a temporary directory (once per test run) to speed up running tests.
/// </summary>
internal sealed class CompileGitPropertiesBuildOnceFixture : IAsyncLifetime
{
    private string? _outputDirectory;

    public static string TargetsFilePath { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        string outputDirectory = Path.Combine(Path.GetTempPath(), "steeltoe-shared-git-properties-build-copy");

        if (Directory.Exists(outputDirectory))
        {
            Directory.Delete(outputDirectory, true);
        }

        Directory.CreateDirectory(outputDirectory);
        TargetsFilePath = await TestProjectWriter.BuildSharedGitPropertiesBuildCopyAsync(outputDirectory);
        _outputDirectory = outputDirectory;
    }

    public ValueTask DisposeAsync()
    {
        if (_outputDirectory != null)
        {
            try
            {
                Directory.Delete(_outputDirectory, true);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Best-effort cleanup only: a transiently locked file (e.g. an antivirus scan) must not fail the test run.
            }
        }

        return ValueTask.CompletedTask;
    }
}
