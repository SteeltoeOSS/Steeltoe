﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace Steeltoe.Connector.Services
{
    [ServiceInfoFactory]
    public abstract class ServiceInfoFactory : IServiceInfoFactory
    {
        private static readonly List<string> _userList = new List<string>() { "user", "username", "uid" };
        private static readonly List<string> _passwordList = new List<string>() { "password", "pw" };
        private static readonly List<string> _hostList = new List<string>() { "hostname", "host" };

        public ServiceInfoFactory(Tags tags, string scheme)
            : this(tags, new string[] { scheme })
        {
            if (string.IsNullOrEmpty(scheme))
            {
                throw new ArgumentNullException(nameof(scheme));
            }
        }

        public ServiceInfoFactory(Tags tags, string[] schemes)
        {
            ServiceInfoTags = tags ?? throw new ArgumentNullException(nameof(tags));
            UriSchemes = schemes;
            if (schemes != null)
            {
                foreach (var s in schemes)
                {
                    UriKeys.Add(s + "uri");
                    UriKeys.Add(s + "url");
                }
            }
        }

        public virtual string DefaultUriScheme
        {
            get
            {
                if (UriSchemes != null && UriSchemes.Length > 0)
                {
                    return UriSchemes[0];
                }
                else
                {
                    return null;
                }
            }
        }

        public virtual bool Accept(Service binding)
        {
            return TagsMatch(binding) || LabelStartsWithTag(binding) ||
                 UriMatchesScheme(binding) || UriKeyMatchesScheme(binding);
        }

        public abstract IServiceInfo Create(Service binding);

        protected Tags ServiceInfoTags { get; set; }

        protected List<string> UriKeys { get; set; } = new List<string> { "uri", "url" };

        protected string[] UriSchemes { get; set; }

        protected internal virtual bool TagsMatch(Service binding)
        {
            var serviceTags = binding.Tags;
            return ServiceInfoTags.ContainsOne(serviceTags);
        }

        protected internal virtual bool LabelStartsWithTag(Service binding)
        {
            var label = binding.Label;
            return ServiceInfoTags.StartsWith(label);
        }

        protected internal virtual bool UriMatchesScheme(Service binding)
        {
            if (UriSchemes == null)
            {
                return false;
            }

            var credentials = binding.Credentials;
            if (credentials == null)
            {
                return false;
            }

            var uri = GetStringFromCredentials(binding.Credentials, UriKeys);
            if (uri != null)
            {
                foreach (var uriScheme in UriSchemes)
                {
                    if (uri.StartsWith(uriScheme + "://", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        protected internal virtual bool UriKeyMatchesScheme(Service binding)
        {
            if (UriSchemes == null)
            {
                return false;
            }

            var credentials = binding.Credentials;
            if (credentials == null)
            {
                return false;
            }

            foreach (var uriScheme in UriSchemes)
            {
                if (credentials.ContainsKey(uriScheme + "Uri") || credentials.ContainsKey(uriScheme + "uri") || credentials.ContainsKey(uriScheme + "Url") || credentials.ContainsKey(uriScheme + "url"))
                {
                    return true;
                }
            }

            return false;
        }

        protected internal virtual string GetUsernameFromCredentials(Dictionary<string, Credential> credentials)
        {
            return GetStringFromCredentials(credentials, _userList);
        }

        protected internal virtual string GetPasswordFromCredentials(Dictionary<string, Credential> credentials)
        {
            return GetStringFromCredentials(credentials, _passwordList);
        }

        protected internal virtual int GetPortFromCredentials(Dictionary<string, Credential> credentials)
        {
            return GetIntFromCredentials(credentials, "port");
        }

        protected internal virtual int GetTlsPortFromCredentials(Dictionary<string, Credential> credentials)
        {
            return GetIntFromCredentials(credentials, "tls_port");
        }

        protected internal virtual string GetHostFromCredentials(Dictionary<string, Credential> credentials)
        {
            return GetStringFromCredentials(credentials, _hostList);
        }

        protected internal virtual string GetUriFromCredentials(Dictionary<string, Credential> credentials)
        {
            return GetStringFromCredentials(credentials, UriKeys);
        }

        protected internal virtual string GetClientIdFromCredentials(Dictionary<string, Credential> credentials)
        {
            return GetStringFromCredentials(credentials, "client_id");
        }

        protected internal virtual string GetClientSecretFromCredentials(Dictionary<string, Credential> credentials)
        {
            return GetStringFromCredentials(credentials, "client_secret");
        }

        protected internal virtual string GetAccessTokenUriFromCredentials(Dictionary<string, Credential> credentials)
        {
            return GetStringFromCredentials(credentials, "access_token_uri");
        }

        protected internal virtual string GetStringFromCredentials(Dictionary<string, Credential> credentials, string key)
        {
            return GetStringFromCredentials(credentials, new List<string>() { key });
        }

        protected internal virtual string GetStringFromCredentials(Dictionary<string, Credential> credentials, List<string> keys)
        {
            if (credentials != null)
            {
                foreach (var key in keys)
                {
                    if (credentials.ContainsKey(key))
                    {
                        return credentials[key].Value;
                    }
                }
            }

            return null;
        }

        protected internal virtual bool GetBoolFromCredentials(Dictionary<string, Credential> credentials, string key)
        {
            var result = false;
            if (credentials != null && credentials.ContainsKey(key))
            {
                bool.TryParse(credentials[key].Value, out result);
            }

            return result;
        }

        protected internal virtual int GetIntFromCredentials(Dictionary<string, Credential> credentials, string key)
        {
            return GetIntFromCredentials(credentials, new List<string>() { key });
        }

        protected internal virtual int GetIntFromCredentials(Dictionary<string, Credential> credentials, List<string> keys)
        {
            var result = 0;

            if (credentials != null)
            {
                foreach (var key in keys)
                {
                    if (credentials.ContainsKey(key))
                    {
                        result = int.Parse(credentials[key].Value);
                    }
                }
            }

            return result;
        }

        protected internal virtual List<string> GetListFromCredentials(Dictionary<string, Credential> credentials, string key)
        {
            var result = new List<string>();
            if (credentials != null && credentials.ContainsKey(key))
            {
                var keyVal = credentials[key];
                if (keyVal.Count > 0)
                {
                    foreach (var kvp in keyVal)
                    {
                        if (kvp.Value.Count != 0 || string.IsNullOrEmpty(kvp.Value.Value))
                        {
                            throw new ConnectorException(string.Format("Unable to extract list from credentials: key={0}, value={1}/{2}", key, kvp.Key, kvp.Value));
                        }
                        else
                        {
                            result.Add(kvp.Value.Value);
                        }
                    }
                }
            }

            return result;
        }
    }
}
