//
// Copyright 2015 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using System;
using System.Collections.Generic;
using SteelToe.Extensions.Configuration.CloudFoundry;

namespace SteelToe.CloudFoundry.Connector.Services
{
    [ServiceInfoFactory]
    public abstract class ServiceInfoFactory : IServiceInfoFactory
    {
        private Tags _tags;
        private string[] _schemes;

        private List<string> uriKeys = new List<string> { "uri", "url" };

        public ServiceInfoFactory(Tags tags, string scheme) :
            this(tags, new string[] { scheme })
        {
            if (string.IsNullOrEmpty(scheme))
            {
                throw new ArgumentNullException(nameof(scheme));
            }
        }
        public ServiceInfoFactory(Tags tags, string[] schemes)
        {
            if (tags == null)
            {
                throw new ArgumentNullException(nameof(tags));
            }

            _tags = tags;
            _schemes = schemes;
            if (schemes != null)
            {
               foreach(string s in schemes)
                {
                    uriKeys.Add(s + "Uri");
                    uriKeys.Add(s + "uri");
                    uriKeys.Add(s + "Url");
                    uriKeys.Add(s + "url");
                }
            }
        }
        public virtual bool Accept(Service binding)
        {
            return TagsMatch(binding) || LabelStartsWithTag(binding) ||
                 UriMatchesScheme(binding) || UriKeyMatchesScheme(binding);
        }

        public abstract IServiceInfo Create(Service binding);


        internal virtual protected bool TagsMatch(Service binding)
        {

            var serviceTags = binding.Tags;
            return _tags.ContainsOne(serviceTags);
        }

        internal virtual protected bool LabelStartsWithTag(Service binding)
        {
            String label = binding.Label;
            return _tags.StartsWith(label);
        }

        internal virtual protected bool UriMatchesScheme(Service binding)
        {
            if (_schemes == null)
            {
                return false;
            }

            var credentials = binding.Credentials;
            if (credentials == null)
            {
                return false;
            }

            String uri = GetStringFromCredentials(binding.Credentials, uriKeys);
            if (uri != null)
            {
                foreach (string uriScheme in _schemes)
                {
                    if (uri.StartsWith(uriScheme + "://"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        internal protected virtual bool UriKeyMatchesScheme(Service binding)
        {
            if (_schemes == null)
            {
                return false;
            }

            var credentials = binding.Credentials;
            if (credentials == null)
            {
                return false;
            }

            foreach (string uriScheme in _schemes)
            {
                if (credentials.ContainsKey(uriScheme + "Uri") || credentials.ContainsKey(uriScheme + "uri") ||
                        credentials.ContainsKey(uriScheme + "Url") || credentials.ContainsKey(uriScheme + "url"))
                {
                    return true;
                }
            }
            return false;
        }

        private static List<string> _userList = new List<string>() { "user", "username" };
        internal protected virtual string GetUsernameFromCredentials(Dictionary<string, Credential> credentials)
        {
            return GetStringFromCredentials(credentials, _userList );
        }
        internal protected virtual string GetPasswordFromCredentials(Dictionary<string, Credential> credentials)
        {
            return GetStringFromCredentials(credentials, "password");
        }
        internal protected virtual int GetPortFromCredentials(Dictionary<string, Credential> credentials)
        {
            return GetIntFromCredentials(credentials, "port");
        }

        private static List<string> _hostList = new List<string>() { "hostname", "host" };
        internal protected virtual string GetHostFromCredentials(Dictionary<string, Credential> credentials)
        {
            return GetStringFromCredentials(credentials, _hostList);
        }

        internal protected virtual string GetUriFromCredentials(Dictionary<string, Credential> credentials)
        {
            return GetStringFromCredentials(credentials, uriKeys);
        }

        internal protected virtual string GetClientIdFromCredentials(Dictionary<string, Credential> credentials)
        {
            return GetStringFromCredentials(credentials, "client_id");
        }

        internal protected virtual string GetClientSecretFromCredentials(Dictionary<string, Credential> credentials)
        {
            return GetStringFromCredentials(credentials, "client_secret");
        }

        internal protected virtual string GetAccessTokenUriFromCredentials(Dictionary<string, Credential> credentials)
        {
            return GetStringFromCredentials(credentials, "access_token_uri");
        }

        internal protected virtual string GetStringFromCredentials(Dictionary<string, Credential> credentials, string key)
        {
            return GetStringFromCredentials(credentials, new List<string>() { key });
        }

        internal protected virtual string GetStringFromCredentials(Dictionary<string, Credential> credentials, List<string> keys)
        {
            if (credentials != null)
            {
                foreach (string key in keys)
                {
                    if (credentials.ContainsKey(key))
                    {
                        return credentials[key].Value;
                    }
                }
            }
            return null;
        }

        internal protected virtual int GetIntFromCredentials(Dictionary<string, Credential> credentials, string key)
        {
            return GetIntFromCredentials(credentials, new List<string>() { key });
        }

        internal protected virtual int GetIntFromCredentials(Dictionary<string, Credential> credentials, List<string> keys)
        {
            int result = 0;

            if (credentials != null)
            {
                foreach (string key in keys)
                {
                    if (credentials.ContainsKey(key))
                    {
                        result = int.Parse(credentials[key].Value);
                    }
                }
            }
            return result;
        }

        internal protected virtual List<string> GetListFromCredentials(Dictionary<string, Credential> credentials, string key)
        {
            List<string> result = new List<string>();
            if (credentials != null)
            {
               if (credentials.ContainsKey(key))
                {
                    Credential keyVal = credentials[key];
                    if (keyVal.Count > 0)
                    {
                        foreach(KeyValuePair<string, Credential> kvp in keyVal)
                        {
                            if (kvp.Value.Count != 0 || string.IsNullOrEmpty(kvp.Value.Value))
                            {
                                throw new ConnectorException(string.Format("Unable to extract list from credentials: key={0}, value={1}/{2}", key, kvp.Key, kvp.Value));
                            } else
                            {
                                result.Add(kvp.Value.Value);
                            }
                        }
                    }

                }
            }

            return result;
        }


        public virtual string DefaultUriScheme
        {
            get
            {
                if (_schemes != null && _schemes.Length > 0)
                    return _schemes[0];
                else
                    return null;
            }
            
        }

    }
}
