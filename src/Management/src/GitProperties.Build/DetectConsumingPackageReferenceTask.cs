// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Steeltoe.Management.GitProperties.Build;

/// <summary>
/// Determines whether this project's own fully-resolved dependency graph includes any of <see cref="PackageIds" /> - used to smart-default
/// $(GenerateGitProperties) so the vast majority of projects in a large solution (class libraries, test projects, anything that can't possibly expose an
/// actuator) skip generation entirely, without requiring each of them to opt out individually.
/// </summary>
/// <remarks>
/// Reads <see cref="ProjectAssetsFile" /> (project.assets.json) rather than this project's own @(PackageReference) items deliberately: NuGet flattens
/// the *entire* transitive graph - through both PackageReference and ProjectReference chains - into every consuming project's own assets file, the same
/// mechanism that already lets a shared base library's own dependencies "just work" for whoever references it, without redeclaring them. That means this
/// also correctly detects the common pattern of a shared library wrapping actuator registration on behalf of many host apps, as long as that library's
/// own reference isn't PrivateAssets="All" - which would also strip the actuator assembly from every host's own runtime output, breaking the feature
/// outright, so it's not a viable pattern for a library that actually activates actuators in the first place. The file is written by a prior, separate
/// restore pass (implicit or explicit) - never by a target within the current build - so there's no ordering dependency on any of our own targets/hooks:
/// it's either already on disk with the final, fully-resolved graph, or it doesn't exist yet (a fresh clone with no restore at all), in which case this
/// safely reports no match instead of failing the build.
/// </remarks>
// ReSharper disable once UnusedType.Global
public sealed class DetectConsumingPackageReferenceTask : Task
{
    /// <summary>
    /// Gets or sets the semicolon-separated list of package IDs to look for.
    /// </summary>
    /// <remarks>
    /// Deliberately not [Required], unlike every other string parameter on tasks in this project: MSBuild's required-parameter check treats an empty string
    /// the same as "not supplied" at all, which would turn $(GitPropertiesConsumingPackageIds) explicitly set to blank (e.g. via
    /// "-p:GitPropertiesConsumingPackageIds=") into a build error instead of the well-defined, graceful "no package ID ever matches" outcome
    /// <see cref="ContainsAnyPackage" /> already produces for it.
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
        bool hasReference = false;

        if (ProjectAssetsFile.Length > 0 && File.Exists(ProjectAssetsFile))
        {
            try
            {
                string content = File.ReadAllText(ProjectAssetsFile);
                hasReference = ContainsAnyPackage(content);
            }
            catch (Exception exception)
            {
                Log.LogError($"git.properties: failed to read '{ProjectAssetsFile}' while checking for a consuming package reference:" +
                    $"{Environment.NewLine}{exception}");

                return false;
            }
        }

        HasReference = hasReference;
        return true;
    }

    /// <summary>
    /// A plain substring search for the quoted package ID plus a trailing slash - the shape every "libraries" entry key in project.assets.json takes
    /// ("PackageId/Version") - rather than a full JSON parse. Deliberately not scoped to the "libraries" object specifically, and deliberately not guarding
    /// against a configured ID that happens to collide with an unrelated NuGet content-folder name (e.g. "lib", "tools", "analyzers", which show up as path
    /// prefixes inside every package's own file list): Steeltoe's own consumers are enterprise teams that namespace-qualify their packages (e.g.
    /// "Contoso.Actuators"), so a configured ID colliding with a short, generic folder name isn't a realistic scenario worth the complexity of a bounded
    /// parse to rule out.
    /// </summary>
    private bool ContainsAnyPackage(string assetsFileContent)
    {
        foreach (string rawPackageId in PackageIds.Split(';'))
        {
            string packageId = rawPackageId.Trim();

            if (packageId.Length > 0 && assetsFileContent.IndexOf($"\"{packageId}/", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }
}
