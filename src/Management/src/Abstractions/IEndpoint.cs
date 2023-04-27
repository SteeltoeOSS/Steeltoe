// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management;

public interface IEndpoint
{
    IEndpointOptions Options { get; }
}

public interface IEndpoint<TResult> : IEndpoint
{
    Task<TResult> InvokeAsync(CancellationToken cancellationToken);
}

public interface IEndpoint<TResult, in TRequest> : IEndpoint
{
    Task<TResult> InvokeAsync(CancellationToken cancellationToken, TRequest arg);
}
