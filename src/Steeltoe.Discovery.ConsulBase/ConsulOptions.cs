// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Steeltoe.Discovery.Consul
{
    public class ConsulOptions
    {
        public string Host { get; set; } = "localhost";

        public string Scheme { get; set; } = "http";

        public int Port { get; set; } = 8500;

        public string Datacenter { get; set; }

        public string Token { get; set; }

        public string WaitTime { get; set; }

        public bool Enable { get; set; }
    }
}