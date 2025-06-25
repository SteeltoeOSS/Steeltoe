// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Net;

internal sealed class MultipleProviderRouter : IMultipleProviderRouter
{
    public MultipleProviderRouter()
    {
        if (!Platform.IsWindows)
        {
            throw new PlatformNotSupportedException("Sorry, this functionality only works on Windows");
        }
    }

    public int UseConnection(IntPtr hwndOwner, NativeMethods.NetResource netResource, string? password, string? username, int flags, string? lpAccessName,
        string? lpBufferSize, string? lpResult)
    {
        return NativeMethods.WNetUseConnection(hwndOwner, netResource, password, username, flags, lpAccessName, lpBufferSize, lpResult);
    }

    public int CancelConnection(string name, int flags, bool force)
    {
        return NativeMethods.WNetCancelConnection2(name, flags, force);
    }
}
