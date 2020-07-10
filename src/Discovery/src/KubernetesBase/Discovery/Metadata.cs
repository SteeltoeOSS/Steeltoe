// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
