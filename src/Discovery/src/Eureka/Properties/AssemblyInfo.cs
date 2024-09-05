// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Aspire;
using Steeltoe.Common.Certificates;
using Steeltoe.Common.Configuration;
using Steeltoe.Common.Net;
using Steeltoe.Discovery.Eureka;
using Steeltoe.Discovery.Eureka.Configuration;

[assembly: ConfigurationSchema("Spring:Application", typeof(SpringApplicationSettings))]
[assembly: ConfigurationSchema("Spring:Cloud:Discovery", typeof(SpringDiscoverySettings))]
[assembly: ConfigurationSchema("Spring:Cloud:Inet", typeof(InetOptions))]
[assembly: ConfigurationSchema("Certificates:Eureka", typeof(CertificateSettings))]
[assembly: ConfigurationSchema("Eureka:Client", typeof(EurekaClientOptions))]
[assembly: ConfigurationSchema("Eureka:Instance", typeof(EurekaInstanceOptions))]
[assembly: LoggingCategories("Steeltoe", "Steeltoe.Discovery", "Steeltoe.Discovery.Eureka")]

[assembly: InternalsVisibleTo("Steeltoe.Discovery.Eureka.Test")]
[assembly: InternalsVisibleTo("Steeltoe.Discovery.Eureka.CloudFoundry.Test")]
[assembly: InternalsVisibleTo("Steeltoe.Discovery.HttpClients.Test")]
