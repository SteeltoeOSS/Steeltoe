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
using k8s.Models;

namespace Steeltoe.Discovery.KubernetesBase.Discovery
{
    public class EndpointSubsetNs
    {
        public string Namespace { get; set; }
        public IList<V1EndpointSubset> EndpointSubsets { get; set; } = new List<V1EndpointSubset>();
    }
}