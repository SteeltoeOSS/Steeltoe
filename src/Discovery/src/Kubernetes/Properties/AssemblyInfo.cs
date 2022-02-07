// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery;
using Steeltoe.Discovery.Kubernetes;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Steeltoe.Discovery.Kubernetes.Test")]

[assembly: DiscoveryClientAssembly(typeof(KubernetesDiscoveryClientExtension))]