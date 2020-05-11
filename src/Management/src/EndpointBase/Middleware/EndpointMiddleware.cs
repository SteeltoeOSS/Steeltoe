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
        protected ILogger _logger;
        protected IEnumerable<HttpMethod> _allowedMethods;
        protected bool _exactRequestPathMatching;
        protected IList<IManagementOptions> _mgmtOptions;

        public EndpointMiddleware(IEnumerable<IManagementOptions> mgmtOptions, IEnumerable<HttpMethod> allowedMethods = null, bool exactRequestPathMatching = true, ILogger logger = null)
        {
            _allowedMethods = allowedMethods ?? new List<HttpMethod> { HttpMethod.Get };
            _exactRequestPathMatching = exactRequestPathMatching;
            _logger = logger;

            if (mgmtOptions == null)
            {
                throw new ArgumentNullException(nameof(mgmtOptions));
            }

            var mOptions = mgmtOptions.ToList();
            _mgmtOptions = mOptions.Count > 0 ? mOptions : null;
        }

        public EndpointMiddleware(IEndpoint<TResult> endpoint, IEnumerable<IManagementOptions> mgmtOptions, IEnumerable<HttpMethod> allowedMethods = null, bool exactRequestPathMatching = true, ILogger logger = null)
        {
            _allowedMethods = allowedMethods ?? new List<HttpMethod> { HttpMethod.Get };
            _exactRequestPathMatching = exactRequestPathMatching;
            _logger = logger;
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));

            if (mgmtOptions == null)
            {
                throw new ArgumentNullException(nameof(mgmtOptions));
            }

            var mOptions = mgmtOptions.ToList();
            _mgmtOptions = mOptions.Count > 0 ? mOptions : null;
        }

        protected IEndpoint<TResult> Endpoint { get; set; }

        public virtual string HandleRequest()
        {
            var result = Endpoint.Invoke();
            return Serialize(result);
        }

        public virtual bool RequestVerbAndPathMatch(string httpMethod, string requestPath)
        {
            _logger?.LogDebug($"endpoint: {Endpoint.Id}, httpMethod:  {httpMethod}, requestPath: {requestPath}, contextPaths: {string.Join(",", _mgmtOptions?.Select(x => x.Path))}");

            return Endpoint.RequestVerbAndPathMatch(httpMethod, requestPath, _allowedMethods, _mgmtOptions, _exactRequestPathMatching);
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
        protected new IEndpoint<TResult, TRequest> Endpoint { get; set; }

        public EndpointMiddleware(IEndpoint<TResult, TRequest> endpoint, IEnumerable<IManagementOptions> mgmtOptions, IEnumerable<HttpMethod> allowedMethods = null, bool exactRequestPathMatching = true, ILogger logger = null)
            : base(mgmtOptions, allowedMethods, exactRequestPathMatching, logger)
        {
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        }

        public EndpointMiddleware(IEnumerable<IManagementOptions> mgmtOptions, IEnumerable<HttpMethod> allowedMethods = null, bool exactRequestPathMatching = true, ILogger logger = null)
          : base(mgmtOptions, allowedMethods, exactRequestPathMatching, logger)
        {
        }

        public virtual string HandleRequest(TRequest arg)
        {
            var result = Endpoint.Invoke(arg);
            return Serialize(result);
        }

        public override bool RequestVerbAndPathMatch(string httpMethod, string requestPath)
        {
            var result = Endpoint.RequestVerbAndPathMatch(httpMethod, requestPath, _allowedMethods, _mgmtOptions, _exactRequestPathMatching);
            _logger?.LogDebug($"endpoint: {Endpoint.Id}, httpMethod:  {httpMethod}, requestPath: {requestPath}, contextPaths: {string.Join(",", _mgmtOptions?.Select(x => x.Path))}, result: {result}");
            return result;
        }
    }
#pragma warning restore SA1402 // File may only contain a single class
}
