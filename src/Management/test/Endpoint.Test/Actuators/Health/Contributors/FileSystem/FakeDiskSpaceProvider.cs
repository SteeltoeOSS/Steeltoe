// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Actuators.Health.Contributors.FileSystem;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Health.Contributors.FileSystem;

internal sealed class FakeDiskSpaceProvider : IDiskSpaceProvider
{
    private readonly IDriveInfoWrapper[] _drives;
    private readonly FakeNetworkShareWrapper[] _networkShares;
    private readonly HashSet<string> _existingPaths;

    public bool IsRunningOnWindows { get; }

    public FakeDiskSpaceProvider(bool isRunningOnWindows, List<FakeDriveInfoWrapper> drives, List<FakeNetworkShareWrapper> networkShares,
        List<string> existingPaths)
    {
        ArgumentNullException.ThrowIfNull(drives);
        ArgumentNullException.ThrowIfNull(existingPaths);
        ArgumentNullException.ThrowIfNull(networkShares);

        IsRunningOnWindows = isRunningOnWindows;
        _drives = [.. drives];
        _networkShares = [.. networkShares];

        _existingPaths = new HashSet<string>(existingPaths, isRunningOnWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

        foreach (FakeDriveInfoWrapper drive in drives)
        {
            if (drive.RootDirectory.Exists)
            {
                _existingPaths.Add(drive.RootDirectory.FullName);
            }
        }

        foreach (FakeNetworkShareWrapper networkShare in networkShares)
        {
            _existingPaths.Add(networkShare.FullPath);
        }
    }

    public IList<IDriveInfoWrapper> GetDrives()
    {
        return _drives;
    }

    public IDirectoryInfoWrapper GetDirectoryInfo(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return new FakeDirectoryInfoWrapper(path, _existingPaths.Contains(path));
    }

    public INetworkShareWrapper? TryGetNetworkShare(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return Array.Find(_networkShares, share => share.ContainsDirectory(path));
    }
}
