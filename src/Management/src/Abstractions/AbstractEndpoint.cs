// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Management
{
    public abstract class AbstractEndpoint : IEndpoint
    {
        protected IEndpointOptions options;

        public AbstractEndpoint(IEndpointOptions options)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public virtual string Id => options.Id;

        public virtual bool Enabled => options.Enabled.Value;

        public virtual IEndpointOptions Options => options;

        public string Path => options.Path;
    }

#pragma warning disable SA1402 // File may only contain a single class
    /// <summary>
    /// Base class for management endpoints
    /// </summary>
    /// <typeparam name="TResult">Type of response returned from calls to this endpoint</typeparam>
    public abstract class AbstractEndpoint<TResult> : AbstractEndpoint, IEndpoint<TResult>
    {
        public AbstractEndpoint(IEndpointOptions options)
            : base(options)
        {
        }

        public virtual TResult Invoke()
        {
            return default;
        }
    }

    /// <summary>
    /// Base class for endpoints that allow POST requests
    /// </summary>
    /// <typeparam name="TResult">Type of response returned from calls to this endpoint</typeparam>
    /// <typeparam name="TRequest">Type of request that can be passed to this endpoint</typeparam>
    public abstract class AbstractEndpoint<TResult, TRequest> : AbstractEndpoint, IEndpoint<TResult, TRequest>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractEndpoint{TResult, TRequest}"/> class.
        /// </summary>
        /// <param name="options">Endpoint configuration options</param>
        public AbstractEndpoint(IEndpointOptions options)
            : base(options)
        {
        }

        public virtual TResult Invoke(TRequest arg)
        {
            return default;
        }
    }
#pragma warning restore SA1402 // File may only contain a single class
}
