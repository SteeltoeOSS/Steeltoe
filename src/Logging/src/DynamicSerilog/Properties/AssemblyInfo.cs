// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Aspire;
using Steeltoe.Logging.DynamicSerilog;

[assembly: ConfigurationSchema("Serilog", typeof(SerilogOptions))]
[assembly: LoggingCategories("Steeltoe", "Steeltoe.Logging", "Steeltoe.Logging.DynamicSerilog")]

[assembly: InternalsVisibleTo("Steeltoe.Bootstrap.AutoConfiguration")]
[assembly: InternalsVisibleTo("Steeltoe.Logging.DynamicSerilog.Test")]
