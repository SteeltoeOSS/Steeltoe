﻿// Copyright 2017 the original author or authors.
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
using Microsoft.Owin;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Health;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Steeltoe.Management.EndpointOwin
{
    public class EndpointOwinMiddleware<TResult> : OwinMiddleware
    {
        protected IEndpoint<TResult> _endpoint;
        protected ILogger _logger;
        protected IEnumerable<HttpMethod> _allowedMethods;
        protected bool _exactRequestPathMatching;
        protected IEnumerable<IManagementOptions> _mgmtOptions;

        public EndpointOwinMiddleware(OwinMiddleware next, IEnumerable<IManagementOptions> mgmtOptions, IEnumerable<HttpMethod> allowedMethods = null, bool exactRequestPathMatching = true, ILogger logger = null)
            : base(next)
        {
            _allowedMethods = allowedMethods ?? new List<HttpMethod> { HttpMethod.Get };

            if (!_allowedMethods.Any())
            {
                _allowedMethods = new List<HttpMethod> { HttpMethod.Get };
            }

            _exactRequestPathMatching = exactRequestPathMatching;
            _logger = logger;
            _mgmtOptions = mgmtOptions;
        }

        public EndpointOwinMiddleware(OwinMiddleware next, IEndpoint<TResult> endpoint, IEnumerable<IManagementOptions> mgmtOptions, IEnumerable<HttpMethod> allowedMethods = null, bool exactRequestPathMatching = true, ILogger logger = null)
            : this(next, mgmtOptions, allowedMethods, exactRequestPathMatching, logger)
        {
            _endpoint = endpoint;
        }

        [Obsolete("Use newer constructor that passes in IManagementOptions instead")]
        public EndpointOwinMiddleware(OwinMiddleware next, IEnumerable<HttpMethod> allowedMethods = null, bool exactRequestPathMatching = true, ILogger logger = null)
            : base(next)
        {
            _allowedMethods = allowedMethods ?? new List<HttpMethod> { HttpMethod.Get };

            if (_allowedMethods.Any())
            {
                _allowedMethods = new List<HttpMethod> { HttpMethod.Get };
            }

            _exactRequestPathMatching = exactRequestPathMatching;
            _logger = logger;
        }

        [Obsolete("Use newer constructor that passes in IManagementOptions instead")]
        public EndpointOwinMiddleware(OwinMiddleware next, IEndpoint<TResult> endpoint, IEnumerable<HttpMethod> allowedMethods = null, bool exactRequestPathMatching = true, ILogger logger = null)
            : this(next, allowedMethods, exactRequestPathMatching, logger)
        {
            _endpoint = endpoint;
        }

        public override async Task Invoke(IOwinContext context)
        {
            if (!RequestVerbAndPathMatch(context.Request.Method, context.Request.Path.Value))
            {
                await Next.Invoke(context).ConfigureAwait(false);
            }
            else
            {
                _logger?.LogTrace("Processing {SteeltoeEndpoint} request", _endpoint.GetType());
                var result = _endpoint.Invoke();
                context.Response.Headers.SetValues("Content-Type", new string[] { "application/vnd.spring-boot.actuator.v2+json" });
                await context.Response.WriteAsync(Serialize(result)).ConfigureAwait(false);
            }
        }

        public virtual bool RequestVerbAndPathMatch(string httpMethod, string requestPath)
        {
            return _endpoint.RequestVerbAndPathMatch(httpMethod, requestPath, _allowedMethods, _mgmtOptions, _exactRequestPathMatching);
        }

        protected virtual string Serialize<T>(T result)
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
    public class EndpointOwinMiddleware<TResult, TRequest> : EndpointOwinMiddleware<TResult>
    {
        protected new IEndpoint<TResult, TRequest> _endpoint;

        public EndpointOwinMiddleware(OwinMiddleware next, IEndpoint<TResult, TRequest> endpoint, IEnumerable<IManagementOptions> mgmtOptions, IEnumerable<HttpMethod> allowedMethods = null, bool exactRequestPathMatching = true, ILogger logger = null)
          : base(next, mgmtOptions, allowedMethods, exactRequestPathMatching, logger)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        }

        [Obsolete("Use newer constructor that passes in IManagementOptions instead")]
        public EndpointOwinMiddleware(OwinMiddleware next, IEndpoint<TResult, TRequest> endpoint, IEnumerable<HttpMethod> allowedMethods = null, bool exactRequestPathMatching = true, ILogger logger = null)
            : base(next, allowedMethods, exactRequestPathMatching, logger)
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
            return _endpoint.RequestVerbAndPathMatch(httpMethod, requestPath, _allowedMethods, _mgmtOptions, _exactRequestPathMatching);
        }
    }

#pragma warning restore SA1402 // File may only contain a single class
}