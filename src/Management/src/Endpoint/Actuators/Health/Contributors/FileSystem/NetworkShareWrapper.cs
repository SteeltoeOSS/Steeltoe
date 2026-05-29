// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace Steeltoe.Management.Endpoint.Actuators.Health.Contributors.FileSystem;

internal sealed partial class NetworkShareWrapper : INetworkShareWrapper
{
    public ulong FreeBytesAvailable { get; }
    public ulong TotalNumberOfBytes { get; }

    private NetworkShareWrapper(ulong freeBytesAvailable, ulong totalNumberOfBytes)
    {
        FreeBytesAvailable = freeBytesAvailable;
        TotalNumberOfBytes = totalNumberOfBytes;
    }

    public static NetworkShareWrapper? TryCreate(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return NativeMethods.GetDiskFreeSpaceEx(path, out ulong freeBytesAvailable, out ulong totalNumberOfBytes, out _)
            ? new NetworkShareWrapper(freeBytesAvailable, totalNumberOfBytes)
            : null;
    }

    private static partial class NativeMethods
    {
        [LibraryImport("kernel32.dll", EntryPoint = "GetDiskFreeSpaceExW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool GetDiskFreeSpaceEx(string lpDirectoryName, out ulong lpFreeBytesAvailableToCaller, out ulong lpTotalNumberOfBytes,
            out ulong lpTotalNumberOfFreeBytes);
    }
}
