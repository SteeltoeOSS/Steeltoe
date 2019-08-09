// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Consul;
using Steeltoe.Consul.Util;
using System;
using System.Net;

namespace Steeltoe.Consul.Client
{
    /// <summary>
    /// A factory to use in configuring and creating a Consul client
    /// </summary>
    public static class ConsulClientFactory
    {
        /// <summary>
        /// Create a Consul client using the provided configuration options
        /// </summary>
        /// <param name="options">the configuration options</param>
        /// <returns>a Consul client</returns>
        public static IConsulClient CreateClient(ConsulOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var client = new ConsulClient(s =>
            {
                s.Address = new Uri($"{options.Scheme}://{options.Host}:{options.Port}");
                s.Token = options.Token;
                s.Datacenter = options.Datacenter;
                if (!string.IsNullOrEmpty(options.WaitTime))
                {
                    s.WaitTime = DateTimeConversions.ToTimeSpan(options.WaitTime);
                }
                if (!string.IsNullOrEmpty(options.Password) || !string.IsNullOrEmpty(options.Username))
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    s.HttpAuth = new NetworkCredential(options.Username, options.Password);
#pragma warning restore CS0618 // Type or member is obsolete
                }
            });
            return client;
        }
    }
}
