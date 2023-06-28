// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Env;

namespace Steeltoe.Management.Endpoint.Environment;

#pragma warning disable S4023 // Interfaces should not be empty
public interface IEnvironmentEndpointHandler : IEndpointHandler<object, EnvironmentDescriptor>
#pragma warning restore S4023 // Interfaces should not be empty
{
}
