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

using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace Steeltoe.Connector
{
    public interface IConnectionInfo
    {
        Connection Get(IConfiguration configuration, string serviceName);
    }

    public class Connection
    {
        public string ConnectionString { get; set; }

        public string Name { get; set; }

        public Dictionary<string, string> Properties { get; } = new Dictionary<string, string>();
    }
}