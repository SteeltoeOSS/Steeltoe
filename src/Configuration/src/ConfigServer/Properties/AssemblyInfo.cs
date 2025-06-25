// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Aspire;
using Steeltoe.Common.Certificates;
using Steeltoe.Common.Configuration;
using Steeltoe.Configuration.ConfigServer;

[assembly: ConfigurationSchema("Spring:Application", typeof(SpringApplicationSettings))]
[assembly: ConfigurationSchema("Spring:Cloud:Config", typeof(ConfigServerClientOptions))]
[assembly: ConfigurationSchema("Certificates:ConfigServer", typeof(CertificateSettings))]
[assembly: LoggingCategories("Steeltoe", "Steeltoe.Configuration", "Steeltoe.Configuration.ConfigServer")]

[assembly: InternalsVisibleTo("Steeltoe.Bootstrap.AutoConfiguration")]
[assembly: InternalsVisibleTo("Steeltoe.Bootstrap.AutoConfiguration.Test")]
[assembly: InternalsVisibleTo("Steeltoe.Configuration.ConfigServer.Test")]
[assembly: InternalsVisibleTo("Steeltoe.Configuration.ConfigServer.Discovery.Test")]
