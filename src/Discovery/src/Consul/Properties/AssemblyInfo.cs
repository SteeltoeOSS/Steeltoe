// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Aspire;
using Steeltoe.Discovery.Consul.Configuration;

[assembly: ConfigurationSchema("Consul", typeof(ConsulOptions))]
[assembly: ConfigurationSchema("Consul:Discovery", typeof(ConsulDiscoveryOptions))]
[assembly: LoggingCategories("Steeltoe", "Steeltoe.Discovery", "Steeltoe.Discovery.Consul")]

[assembly: InternalsVisibleTo("Steeltoe.Discovery.Consul.Test")]
