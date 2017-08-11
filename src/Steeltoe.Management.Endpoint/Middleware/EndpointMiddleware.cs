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

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;

namespace Steeltoe.Management.Endpoint.Middleware
{
    public class EndpointMiddleware<TResult>
    {
        protected IEndpoint<TResult> endpoint;
        protected ILogger logger;

        public EndpointMiddleware(ILogger logger)
        {
            this.logger = logger;
        }

        public EndpointMiddleware(IEndpoint<TResult> endpoint, ILogger logger) 
            : this(logger)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            this.endpoint = endpoint;
        }

        public virtual string HandleRequest()
        {
            var result = endpoint.Invoke();
            return Serialize(result);
        }

        protected virtual string Serialize(TResult result)
        {
            try
            {
                return JsonConvert.SerializeObject(result, 
                    new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore }
                    );
            } catch (Exception e)
            {
                logger?.LogError("Error {0} serializaing {1}", e, result);
            }
            return string.Empty;
        }

    }

    public class EndpointMiddleware<TResult, TRequest> : EndpointMiddleware<TResult>
    {
        protected new IEndpoint<TResult, TRequest> endpoint;

        public EndpointMiddleware(IEndpoint<TResult, TRequest> endpoint, ILogger logger) 
            : base(logger)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            this.endpoint = endpoint;
        }

        public virtual string HandleRequest(TRequest arg)
        {
            var result = endpoint.Invoke(arg);
            return Serialize(result);
        }
    }

}
