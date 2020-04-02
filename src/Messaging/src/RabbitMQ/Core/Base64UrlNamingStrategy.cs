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

using System;

namespace Steeltoe.Messaging.Rabbit.Core
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
