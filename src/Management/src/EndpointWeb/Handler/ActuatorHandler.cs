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
using Steeltoe.Management.Endpoint.Security;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace Steeltoe.Management.Endpoint.Handler
{
    public class ActuatorHandler : IActuatorHandler
    {
        protected ILogger _logger;
        protected IEnumerable<HttpMethod> _allowedMethods;
        protected bool _exactRequestPathMatching;
        protected IEnumerable<IManagementOptions> _mgmtOptions;
        protected IEnumerable<ISecurityService> _securityServices;

        public ActuatorHandler(IEnumerable<ISecurityService> securityServices, IEnumerable<IManagementOptions> mgmtOptions, IEnumerable<HttpMethod> allowedMethods = null, bool exactRequestPathMatching = true, ILogger logger = null)
        {
            _logger = logger;
            _allowedMethods = allowedMethods ?? new List<HttpMethod> { HttpMethod.Get };
            _exactRequestPathMatching = exactRequestPathMatching;
            _securityServices = securityServices;
            _mgmtOptions = mgmtOptions;
        }

        [Obsolete("Use newer constructor that passes in IManagementOptions instead")]
        public ActuatorHandler(IEnumerable<ISecurityService> securityServices, IEnumerable<HttpMethod> allowedMethods = null, bool exactRequestPathMatching = true, ILogger logger = null)
        {
            _logger = logger;
            _allowedMethods = allowedMethods ?? new List<HttpMethod> { HttpMethod.Get };
            _exactRequestPathMatching = exactRequestPathMatching;
            _securityServices = securityServices;
        }

        public virtual void HandleRequest(HttpContextBase context)
        {
        }

        public virtual Task<bool> IsAccessAllowed(HttpContextBase context)
        {
            return Task.FromResult(false);
        }

        public virtual bool RequestVerbAndPathMatch(string httpMethod, string requestPath)
        {
            return false;
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
    public class ActuatorHandler<TEndpoint, TResult> : ActuatorHandler
#pragma warning restore SA1402 // File may only contain a single class
    {
        protected IEndpoint<TResult> _endpoint;

        public ActuatorHandler(IEnumerable<ISecurityService> securityServices, IEnumerable<IManagementOptions> mgmtOptions, IEnumerable<HttpMethod> allowedMethods = null, bool exactRequestPathMatching = true, ILogger logger = null)
           : base(securityServices, mgmtOptions, allowedMethods, exactRequestPathMatching, logger)
        {
        }

        public ActuatorHandler(IEndpoint<TResult> endpoint, IEnumerable<ISecurityService> securityServices, IEnumerable<IManagementOptions> mgmtOptions, IEnumerable<HttpMethod> allowedMethods = null, bool exactRequestPathMatching = true, ILogger logger = null)
          : base(securityServices, mgmtOptions, allowedMethods, exactRequestPathMatching, logger)
        {
            _endpoint = endpoint ?? throw new NullReferenceException(nameof(endpoint));
        }

        [Obsolete("Use newer constructor that passes in IManagementOptions instead")]
        public ActuatorHandler(IEnumerable<ISecurityService> securityServices, IEnumerable<HttpMethod> allowedMethods = null, bool exactRequestPathMatching = true, ILogger logger = null)
            : base(securityServices, allowedMethods, exactRequestPathMatching, logger)
        {
        }

        [Obsolete("Use newer constructor that passes in IManagementOptions instead")]
        public ActuatorHandler(IEndpoint<TResult> endpoint, IEnumerable<ISecurityService> securityServices, IEnumerable<HttpMethod> allowedMethods = null, bool exactRequestPathMatching = true, ILogger logger = null)
            : base(securityServices, allowedMethods, exactRequestPathMatching, logger)
        {
            _endpoint = endpoint ?? throw new NullReferenceException(nameof(endpoint));
        }

        public override void HandleRequest(HttpContextBase context)
        {
            _logger?.LogTrace("Processing {SteeltoeEndpoint} request", typeof(TEndpoint));
            var result = _endpoint.Invoke();
            context.Response.Headers.Set("Content-Type", "application/vnd.spring-boot.actuator.v2+json");
            context.Response.Write(Serialize(result));
        }

        public override bool RequestVerbAndPathMatch(string httpMethod, string requestPath)
        {
            return _endpoint.RequestVerbAndPathMatch(httpMethod, requestPath, _allowedMethods, _mgmtOptions, _exactRequestPathMatching);
        }

        public override Task<bool> IsAccessAllowed(HttpContextBase context)
        {
            return _securityServices?.IsAccessAllowed(context, _endpoint.Options);
        }
    }

#pragma warning disable SA1402 // File may only contain a single class
#pragma warning disable S2436 // Types and methods should not have too many generic parameters
    public class ActuatorHandler<TEndpoint, TResult, TRequest> : ActuatorHandler<TEndpoint, TResult>
#pragma warning restore S2436 // Types and methods should not have too many generic parameters
#pragma warning restore SA1402 // File may only contain a single class
    {
        protected new IEndpoint<TResult, TRequest> _endpoint;

        public ActuatorHandler(IEndpoint<TResult, TRequest> endpoint, IEnumerable<ISecurityService> securityServices, IEnumerable<IManagementOptions> mgmtOptions,  IEnumerable<HttpMethod> allowedMethods = null, bool exactRequestPathMatching = true, ILogger logger = null)
            : base(securityServices, mgmtOptions, allowedMethods, exactRequestPathMatching, logger)
        {
            _endpoint = endpoint ?? throw new NullReferenceException(nameof(endpoint));
        }

        [Obsolete("Use newer constructor that passes in IManagementOptions instead")]
        public ActuatorHandler(IEndpoint<TResult, TRequest> endpoint, IEnumerable<ISecurityService> securityServices, IEnumerable<HttpMethod> allowedMethods = null, bool exactRequestPathMatching = true, ILogger logger = null)
            : base(securityServices, allowedMethods, exactRequestPathMatching, logger)
        {
            _endpoint = endpoint ?? throw new NullReferenceException(nameof(endpoint));
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

        public async override Task<bool> IsAccessAllowed(HttpContextBase context)
        {
            return await _securityServices.IsAccessAllowed(context, _endpoint.Options).ConfigureAwait(false);
        }
    }
}
