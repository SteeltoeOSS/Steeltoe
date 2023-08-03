// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.HeapDump;

#pragma warning disable S4023 // Interfaces should not be empty
public interface IHeapDumpEndpointHandler : IEndpointHandler<object?, string?>
#pragma warning restore S4023 // Interfaces should not be empty
{
}
