// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Aspire;
using Steeltoe.Discovery.Configuration;

[assembly: ConfigurationSchema("Discovery", typeof(ConfigurationDiscoveryOptions))]
[assembly: LoggingCategories("Steeltoe", "Steeltoe.Discovery", "Steeltoe.Discovery.Configuration")]
