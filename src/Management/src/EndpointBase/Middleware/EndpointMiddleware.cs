// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));

            if (mgmtOptions == null)
            {
                throw new ArgumentNullException(nameof(mgmtOptions));
            }

            var mOptions = mgmtOptions.ToList();
            _mgmtOptions = mOptions.Count > 0 ? mOptions : null;
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
            _logger?.LogDebug($"endpoint: {_endpoint.Id}, httpMethod:  {httpMethod}, requestPath: {requestPath}, contextPaths: {string.Join(",", _mgmtOptions?.Select(x => x.Path))}");

            return _endpoint.RequestVerbAndPathMatch(httpMethod, requestPath, _allowedMethods, _mgmtOptions, _exactRequestPathMatching);
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

        internal new IEndpoint<TResult, TRequest> Endpoint
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

        public EndpointMiddleware(IEndpoint<TResult, TRequest> endpoint, IEnumerable<IManagementOptions> mgmtOptions, IEnumerable<HttpMethod> allowedMethods = null, bool exactRequestPathMatching = true, ILogger logger = null)
            : base(mgmtOptions, allowedMethods, exactRequestPathMatching, logger)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        }

        public EndpointMiddleware(IEnumerable<IManagementOptions> mgmtOptions, IEnumerable<HttpMethod> allowedMethods = null, bool exactRequestPathMatching = true, ILogger logger = null)
          : base(mgmtOptions, allowedMethods, exactRequestPathMatching, logger)
        {
        }

        public virtual string HandleRequest(TRequest arg)
        {
            var result = _endpoint.Invoke(arg);
            return Serialize(result);
        }

        public override bool RequestVerbAndPathMatch(string httpMethod, string requestPath)
        {
            var result = _endpoint.RequestVerbAndPathMatch(httpMethod, requestPath, _allowedMethods, _mgmtOptions, _exactRequestPathMatching);
            _logger?.LogDebug($"endpoint: {_endpoint.Id}, httpMethod:  {httpMethod}, requestPath: {requestPath}, contextPaths: {string.Join(",", _mgmtOptions?.Select(x => x.Path))}, result: {result}");
            return result;
        }
    }
#pragma warning restore SA1402 // File may only contain a single class
}
