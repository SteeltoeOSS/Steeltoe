// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;

namespace Steeltoe.Configuration.Kubernetes.ServiceBindings;

internal sealed class EnvironmentServiceBindingsReader : IServiceBindingsReader
{
    internal const string EnvironmentVariableName = "SERVICE_BINDING_ROOT";

    /// <inheritdoc />
    public IFileProvider? GetRootDirectory()
    {
        string? rootDirectory = Environment.GetEnvironmentVariable(EnvironmentVariableName);

        if (rootDirectory != null)
        {
            rootDirectory = Path.GetFullPath(rootDirectory);

            if (Directory.Exists(rootDirectory))
            {
                return new PhysicalFileProvider(rootDirectory, ExclusionFilters.Sensitive);
            }
        }

        return null;
    }
}
