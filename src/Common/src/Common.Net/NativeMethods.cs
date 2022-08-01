// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Text;
using static Steeltoe.Common.Net.WindowsNetworkFileShare;

namespace Steeltoe.Common.Net;

internal static class NativeMethods
{
    [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
    internal static extern int WNetAddConnection2(
        NetResource netResource,
        string password,
        string username,
        int flags);

    [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
    internal static extern int WNetCancelConnection2(
        string name,
        int flags,
        bool force);

    [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
    internal static extern int WNetUseConnection(
        IntPtr hwndOwner,
        NetResource netResource,
        string password,
        string username,
        int flags,
        string lpAccessName,
        string lpBufferSize,
        string lpResult);

    [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
    internal static extern int WNetGetLastError(
        out int error,
        out StringBuilder errorBuf,
        int errorBufSize,
        out StringBuilder nameBuf,
        int nameBufSize);
}
