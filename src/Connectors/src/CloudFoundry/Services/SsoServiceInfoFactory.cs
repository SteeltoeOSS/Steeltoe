// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Configuration;
using Steeltoe.Connectors.Services;

namespace Steeltoe.Connectors.CloudFoundry.Services;

internal sealed class SsoServiceInfoFactory : ServiceInfoFactory
{
    public SsoServiceInfoFactory()
        : base(new Tags("p-identity"), "uaa")
    {
    }

    public override IServiceInfo Create(Service binding)
    {
        string clientId = GetClientIdFromCredentials(binding.Credentials);
        string clientSecret = GetClientSecretFromCredentials(binding.Credentials);
        string authDomain = GetStringFromCredentials(binding.Credentials, "auth_domain");
        string uri = GetUriFromCredentials(binding.Credentials);

        if (!string.IsNullOrEmpty(authDomain))
        {
            return new SsoServiceInfo(binding.Name, clientId, clientSecret, authDomain);
        }

        if (!string.IsNullOrEmpty(uri))
        {
            return new SsoServiceInfo(binding.Name, clientId, clientSecret, UpdateUaaScheme(uri));
        }

        return null;
    }

    internal string UpdateUaaScheme(string uaaString)
    {
        if (uaaString.StartsWith("uaa:", StringComparison.Ordinal))
        {
            return $"https:{uaaString.Substring(4)}";
        }

        return uaaString;
    }
}
