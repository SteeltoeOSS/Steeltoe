// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
