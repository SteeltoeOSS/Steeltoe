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
using System.Collections.Generic;

namespace Steeltoe.Common.Net
{
    public class InetOptions
    {
        public const string PREFIX = "spring:cloud:inet";

        public string DefaultHostname { get; set; } = "localhost";

        public string DefaultIpAddress { get; set; } = "127.0.0.1";

        public string IgnoredInterfaces { get; set; }

        public bool UseOnlySiteLocalInterfaces { get; set; } = false;

        public string PreferredNetworks { get; set; }

        internal IEnumerable<string> GetIgnoredInterfaces()
        {
            if (string.IsNullOrEmpty(IgnoredInterfaces))
            {
                return new List<string>();
            }

            return IgnoredInterfaces.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        internal IEnumerable<string> GetPreferredNetworks()
        {
            if (string.IsNullOrEmpty(PreferredNetworks))
            {
                return null;
            }

            return PreferredNetworks.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
