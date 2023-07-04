// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Steeltoe.Common;
using Steeltoe.Configuration;
using Steeltoe.Connectors.Services;

namespace Steeltoe.Connectors.CloudFoundry.Services;

[ServiceInfoFactory]
internal abstract class ServiceInfoFactory : IServiceInfoFactory
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

    private readonly Tags _serviceInfoTags;

    private readonly List<string> _uriKeys = new()
    {
        "uri",
        "url"
    };

    private readonly IEnumerable<string> _uriSchemes;

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

        _serviceInfoTags = tags;
        _uriSchemes = schemes;

        if (_uriSchemes != null)
        {
            foreach (string s in _uriSchemes)
            {
                _uriKeys.Add($"{s}uri");
                _uriKeys.Add($"{s}url");
            }
        }
    }

    public virtual bool Accepts(Service binding)
    {
        return TagsMatch(binding) || LabelStartsWithTag(binding) || UriMatchesScheme(binding) || UriKeyMatchesScheme(binding);
    }

    public abstract IServiceInfo Create(Service binding);

    protected internal bool TagsMatch(Service binding)
    {
        return _serviceInfoTags.ContainsOne(binding.Tags);
    }

    protected internal bool LabelStartsWithTag(Service binding)
    {
        return _serviceInfoTags.StartsWith(binding.Label);
    }

    protected internal bool UriMatchesScheme(Service binding)
    {
        if (_uriSchemes == null)
        {
            return false;
        }

        IDictionary<string, Credential> credentials = binding.Credentials;

        if (credentials == null)
        {
            return false;
        }

        string uri = GetStringFromCredentials(binding.Credentials, _uriKeys);

        if (uri != null)
        {
            foreach (string uriScheme in _uriSchemes)
            {
                if (uri.StartsWith($"{uriScheme}://", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool UriKeyMatchesScheme(Service binding)
    {
        if (_uriSchemes == null)
        {
            return false;
        }

        IDictionary<string, Credential> credentials = binding.Credentials;

        if (credentials == null)
        {
            return false;
        }

        foreach (string uriScheme in _uriSchemes)
        {
            if (credentials.ContainsKey($"{uriScheme}Uri") || credentials.ContainsKey($"{uriScheme}uri") || credentials.ContainsKey($"{uriScheme}Url") ||
                credentials.ContainsKey($"{uriScheme}url"))
            {
                return true;
            }
        }

        return false;
    }

    protected internal string GetUsernameFromCredentials(IDictionary<string, Credential> credentials)
    {
        return GetStringFromCredentials(credentials, UserList);
    }

    protected internal string GetPasswordFromCredentials(IDictionary<string, Credential> credentials)
    {
        return GetStringFromCredentials(credentials, PasswordList);
    }

    protected internal int GetPortFromCredentials(IDictionary<string, Credential> credentials)
    {
        return GetIntFromCredentials(credentials, "port");
    }

    protected internal string GetHostFromCredentials(IDictionary<string, Credential> credentials)
    {
        return GetStringFromCredentials(credentials, HostList);
    }

    protected internal string GetUriFromCredentials(IDictionary<string, Credential> credentials)
    {
        return GetStringFromCredentials(credentials, _uriKeys);
    }

    protected string GetClientIdFromCredentials(IDictionary<string, Credential> credentials)
    {
        return GetStringFromCredentials(credentials, "client_id");
    }

    protected string GetClientSecretFromCredentials(IDictionary<string, Credential> credentials)
    {
        return GetStringFromCredentials(credentials, "client_secret");
    }

    protected string GetAccessTokenUriFromCredentials(IDictionary<string, Credential> credentials)
    {
        return GetStringFromCredentials(credentials, "access_token_uri");
    }

    protected string GetStringFromCredentials(IDictionary<string, Credential> credentials, string key)
    {
        return GetStringFromCredentials(credentials, new List<string>
        {
            key
        });
    }

    private string GetStringFromCredentials(IDictionary<string, Credential> credentials, List<string> keys)
    {
        if (credentials != null)
        {
            foreach (string key in keys)
            {
                if (credentials.TryGetValue(key, out Credential credential))
                {
                    return credential.Value;
                }
            }
        }

        return null;
    }

    protected internal int GetIntFromCredentials(IDictionary<string, Credential> credentials, string key)
    {
        return GetIntFromCredentials(credentials, new List<string>
        {
            key
        });
    }

    private int GetIntFromCredentials(IDictionary<string, Credential> credentials, List<string> keys)
    {
        int result = 0;

        if (credentials != null)
        {
            foreach (string key in keys)
            {
                if (credentials.TryGetValue(key, out Credential credential))
                {
                    result = int.Parse(credential.Value, CultureInfo.InvariantCulture);
                }
            }
        }

        return result;
    }

    protected internal List<string> GetListFromCredentials(IDictionary<string, Credential> credentials, string key)
    {
        var result = new List<string>();

        if (credentials != null && credentials.TryGetValue(key, out Credential credential) && credential.Count > 0)
        {
            foreach (KeyValuePair<string, Credential> kvp in credential)
            {
                if (kvp.Value.Count != 0 || string.IsNullOrEmpty(kvp.Value.Value))
                {
                    throw new ConnectorException($"Unable to extract list from credentials: key={key}, value={kvp.Key}/{kvp.Value}");
                }

                result.Add(kvp.Value.Value);
            }
        }

        return result;
    }
}
