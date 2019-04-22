//
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


namespace Steeltoe.Management.Endpoint
{
    public abstract class AbstractEndpoint : IEndpoint
    {
        protected IEndpointOptions options;

        public AbstractEndpoint(IEndpointOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            this.options = options;
        }
        public virtual string Id { get { return options.Id; } }

        public virtual bool Enabled { get { return options.Enabled.Value; } }

        public virtual bool Sensitive { get { return options.Sensitive.Value; } }

        public virtual IEndpointOptions Options { get { return options; } }

        public string Path { get { return options.Path; } }
    }

    public abstract class AbstractEndpoint<TResult> : AbstractEndpoint, IEndpoint<TResult>
    {
        public AbstractEndpoint(IEndpointOptions options) : base(options)
        {
        }

        public virtual TResult Invoke()
        {
            return default(TResult);
        }
    }

    public abstract class AbstractEndpoint<TResult, TRequest> : AbstractEndpoint, IEndpoint<TResult, TRequest>
    {
        public AbstractEndpoint(IEndpointOptions options) : base(options)
        {
        }

        public virtual TResult Invoke(TRequest arg)
        {
            return default(TResult);
        }
    }
}
