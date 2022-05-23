// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Metrics;
using System;
using System.Linq;
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
            if (_mgmtOptions is ManagementEndpointOptions mgmt)
            {
                mgmt.SerializerOptions = GetSerializerOptions(mgmt.SerializerOptions);
            }
        }

        public EndpointMiddleware(IEndpoint<TResult> endpoint, IManagementOptions mgmtOptions, ILogger logger = null)
            : this(mgmtOptions, logger)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        }

        public IEndpoint<TResult> Endpoint
        {
            get => _endpoint;

            set => _endpoint = value;
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
                JsonSerializerOptions options;
                if (_mgmtOptions is ManagementEndpointOptions mgmt)
                {
                    options = mgmt.SerializerOptions;
                }
                else
                {
                    options = GetSerializerOptions(null);
                }

                return JsonSerializer.Serialize(result, options);
            }
            catch (Exception e)
            {
                _logger?.LogError("Error {Exception} serializing {MiddlewareResponse}", e, result);
            }

            return string.Empty;
        }

        internal JsonSerializerOptions GetSerializerOptions(JsonSerializerOptions serializerOptions)
        {
            serializerOptions ??= new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            serializerOptions.IgnoreNullValues = true;
            if (serializerOptions.Converters?.Any(c => c is HealthConverter) != true)
            {
                serializerOptions.Converters.Add(new HealthConverter());
            }

            if (serializerOptions.Converters?.Any(c => c is MetricsResponseConverter) != true)
            {
                serializerOptions.Converters.Add(new MetricsResponseConverter());
            }

            return serializerOptions;
        }
    }

    public class EndpointMiddleware<TResult, TRequest> : EndpointMiddleware<TResult>
    {
        protected new IEndpoint<TResult, TRequest> _endpoint;

        internal new IEndpoint<TResult, TRequest> Endpoint
        {
            get => _endpoint;

            set => _endpoint = value;
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
}
