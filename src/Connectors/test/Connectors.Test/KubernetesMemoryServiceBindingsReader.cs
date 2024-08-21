// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.FileProviders;
using Steeltoe.Configuration.Kubernetes.ServiceBinding;

namespace Steeltoe.Connectors.Test;

internal sealed class KubernetesMemoryServiceBindingsReader(MemoryFileProvider? fileProvider) : IServiceBindingsReader
{
    private readonly MemoryFileProvider? _fileProvider = fileProvider;

    public IFileProvider? GetRootDirectory()
    {
        return _fileProvider;
    }
}
