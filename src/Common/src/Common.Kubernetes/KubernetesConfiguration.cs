// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Steeltoe.Common.Kubernetes
{
    public class KubernetesConfiguration : WatchableResource
    {
        public List<string> Paths { get; set; } = new List<string>();
    }
}
