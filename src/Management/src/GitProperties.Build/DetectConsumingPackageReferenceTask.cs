// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Steeltoe.Management.GitProperties.Build;

/// <summary>
/// Determines whether this project's own fully-resolved dependency graph includes any of <see cref="PackageIds" />. Used to auto-detect whether to
/// generate git.properties, so most projects in a large solution skip generation without opting out individually.
/// </summary>
/// <remarks>
/// Reads <see cref="ProjectAssetsFile" /> (project.assets.json) rather than this project's own @(PackageReference) items: NuGet flattens the entire
/// transitive graph, through both PackageReference and ProjectReference chains, into every consuming project's assets file, so this also detects a
/// shared library wrapping actuator registration on behalf of many host apps. The assets file is written by a prior, separate restore pass, so if it
/// doesn't exist yet (a fresh clone with no restore), this safely reports no match instead of failing the build.
/// </remarks>
// ReSharper disable once UnusedType.Global
public sealed class DetectConsumingPackageReferenceTask : Task
{
    /// <summary>
    /// Gets or sets the semicolon-separated list of package IDs to look for.
    /// </summary>
    /// <remarks>
    /// Deliberately not [Required]: MSBuild's required-parameter check treats an empty string the same as "not supplied", which would turn an explicitly
    /// blank value into a build error instead of the graceful "no package ID ever matches" outcome <see cref="ContainsAnyPackage" /> already produces. An
    /// explicit blank value does reach this property in one case: a global property set on the command line (e.g. "-p:GitPropertiesConsumingPackageIds=")
    /// can never be reassigned by the project's own conditional default, so it stays blank all the way through instead of falling back to the default
    /// package ID.
    /// </remarks>
    public string PackageIds { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the project's resolved assets file (typically $(ProjectAssetsFile)), or empty/nonexistent when the project has never been restored.
    /// </summary>
    public string ProjectAssetsFile { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether any of <see cref="PackageIds" /> was found.
    /// </summary>
    [Output]
    public bool HasReference { get; set; }

    /// <inheritdoc />
    public override bool Execute()
    {
        if (ProjectAssetsFile.Length > 0 && File.Exists(ProjectAssetsFile))
        {
            return this.LogOnFailure($"failed to read '{ProjectAssetsFile}' while checking for a consuming package reference", () =>
            {
                string content = File.ReadAllText(ProjectAssetsFile);
                HasReference = ContainsAnyPackage(content);
            });
        }

        return true;
    }

    private bool ContainsAnyPackage(string assetsFileContent)
    {
        foreach (string rawPackageId in PackageIds.Split(';'))
        {
            string packageId = rawPackageId.Trim();

            // A plain substring search for efficiency. Taking a dependency on a JSON parser (so we can search inside "libraries") is too intrusive.
            if (packageId.Length > 0 && assetsFileContent.IndexOf($"\"{packageId}/", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }
}
