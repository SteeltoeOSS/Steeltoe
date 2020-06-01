// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Apache.Geode.Client;

namespace Steeltoe.CloudFoundry.Connector.Test
{
    public class BasicAuthInitialize : IAuthInitialize
    {
        private string _username;
        private string _password;

        public BasicAuthInitialize(string username, string password)
        {
            _username = username;
            _password = password;
        }

        public void Close()
        {
        }

        public Properties<string, object> GetCredentials(Properties<string, string> props, string server)
        {
            var credentials = new Properties<string, object>();

            credentials.Insert("security-username", _username);
            credentials.Insert("security-password", _password);

            return credentials;
        }
    }
}
