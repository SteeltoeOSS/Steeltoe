// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Aspire;
using Steeltoe.Common.Certificates;

[assembly: ConfigurationSchema("Certificates", typeof(CertificateSettings))]
[assembly: LoggingCategories("Steeltoe", "Steeltoe.Common", "Steeltoe.Common.Certificates")]

[assembly: InternalsVisibleTo("Steeltoe.Common.Certificates.Test")]
[assembly: InternalsVisibleTo("Steeltoe.Configuration.ConfigServer")]
[assembly: InternalsVisibleTo("Steeltoe.Discovery.Eureka")]
[assembly: InternalsVisibleTo("Steeltoe.Security.Authorization.Certificate")]
[assembly: InternalsVisibleTo("Steeltoe.Security.Authorization.Certificate.Test")]
