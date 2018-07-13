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
using Newtonsoft.Json.Serialization;
using Steeltoe.Management.Endpoint.Health;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Steeltoe.Management.Endpoint.Middleware
{
    public class EndpointMiddleware<TResult>
    {
        protected IEndpoint<TResult> _endpoint;
        protected ILogger _logger;
        protected IEnumerable<HttpMethod> _allowedMethods;
        protected bool _exactRequestPathMatching;

        public EndpointMiddleware(IEnumerable<HttpMethod> allowedMethods = null, bool exactRequestPathMatching = true, ILogger logger = null)
        {
            _allowedMethods = allowedMethods ?? new List<HttpMethod> { HttpMethod.Get };
            _exactRequestPathMatching = exactRequestPathMatching;
            _logger = logger;
        }

        public EndpointMiddleware(IEndpoint<TResult> endpoint, IEnumerable<HttpMethod> allowedMethods = null, bool exactRequestPathMatching = true, ILogger logger = null)
            : this(allowedMethods, exactRequestPathMatching, logger)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        }

        internal IEndpoint<TResult> Endpoint
        {
            get
            {
                return _endpoint;
            }

            set
            {
                _endpoint = value;
            }
        }

        public virtual string HandleRequest()
        {
            var result = _endpoint.Invoke();
            return Serialize(result);
        }

        public virtual bool RequestVerbAndPathMatch(string httpMethod, string requestPath)
        {
            return _exactRequestPathMatching
                ? requestPath.Equals(_endpoint.Path) && _allowedMethods.Any(m => m.Method.Equals(httpMethod))
                : requestPath.StartsWith(_endpoint.Path) && _allowedMethods.Any(m => m.Method.Equals(httpMethod));
        }

        protected virtual string Serialize(TResult result)
        {
            try
            {
                var serializerSettings = new JsonSerializerSettings()
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() }
                };
                serializerSettings.Converters.Add(new HealthJsonConverter());

                return JsonConvert.SerializeObject(result, serializerSettings);
            }
            catch (Exception e)
            {
                _logger?.LogError("Error {Exception} serializing {MiddlewareResponse}", e, result);
            }

            return string.Empty;
        }
    }

#pragma warning disable SA1402 // File may only contain a single class
    public class EndpointMiddleware<TResult, TRequest> : EndpointMiddleware<TResult>
    {
        protected new IEndpoint<TResult, TRequest> _endpoint;

        public EndpointMiddleware(IEndpoint<TResult, TRequest> endpoint, IEnumerable<HttpMethod> allowedMethods = null, bool exactRequestPathMatching = true, ILogger logger = null)
            : base(allowedMethods, exactRequestPathMatching, logger)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        }

        public virtual string HandleRequest(TRequest arg)
        {
            var result = _endpoint.Invoke(arg);
            return Serialize(result);
        }

        public override bool RequestVerbAndPathMatch(string httpMethod, string requestPath)
        {
            return _exactRequestPathMatching
                ? requestPath.Equals(_endpoint.Path) && _allowedMethods.Any(m => m.Method.Equals(httpMethod))
                : requestPath.StartsWith(_endpoint.Path) && _allowedMethods.Any(m => m.Method.Equals(httpMethod));
        }
    }
#pragma warning restore SA1402 // File may only contain a single class
}
