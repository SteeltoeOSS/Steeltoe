// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Discovery.Kubernetes.Discovery;

public class Metadata
{
    /// <summary>
    /// Gets or sets a value indicating whether Kubernetes labels of the services will be included.
    /// </summary>
    public bool AddLabels { get; set; } = true;

    /// <summary>
    /// Gets or sets a value to use as a prefix for the keys in Metadata hashtable.
    /// </summary>
    public string LabelsPrefix { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the Kubernetes annotations of the services will be included.
    /// </summary>
    public bool AddAnnotations { get; set; } = true;

    /// <summary>
    /// Gets or sets a value to use as a prefix to the key names in the metadata hashtable.
    /// </summary>
    public string AnnotationsPrefix { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether named Kubernetes service ports will be included.
    /// </summary>
    public bool AddPorts { get; set; } = true;

    /// <summary>
    /// Gets or sets a value to use as a prefix to the keys on metadata entries for ports.
    /// </summary>
    public string PortsPrefix { get; set; } = "port.";
}
