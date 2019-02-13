// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Steeltoe.Management.Endpoint
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

        [Obsolete]
        public virtual bool Sensitive => options.Sensitive.Value;

        public virtual IEndpointOptions Options => options;

        public string Path => options.Path;

        private List<string> _otherPaths;

        public List<string> OtherPaths
        {
            get
            {
                if (_otherPaths == null)
                {
                    _otherPaths = new List<string> { Path };
                }

                return _otherPaths;
            }
        }
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
            return default(TResult);
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
            return default(TResult);
        }
    }
#pragma warning restore SA1402 // File may only contain a single class
}
