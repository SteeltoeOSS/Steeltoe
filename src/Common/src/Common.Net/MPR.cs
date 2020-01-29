// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Text;
using static Steeltoe.Common.Net.WindowsNetworkFileShare;

namespace Steeltoe.Common.Net
{
    internal class MPR : IMPR
    {
        public MPR()
        {
            if (!Platform.IsWindows)
            {
                throw new PlatformNotSupportedException("Sorry, this functionality only works on Windows");
            }
        }

        public int AddConnection(NetResource netResource, string password, string username, int flags)
        {
            return NativeMethods.WNetAddConnection2(netResource, password, username, flags);
        }

        public int UseConnection(IntPtr hwndOwner, NetResource netResource, string password, string username, int flags, string lpAccessName, string lpBufferSize, string lpResult)
        {
            return NativeMethods.WNetUseConnection(hwndOwner, netResource, password, username, flags, lpAccessName, lpBufferSize, lpResult);
        }

        public int CancelConnection(string name, int flags, bool force)
        {
            return NativeMethods.WNetCancelConnection2(name, flags, force);
        }

        public int GetLastError(out int error, out StringBuilder errorBuf, int errorBufSize, out StringBuilder nameBuf, int nameBufSize)
        {
            return NativeMethods.WNetGetLastError(out error, out errorBuf, errorBufSize, out nameBuf, nameBufSize);
        }
    }
}
