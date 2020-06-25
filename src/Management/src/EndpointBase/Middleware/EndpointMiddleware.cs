// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Metrics;
using System;
using System.Text.Json;

namespace Steeltoe.Management.Endpoint.Middleware
{
    public class EndpointMiddleware<TResult>
    {
        protected IEndpoint<TResult> _endpoint;
        protected ILogger _logger;
        protected IManagementOptions _mgmtOptions;

        public EndpointMiddleware(IManagementOptions mgmtOptions, ILogger logger = null)
        {
            _logger = logger;
            _mgmtOptions = mgmtOptions ?? throw new ArgumentNullException(nameof(mgmtOptions));
        }

        public EndpointMiddleware(IEndpoint<TResult> endpoint, IManagementOptions mgmtOptions,  ILogger logger = null)
        {
            _logger = logger;
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _mgmtOptions = mgmtOptions ?? throw new ArgumentNullException(nameof(mgmtOptions));
        }

        public IEndpoint<TResult> Endpoint
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

        public virtual string Serialize(TResult result)
        {
            try
            {
                var options = new JsonSerializerOptions()
                {
                    IgnoreNullValues = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                };
                options.Converters.Add(new HealthConverter());
                options.Converters.Add(new MetricsResponseConverter());

                return JsonSerializer.Serialize(result, options);
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

        public EndpointMiddleware(IEndpoint<TResult, TRequest> endpoint, IManagementOptions mgmtOptions, ILogger logger = null)
            : base(mgmtOptions, logger)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        }

        public EndpointMiddleware(IManagementOptions mgmtOptions, ILogger logger = null)
          : base(mgmtOptions, logger)
        {
        }

        public virtual string HandleRequest(TRequest arg)
        {
            var result = _endpoint.Invoke(arg);
            return Serialize(result);
        }
    }
#pragma warning restore SA1402 // File may only contain a single class
}
