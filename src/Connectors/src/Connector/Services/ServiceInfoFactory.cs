// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;
using Steeltoe.Extensions.Configuration;

namespace Steeltoe.Connector.Services;

[ServiceInfoFactory]
public abstract class ServiceInfoFactory : IServiceInfoFactory
{
    private static readonly List<string> UserList = new()
    {
        "user",
        "username",
        "uid"
    };

    private static readonly List<string> PasswordList = new()
    {
        "password",
        "pw"
    };

    private static readonly List<string> HostList = new()
    {
        "hostname",
        "host"
    };

    protected Tags ServiceInfoTags { get; set; }

    protected List<string> UriKeys { get; set; } = new()
    {
        "uri",
        "url"
    };

    protected IEnumerable<string> UriSchemes { get; set; }

    public virtual string DefaultUriScheme => UriSchemes?.Any() == true ? UriSchemes.First() : null;

    protected ServiceInfoFactory(Tags tags, string scheme)
        : this(tags, new List<string>
        {
            scheme
        })
    {
        ArgumentGuard.NotNullOrEmpty(scheme);
    }

    protected ServiceInfoFactory(Tags tags, IEnumerable<string> schemes)
    {
        ArgumentGuard.NotNull(tags);

        ServiceInfoTags = tags;
        UriSchemes = schemes;

        if (schemes != null)
        {
            foreach (string s in schemes)
            {
                UriKeys.Add($"{s}uri");
                UriKeys.Add($"{s}url");
            }
        }
    }

    public virtual bool Accepts(Service binding)
    {
        return TagsMatch(binding) || LabelStartsWithTag(binding) || UriMatchesScheme(binding) || UriKeyMatchesScheme(binding);
    }

    public abstract IServiceInfo Create(Service binding);

    protected internal virtual bool TagsMatch(Service binding)
    {
        return ServiceInfoTags.ContainsOne(binding.Tags);
    }

    protected internal virtual bool LabelStartsWithTag(Service binding)
    {
        return ServiceInfoTags.StartsWith(binding.Label);
    }

    protected internal virtual bool UriMatchesScheme(Service binding)
    {
        if (UriSchemes == null)
        {
            return false;
        }

        Dictionary<string, Credential> credentials = binding.Credentials;

        if (credentials == null)
        {
            return false;
        }

        string uri = GetStringFromCredentials(binding.Credentials, UriKeys);

        if (uri != null)
        {
            foreach (string uriScheme in UriSchemes)
            {
                if (uri.StartsWith($"{uriScheme}://", StringComparison.OrdinalIgnoreCase))
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

        Dictionary<string, Credential> credentials = binding.Credentials;

        if (credentials == null)
        {
            return false;
        }

        foreach (string uriScheme in UriSchemes)
        {
            if (credentials.ContainsKey($"{uriScheme}Uri") || credentials.ContainsKey($"{uriScheme}uri") || credentials.ContainsKey($"{uriScheme}Url") ||
                credentials.ContainsKey($"{uriScheme}url"))
            {
                return true;
            }
        }

        return false;
    }

    protected internal virtual string GetUsernameFromCredentials(Dictionary<string, Credential> credentials)
    {
        return GetStringFromCredentials(credentials, UserList);
    }

    protected internal virtual string GetPasswordFromCredentials(Dictionary<string, Credential> credentials)
    {
        return GetStringFromCredentials(credentials, PasswordList);
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
        return GetStringFromCredentials(credentials, HostList);
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
        return GetStringFromCredentials(credentials, new List<string>
        {
            key
        });
    }

    protected internal virtual string GetStringFromCredentials(Dictionary<string, Credential> credentials, List<string> keys)
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

    protected internal virtual bool GetBoolFromCredentials(Dictionary<string, Credential> credentials, string key)
    {
        bool result = false;

        if (credentials != null && credentials.ContainsKey(key))
        {
            bool.TryParse(credentials[key].Value, out result);
        }

        return result;
    }

    protected internal virtual int GetIntFromCredentials(Dictionary<string, Credential> credentials, string key)
    {
        return GetIntFromCredentials(credentials, new List<string>
        {
            key
        });
    }

    protected internal virtual int GetIntFromCredentials(Dictionary<string, Credential> credentials, List<string> keys)
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

    protected internal virtual List<string> GetListFromCredentials(Dictionary<string, Credential> credentials, string key)
    {
        var result = new List<string>();

        if (credentials != null && credentials.ContainsKey(key))
        {
            Credential keyVal = credentials[key];

            if (keyVal.Count > 0)
            {
                foreach (KeyValuePair<string, Credential> kvp in keyVal)
                {
                    if (kvp.Value.Count != 0 || string.IsNullOrEmpty(kvp.Value.Value))
                    {
                        throw new ConnectorException($"Unable to extract list from credentials: key={key}, value={kvp.Key}/{kvp.Value}");
                    }

                    result.Add(kvp.Value.Value);
                }
            }
        }

        return result;
    }
}
