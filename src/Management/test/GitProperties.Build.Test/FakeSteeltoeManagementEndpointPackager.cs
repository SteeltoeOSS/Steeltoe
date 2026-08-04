// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

/// <summary>
/// Builds and packs a fake Steeltoe.Management.Endpoint project into an isolated local NuGet feed.
/// </summary>
internal static class FakeSteeltoeManagementEndpointPackager
{
    private const string PackageId = "Steeltoe.Management.Endpoint";
    private const string PackageVersion = "4.5.6";

    public static async Task<PackageReference> PackAsync(DirectoryInfo sessionDirectoryInfo, string nuGetFeedDirectory)
    {
        string sourceDirectory = sessionDirectoryInfo.CreateSubdirectory("fake-endpoint-source").FullName;

        string projectContent = $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFrameworks>{string.Join(';', TestAppTargetFramework.Multiple)}</TargetFrameworks>
                <PackageId>{PackageId}</PackageId>
                <IsPackable>true</IsPackable>
              </PropertyGroup>
            </Project>
            """;

        string projectFilePath = Path.Combine(sourceDirectory, $"{PackageId}.csproj");
        await File.WriteAllTextAsync(projectFilePath, projectContent, TestContext.Current.CancellationToken);

        await ProcessRunner.RunDotNetAsync(sourceDirectory, 0, null, "pack", $"-p:Version={PackageVersion}", "-o", nuGetFeedDirectory);
        return new PackageReference(PackageId, PackageVersion, null);
    }
}
