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

namespace Steeltoe.Common.Discovery
{
    [Serializable]
    public class SerializableIServiceInstance : IServiceInstance
    {
        public SerializableIServiceInstance(IServiceInstance instance)
        {
            ServiceId = instance.ServiceId;
            Host = instance.Host;
            Port = instance.Port;
            IsSecure = instance.IsSecure;
            Uri = instance.Uri;
            Metadata = instance.Metadata;
        }

        public string ServiceId { get; set; }

        public string Host { get; set; }

        public int Port { get; set; }

        public bool IsSecure { get; set; }

        public Uri Uri { get; set; }

        public IDictionary<string, string> Metadata { get; set; }
    }
}
