// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Actuators.Health.Contributors.FileSystem;

internal sealed class DiskSpaceProvider : IDiskSpaceProvider
{
    public bool IsRunningOnWindows => Platform.IsWindows;

    public IList<IDriveInfoWrapper> GetDrives()
    {
        return DriveInfoWrapper.GetDrives().Cast<IDriveInfoWrapper>().ToArray();
    }

    public IDirectoryInfoWrapper GetDirectoryInfo(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return new DirectoryInfoWrapper(new DirectoryInfo(path));
    }

    public INetworkShareWrapper? TryGetNetworkShare(string path)
    {
        return NetworkShareWrapper.TryCreate(path);
    }
}
