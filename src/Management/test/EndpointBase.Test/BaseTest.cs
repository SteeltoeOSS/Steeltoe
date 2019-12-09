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

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Steeltoe.Common.Diagnostics;
using System;

namespace Steeltoe.Management.Endpoint.Test
{
    public class BaseTest : IDisposable
    {
        public virtual void Dispose()
        {
            DiagnosticsManager.Instance.Dispose();
        }

        public ILogger<T> GetLogger<T>()
        {
            var lf = new LoggerFactory();
            return lf.CreateLogger<T>();
        }

        public string Serialize<T>(T value)
        {
            return JsonConvert.SerializeObject(
                value,
                GetSerializerSettings());
        }

        public JsonSerializer GetSerializer()
        {
            return JsonSerializer.Create(GetSerializerSettings());
        }

        public JsonSerializerSettings GetSerializerSettings()
        {
            return new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() }
            };
        }
    }
}
