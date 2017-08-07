//
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

using Microsoft.Extensions.Configuration;


namespace Steeltoe.Management.Endpoint.Trace
{
    public class TraceOptions : AbstractOptions, ITraceOptions
    {
        private const string MANAGEMENT_INFO_PREFIX = "management:endpoints:trace";
        private const int DEFAULT_CAPACITY = 100;
        public TraceOptions() : base()
        {
            Id = "trace";
            Capacity = DEFAULT_CAPACITY;
        }

        public TraceOptions(IConfiguration config) :
             base(MANAGEMENT_INFO_PREFIX, config)
        {
            if (string.IsNullOrEmpty(Id))
            {
                Id = "trace";
            }
            if (Capacity == -1)
            {
                Capacity = DEFAULT_CAPACITY;
            }
        }

        public int Capacity { get; set; } = -1;
        public bool AddRequestHeaders { get; set; } = true;
        public bool AddResponseHeaders { get; set; } = true;
        public bool AddPathInfo { get; set; } = false;
        public bool AddUserPrincipal { get; set; } = false;
        public bool AddParameters { get; set; } = false;
        public bool AddQueryString { get; set; } = false;
        public bool AddAuthType { get; set; } = false;
        public bool AddRemoteAddress { get; set; } = false;
        public bool AddSessionId { get; set; } = false;
        public bool AddTimeTaken { get; set; } = true;
    }
}
