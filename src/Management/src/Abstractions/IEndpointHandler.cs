// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management;

public interface IEndpointHandler<in TArgument, TResult> 
{
    Task<TResult> InvokeAsync(TArgument argument, CancellationToken cancellationToken);

    HttpMiddlewareOptions Options { get; }

}
