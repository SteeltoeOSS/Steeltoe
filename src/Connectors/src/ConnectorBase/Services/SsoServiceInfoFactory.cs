// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Extensions.Configuration;

namespace Steeltoe.Connector.Services;

public class SsoServiceInfoFactory : ServiceInfoFactory
{
    public SsoServiceInfoFactory()
        : base(new Tags("p-identity"), "uaa")
    {
    }

    public override IServiceInfo Create(Service binding)
    {
        var clientId = GetClientIdFromCredentials(binding.Credentials);
        var clientSecret = GetClientSecretFromCredentials(binding.Credentials);
        var authDomain = GetStringFromCredentials(binding.Credentials, "auth_domain");
        var uri = GetUriFromCredentials(binding.Credentials);

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
        if (uaaString.StartsWith("uaa:"))
        {
            return $"https:{uaaString.Substring(4)}";
        }

        return uaaString;
    }
}
