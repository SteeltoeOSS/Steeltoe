// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NETCOREAPP3_1
using System;

namespace Steeltoe.Management.TracingCore.Test
{
    internal static class AppCompat
    {
        public static void SetSwitches()
        {
            AppContext.SetSwitch(
                "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        }
    }
}
#endif
