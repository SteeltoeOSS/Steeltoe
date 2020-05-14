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

namespace Steeltoe.Common.Net.Test
{
    internal class FakeMPR : IMPR
    {
        internal string _username;
        internal string _password;
        internal string _networkpath;
        internal bool _shouldConnect;
        internal bool _cancelledConnection = false;

        public FakeMPR(bool shouldConnect = true)
        {
            _shouldConnect = shouldConnect;
        }

        public int AddConnection(NetResource netResource, string password, string username, int flags)
        {
            throw new NotImplementedException();
        }

        public int CancelConnection(string name, int flags, bool force)
        {
            _cancelledConnection = true;
            return 0;
        }

        public int GetLastError(out int error, out StringBuilder errorBuf, int errorBufSize, out StringBuilder nameBuf, int nameBufSize)
        {
            throw new NotImplementedException();
        }

        public int UseConnection(IntPtr hwndOwner, NetResource netResource, string password, string username, int flags, string lpAccessName, string lpBufferSize, string lpResult)
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
}
