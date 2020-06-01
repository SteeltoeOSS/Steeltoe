// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.CloudFoundry.Connector.Services
{
    public class RedisServiceInfo : UriServiceInfo
    {
        public const string REDIS_SCHEME = "redis";
        public const string REDIS_SECURE_SCHEME = "rediss";

        public RedisServiceInfo(string id, string scheme, string host, int port, string password)
            : base(id, scheme, host, port, null, password, null)
        {
        }

        public RedisServiceInfo(string id, string uri)
            : base(id, uri)
        {
        }
    }
}
