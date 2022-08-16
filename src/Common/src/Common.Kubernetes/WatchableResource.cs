// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Kubernetes;

public class WatchableResource
{
    public bool Enabled { get; set; } = true;

    public IList<NamespacedResource> Sources { get; set; } = new List<NamespacedResource>();
}
