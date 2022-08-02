// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Extensions.Configuration;

namespace Steeltoe.Connector.Services;

public class RedisServiceInfoFactory : ServiceInfoFactory
{
    public RedisServiceInfoFactory()
        : base(new Tags("redis"), new[]
        {
            RedisServiceInfo.RedisScheme,
            RedisServiceInfo.RedisSecureScheme
        })
    {
    }

    public override IServiceInfo Create(Service binding)
    {
        string uri = GetUriFromCredentials(binding.Credentials);

        if (string.IsNullOrEmpty(uri))
        {
            string host = GetHostFromCredentials(binding.Credentials);
            string password = GetPasswordFromCredentials(binding.Credentials);
            int port = GetPortFromCredentials(binding.Credentials);
            int tlsPort = GetTlsPortFromCredentials(binding.Credentials);
            bool tlsEnabled = tlsPort != 0;

            return new RedisServiceInfo(binding.Name, tlsEnabled ? RedisServiceInfo.RedisSecureScheme : RedisServiceInfo.RedisScheme, host,
                tlsEnabled ? tlsPort : port, password);
        }

        return new RedisServiceInfo(binding.Name, uri);
    }
}
