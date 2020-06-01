// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CloudFoundry.Connector.Services.Test
{
    internal class TestUriServiceInfo : UriServiceInfo
    {
        public TestUriServiceInfo(string id, string uri)
            : base(id, uri)
        {
        }

        public TestUriServiceInfo(string id, string scheme, string host, int port, string username, string password, string path)
            : base(id, scheme, host, port, username, password, path)
        {
        }
    }
}
