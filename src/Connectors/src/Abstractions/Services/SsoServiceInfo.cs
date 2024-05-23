// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Connectors.Services;

internal sealed class SsoServiceInfo : ServiceInfo
{
    public string ClientId { get; }

    public string ClientSecret { get; }

    public string AuthDomain { get; }

    public SsoServiceInfo(string id, string clientId, string clientSecret, string domain)
        : base(id)
    {
        ClientId = clientId;
        ClientSecret = clientSecret;
        AuthDomain = domain;
    }
}
