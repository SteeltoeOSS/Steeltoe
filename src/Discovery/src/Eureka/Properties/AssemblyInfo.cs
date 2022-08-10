// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Steeltoe.Discovery;
using Steeltoe.Discovery.Eureka;

[assembly: InternalsVisibleTo("Steeltoe.Discovery.Eureka.Test")]
[assembly: InternalsVisibleTo("Steeltoe.Discovery.Client.Test")]
[assembly: DiscoveryClientAssembly(typeof(EurekaDiscoveryClientExtension))]
