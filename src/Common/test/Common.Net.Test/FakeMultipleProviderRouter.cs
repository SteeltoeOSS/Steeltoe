// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace Steeltoe.Common.Net.Test;

internal sealed class FakeMultipleProviderRouter : IMultipleProviderRouter
{
    internal string Username;
    internal string Password;
    internal string NetworkPath;
    internal bool ShouldConnect;

    public FakeMultipleProviderRouter(bool shouldConnect = true)
    {
        this.ShouldConnect = shouldConnect;
    }

    public int AddConnection(WindowsNetworkFileShare.NetResource netResource, string password, string username, int flags)
    {
        throw new NotImplementedException();
    }

    public int CancelConnection(string name, int flags, bool force)
    {
        return -1;
    }

    public int GetLastError(out int error, out StringBuilder errorBuf, int errorBufSize, out StringBuilder nameBuf, int nameBufSize)
    {
        throw new NotImplementedException();
    }

    public int UseConnection(IntPtr hwndOwner, WindowsNetworkFileShare.NetResource netResource, string password, string username, int flags, string lpAccessName, string lpBufferSize, string lpResult)
    {
        NetworkPath = netResource.RemoteName;
        this.Username = username;
        this.Password = password;
        if (ShouldConnect)
        {
            return 0;
        }
        else
        {
            // throw "bad device" error
            return 1200;
        }
    }
}
