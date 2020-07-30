// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Steeltoe.Discovery.Kubernetes.Discovery
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

            if (input.GetPort() != null && _kubernetesDiscoveryOptions.KnownSecurePorts.Contains(item: input.GetPort().Value))
            {
                return true;
            }

            return false;
        }
    }
}