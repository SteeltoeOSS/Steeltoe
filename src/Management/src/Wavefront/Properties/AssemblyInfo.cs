// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Aspire;
using Steeltoe.Management.Wavefront.Exporters;

[assembly: ConfigurationSchema("Management:Metrics:Export:Wavefront", typeof(WavefrontExporterOptions))]
[assembly: ConfigurationSchema("Wavefront:Application", typeof(WavefrontApplicationOptions))]
[assembly: LoggingCategories("Steeltoe", "Steeltoe.Management", "Steeltoe.Management.Wavefront")]

[assembly: InternalsVisibleTo("Steeltoe.Bootstrap.AutoConfiguration")]
[assembly: InternalsVisibleTo("Steeltoe.Management.Tracing")]
