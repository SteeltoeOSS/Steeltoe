// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

#pragma warning disable S1135 // Track uses of "TODO" tags
// TODO: [assembly: InternalsVisibleTo("Steeltoe.Management.MetricCollectors.Test")]
[assembly: InternalsVisibleTo("Steeltoe.Management.Tracing.Test")]
#pragma warning restore S1135 // Track uses of "TODO" tags
[assembly: InternalsVisibleTo("Steeltoe.Management.Endpoint")]
[assembly: InternalsVisibleTo("Steeltoe.Management.Endpoint.Test")]
