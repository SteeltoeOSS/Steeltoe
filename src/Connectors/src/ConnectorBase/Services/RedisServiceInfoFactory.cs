﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Extensions.Configuration;

namespace Steeltoe.Connector.Services
{
    public class RedisServiceInfoFactory : ServiceInfoFactory
    {
        public RedisServiceInfoFactory()
            : base(new Tags("redis"), new string[] { RedisServiceInfo.REDIS_SCHEME, RedisServiceInfo.REDIS_SECURE_SCHEME })
        {
        }

        public override IServiceInfo Create(Service binding)
        {
            var uri = GetUriFromCredentials(binding.Credentials);
            if (string.IsNullOrEmpty(uri))
            {
                var host = GetHostFromCredentials(binding.Credentials);
                var password = GetPasswordFromCredentials(binding.Credentials);
                var port = GetPortFromCredentials(binding.Credentials);
                var tlsPort = GetTlsPortFromCredentials(binding.Credentials);
                var tlsEnabled = tlsPort != 0;

                return new RedisServiceInfo(
                        binding.Name,
                        tlsEnabled ? RedisServiceInfo.REDIS_SECURE_SCHEME : RedisServiceInfo.REDIS_SCHEME,
                        host,
                        tlsEnabled ? tlsPort : port,
                        password);
            }
            else
            {
                return new RedisServiceInfo(binding.Name, uri);
            }
        }
    }
}
