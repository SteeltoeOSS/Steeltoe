// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Actuators.Health.Contributors.FileSystem;

internal sealed class DirectoryInfoWrapper : IDirectoryInfoWrapper
{
    private readonly Func<string> _getFullName;
    private readonly Func<bool> _getExists;

    public string FullName => _getFullName();
    public bool Exists => _getExists();

    public DirectoryInfoWrapper(DirectoryInfo directoryInfo)
    {
        ArgumentNullException.ThrowIfNull(directoryInfo);

        _getFullName = () => directoryInfo.FullName;
        _getExists = () => directoryInfo.Exists;
    }
}
