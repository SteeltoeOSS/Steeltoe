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

namespace Steeltoe.Discovery.KubernetesBase.Discovery
{
    public class Metadata
    {
        // When set, the Kubernetes labels of the services will be included
        // as metdata of the returned service
        public bool AddLabels { get; set; } = true;

        // When AddLabels is set, then this will be used as a prefix to
        // the key names in Metadata hashtable
        public string LabelsPrefix { get; set; }
        
        // When set, the Kuberentes annotations of the services will be
        // included as metadata
        public bool AddAnnotations { get; set; } = true;

        // When AddAnnotations is set, then this will be used as a prefix
        // to the key names in the metadata hashtable
        public string AnnotationsPrefix { get; set; }
        
        // When set, any named Kubernetes service ports will be included
        // as metadata
        public bool AddPorts { get; set; } = true;
        
        // When AddPorts is set, then this will be used as a prefix to the
        // key names in the metadata hashtable
        public string PortsPrefix { get; set; } = "port.";
    }
}
