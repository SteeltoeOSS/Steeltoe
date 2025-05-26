// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Actuators.Health.Contributors.FileSystem;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Health.Contributors.FileSystem;

internal sealed class FakeNetworkShareWrapper : INetworkShareWrapper
{
    public string FullPath { get; }
    public ulong FreeBytesAvailable { get; }
    public ulong TotalNumberOfBytes { get; }

    public FakeNetworkShareWrapper(string fullPath, ulong freeBytesAvailable, ulong totalNumberOfBytes)
    {
        ArgumentNullException.ThrowIfNull(fullPath);

        FullPath = fullPath;
        FreeBytesAvailable = freeBytesAvailable;
        TotalNumberOfBytes = totalNumberOfBytes;
    }

    public bool ContainsDirectory(string path)
    {
        string pathRelativeToShareRoot = Path.GetRelativePath(FullPath, path);
        return path != pathRelativeToShareRoot;
    }
}
