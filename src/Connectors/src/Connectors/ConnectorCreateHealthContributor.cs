// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Connectors;

public delegate IHealthContributor ConnectorCreateHealthContributor(IServiceProvider serviceProvider, string serviceBindingName);
