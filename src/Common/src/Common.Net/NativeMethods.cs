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
using System.Runtime.InteropServices;
using System.Text;
using static Steeltoe.Common.Net.WindowsNetworkFileShare;

namespace Steeltoe.Common.Net
{
    internal class NativeMethods
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
}
