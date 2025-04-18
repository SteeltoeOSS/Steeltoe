// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Actuators.RouteMappings.ResponseTypes;

namespace Steeltoe.Management.Endpoint.Actuators.RouteMappings;

public interface IRouteMappingsEndpointHandler : IEndpointHandler<object?, RouteMappingsResponse>;
