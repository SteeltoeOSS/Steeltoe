// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Xunit;

namespace Steeltoe.Common.Test.Util;

public sealed class DotSettingsTest
{
    private const string MasterSolutionFileName = "Steeltoe.All.sln";
    private const string DotSettingsSuffix = ".DotSettings";
    private const string MasterDotSettingsFileName = MasterSolutionFileName + DotSettingsSuffix;

    [Fact]
    public async Task DotSettingsFilesAreSynchronized()
    {
        DirectoryInfo masterSolutionDirectory = GetMasterSolutionDirectory();
        masterSolutionDirectory.Should().NotBeNull($"file '{MasterSolutionFileName}' should exist in a parent directory");

        string masterSettingsPath = Path.Combine(masterSolutionDirectory.FullName, MasterDotSettingsFileName);
        var masterSettingsFile = new FileInfo(masterSettingsPath);
        masterSettingsFile.Exists.Should().BeTrue($"file '{MasterDotSettingsFileName}' should exist in same directory as '{MasterSolutionFileName}'");

        string masterSettingsContent = await File.ReadAllTextAsync(masterSettingsFile.FullName);

        foreach (FileInfo solutionFile in masterSolutionDirectory.EnumerateFiles("*.sln", SearchOption.AllDirectories))
        {
            string settingsFileName = solutionFile.Name + DotSettingsSuffix;
            string settingsPath = Path.Combine(solutionFile.DirectoryName!, settingsFileName);
            var settingsFile = new FileInfo(settingsPath);

            if (string.Equals(settingsFile.FullName, masterSettingsFile.FullName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            settingsFile.Exists.Should().BeTrue($"file '{settingsFileName}' should exist in same directory as '{solutionFile.FullName}'");

            string settingsContent = await File.ReadAllTextAsync(settingsFile.FullName);
            bool equalsMasterDotSettingsContent = settingsContent == masterSettingsContent;

            equalsMasterDotSettingsContent.Should().BeTrue($"the contents of '{settingsFileName}' differs from '{MasterDotSettingsFileName}'. " +
                $"Apply your changes in '{MasterDotSettingsFileName}' instead, then run sync-dot-settings.ps1 to replicate your changes to all solution files");
        }
    }

    private static DirectoryInfo GetMasterSolutionDirectory()
    {
        var directory = new DirectoryInfo(Environment.CurrentDirectory);

        while (directory != null)
        {
            FileInfo[] files = directory.GetFiles(MasterSolutionFileName);

            if (files.Any())
            {
                return files.Single().Directory;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
