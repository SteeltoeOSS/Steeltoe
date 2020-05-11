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

using System.Collections.Generic;

namespace Steeltoe.Discovery.KubernetesBase.Discovery
{
    public class Input
    {
        private readonly int? _port;
        private readonly string _serviceName;
        private readonly IDictionary<string, string> _serviceLabels;
        private readonly IDictionary<string, string> _serviceAnnotations;

        public Input(string serviceName, int? port = null,
            IDictionary<string, string> serviceLabels = null, IDictionary<string, string> serviceAnnotations = null)
        {
            _port = port;
            _serviceName = serviceName;
            _serviceLabels = serviceLabels ?? new Dictionary<string, string>();
            _serviceAnnotations = serviceAnnotations ?? new Dictionary<string, string>();
        }

        public string GetServiceName()
        {
            return _serviceName;
        }

        public IDictionary<string, string> GetServiceLabels()
        {
            return _serviceLabels;
        }

        public IDictionary<string, string> GetServiceAnnotations()
        {
            return _serviceAnnotations;
        }
        
        public int? GetPort()
        {
            return _port;
        }
    }
}