// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Apache.Geode.Client;

namespace Steeltoe.CloudFoundry.Connector.Test
{
    public class BadConstructorAuthInitializer : IAuthInitialize
    {
        public void Close()
        {
            throw new System.NotImplementedException();
        }

        public Properties<string, object> GetCredentials(Properties<string, string> props, string server)
        {
            throw new System.NotImplementedException();
        }
    }
}
