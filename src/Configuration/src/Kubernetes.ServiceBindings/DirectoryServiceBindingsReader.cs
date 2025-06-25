// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;

namespace Steeltoe.Configuration.Kubernetes.ServiceBindings;

internal sealed class DirectoryServiceBindingsReader : IServiceBindingsReader
{
    private readonly string _rootDirectory;

    public DirectoryServiceBindingsReader(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(rootDirectory);

        _rootDirectory = rootDirectory;
    }

    /// <inheritdoc />
    public IFileProvider? GetRootDirectory()
    {
        string rootDirectory = Path.GetFullPath(_rootDirectory);

        if (Directory.Exists(rootDirectory))
        {
            return new PhysicalFileProvider(rootDirectory, ExclusionFilters.Sensitive);
        }

        return null;
    }
}
