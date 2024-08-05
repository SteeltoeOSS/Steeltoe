// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace Steeltoe.Common.Net.Test;

internal sealed class FakeMultipleProviderRouter(bool shouldConnect = true) : IMultipleProviderRouter
{
    private readonly bool _shouldConnect = shouldConnect;

    internal string? Username { get; private set; }
    internal string? Password { get; private set; }
    internal string? NetworkPath { get; private set; }

    public int UseConnection(IntPtr hwndOwner, WindowsNetworkFileShare.NetResource netResource, string? password, string? username, int flags,
        string? lpAccessName, string? lpBufferSize, string? lpResult)
    {
        NetworkPath = netResource.RemoteName;
        Username = username;
        Password = password;

        if (_shouldConnect)
        {
            return 0;
        }

        // throw "bad device" error
        return 1200;
    }

    public int CancelConnection(string name, int flags, bool force)
    {
        return -1;
    }

    public int GetLastError(out int error, out StringBuilder errorBuf, int errorBufSize, out StringBuilder nameBuf, int nameBufSize)
    {
        throw new NotImplementedException();
    }
}
