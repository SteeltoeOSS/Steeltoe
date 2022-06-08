// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Extensions.Configuration;
using System;

namespace Steeltoe.Connector.Services;

public class EurekaServiceInfoFactory : ServiceInfoFactory
{
    public EurekaServiceInfoFactory()
        : base(new Tags("eureka"), Array.Empty<string>())
    {
    }

    public override IServiceInfo Create(Service binding)
    {
        var uri = GetUriFromCredentials(binding.Credentials);
        var clientId = GetClientIdFromCredentials(binding.Credentials);
        var clientSecret = GetClientSecretFromCredentials(binding.Credentials);
        var accessTokenUri = GetAccessTokenUriFromCredentials(binding.Credentials);

        return new EurekaServiceInfo(binding.Name, uri, clientId, clientSecret, accessTokenUri);
    }
}
