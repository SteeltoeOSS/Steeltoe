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
    public class DefaultIsServicePortSecureResolver
    {
        private readonly List<string> _truthyStrings = new List<string>
        {
            "true",
            "on",
            "yes",
            "1"
        };

        private readonly KubernetesDiscoveryOptions _kubernetesDiscoveryOptions;

        public DefaultIsServicePortSecureResolver(KubernetesDiscoveryOptions kubernetesDiscoveryOptions)
        {
            _kubernetesDiscoveryOptions = kubernetesDiscoveryOptions;
        }

        public bool Resolve(Input input)
        {
            var securedLabelValue = input.GetServiceLabels().ContainsKey("secured") ? input.GetServiceLabels()["secured"] : "false";
            if (_truthyStrings.Contains(securedLabelValue))
            {
                return true;
            }

            var securedAnnotationValue = input.GetServiceAnnotations().ContainsKey("secured") ? input.GetServiceAnnotations()["secured"] : "false";
            if (_truthyStrings.Contains(securedAnnotationValue))
            {
                return true;
            }

            if (input.GetPort() != null && _kubernetesDiscoveryOptions.KnownSecurePorts.Contains(item: input.GetPort().Value ))
            {
                return true;
            }

            return false;
        }
    }
}