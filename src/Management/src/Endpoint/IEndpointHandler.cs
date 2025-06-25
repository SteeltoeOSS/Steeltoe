// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Configuration;

namespace Steeltoe.Management.Endpoint;

public interface IEndpointHandler<in TRequest, TResponse>
{
    EndpointOptions Options { get; }

    Task<TResponse> InvokeAsync(TRequest argument, CancellationToken cancellationToken);
}
