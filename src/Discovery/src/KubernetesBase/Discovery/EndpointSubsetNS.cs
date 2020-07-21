// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s.Models;
using System.Collections.Generic;

namespace Steeltoe.Discovery.KubernetesBase.Discovery
{
    public class EndpointSubsetNs
    {
        public string Namespace { get; set; }

        public IList<V1EndpointSubset> EndpointSubsets { get; set; } = new List<V1EndpointSubset>();
    }
}