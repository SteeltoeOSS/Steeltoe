// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Messaging.RabbitMQ.Core
{
    public class Base64UrlNamingStrategy : INamingStrategy
    {
        public static readonly Base64UrlNamingStrategy DEFAULT = new Base64UrlNamingStrategy();

        public Base64UrlNamingStrategy()
            : this("spring.gen-")
        {
        }

        public Base64UrlNamingStrategy(string prefix)
        {
            if (prefix == null)
            {
                throw new ArgumentNullException(nameof(prefix));
            }

            Prefix = prefix;
        }

        public string Prefix { get; }

        public string GenerateName()
        {
            var uuid = Guid.NewGuid();
            var converted = Convert.ToBase64String(uuid.ToByteArray())
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", string.Empty);
            return Prefix + converted;
        }
    }
}
