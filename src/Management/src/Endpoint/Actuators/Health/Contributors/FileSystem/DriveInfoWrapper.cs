// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Actuators.Health.Contributors.FileSystem;

internal sealed class DriveInfoWrapper : IDriveInfoWrapper
{
    private readonly Func<long> _getTotalFreeSpace;
    private readonly Func<long> _getTotalSize;
    private readonly Func<DirectoryInfoWrapper> _getRootDirectory;

    public long TotalFreeSpace => _getTotalFreeSpace();
    public long TotalSize => _getTotalSize();
    public IDirectoryInfoWrapper RootDirectory => _getRootDirectory();

    public DriveInfoWrapper(DriveInfo driveInfo)
    {
        ArgumentNullException.ThrowIfNull(driveInfo);

        _getTotalFreeSpace = () => driveInfo.TotalFreeSpace;
        _getTotalSize = () => driveInfo.TotalSize;
        _getRootDirectory = () => new DirectoryInfoWrapper(driveInfo.RootDirectory);
    }

    public static DriveInfoWrapper[] GetDrives()
    {
        return DriveInfo.GetDrives().Select(drive => new DriveInfoWrapper(drive)).ToArray();
    }
}
