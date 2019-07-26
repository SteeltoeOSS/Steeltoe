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
