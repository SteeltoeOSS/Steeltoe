// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;

namespace Steeltoe.Common.Net.Test;

internal sealed class FakeMultipleProviderRouter : IMultipleProviderRouter
{
    internal string _username;
    internal string _password;
    internal string _networkpath;
    internal bool _shouldConnect;

    public FakeMultipleProviderRouter(bool shouldConnect = true)
    {
        _shouldConnect = shouldConnect;
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
        _networkpath = netResource.RemoteName;
        _username = username;
        _password = password;
        if (_shouldConnect)
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
