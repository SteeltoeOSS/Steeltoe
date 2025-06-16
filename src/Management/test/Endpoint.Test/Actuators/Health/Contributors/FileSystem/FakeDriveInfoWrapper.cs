// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Actuators.Health.Contributors.FileSystem;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Health.Contributors.FileSystem;

internal sealed class FakeDriveInfoWrapper : IDriveInfoWrapper
{
    public long TotalFreeSpace { get; }
    public long TotalSize { get; }
    public IDirectoryInfoWrapper RootDirectory { get; }

    public FakeDriveInfoWrapper(long totalFreeSpace, long totalSize, string rootDirectory)
    {
        ArgumentNullException.ThrowIfNull(rootDirectory);

        TotalFreeSpace = totalFreeSpace;
        TotalSize = totalSize;
        RootDirectory = new FakeDirectoryInfoWrapper(rootDirectory, true);
    }
}
