// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Management;

public abstract class AbstractEndpoint : IEndpoint
{
    protected IEndpointOptions innerOptions;

    protected AbstractEndpoint(IEndpointOptions options)
    {
        this.innerOptions = options ?? throw new ArgumentNullException(nameof(options));
    }

    public virtual string Id => innerOptions.Id;

    public virtual bool Enabled => innerOptions.Enabled.Value;

    public virtual IEndpointOptions Options => innerOptions;

    public string Path => innerOptions.Path;
}

/// <summary>
/// Base class for management endpoints.
/// </summary>
/// <typeparam name="TResult">Type of response returned from calls to this endpoint.</typeparam>
public abstract class AbstractEndpoint<TResult> : AbstractEndpoint, IEndpoint<TResult>
{
    protected AbstractEndpoint(IEndpointOptions options)
        : base(options)
    {
    }

    public virtual TResult Invoke()
    {
        return default;
    }
}

/// <summary>
/// Base class for endpoints that allow POST requests.
/// </summary>
/// <typeparam name="TResult">Type of response returned from calls to this endpoint.</typeparam>
/// <typeparam name="TRequest">Type of request that can be passed to this endpoint.</typeparam>
public abstract class AbstractEndpoint<TResult, TRequest> : AbstractEndpoint, IEndpoint<TResult, TRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AbstractEndpoint{TResult, TRequest}"/> class.
    /// </summary>
    /// <param name="options">Endpoint configuration options.</param>
    protected AbstractEndpoint(IEndpointOptions options)
        : base(options)
    {
    }

    public virtual TResult Invoke(TRequest arg)
    {
        return default;
    }
}
