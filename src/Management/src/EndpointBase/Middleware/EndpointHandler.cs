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
using Steeltoe.Management.Endpoint.Health;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.Endpoint.Trace;

namespace Steeltoe.Management.Endpoint.Middleware
{

    public class EndpointHandler<TResult>
    {
        protected IEndpoint<TResult> _endpoint;
        protected ILogger _logger;
        protected IManagementOptions _mgmtOptions;

        public EndpointHandler(IManagementOptions mgmtOptions, ILogger logger = null)
        {
            _logger = logger;
            _mgmtOptions = mgmtOptions ??  throw new ArgumentNullException(nameof(mgmtOptions));
        }

        public EndpointHandler(IEndpoint<TResult> endpoint, IManagementOptions mgmtOptions,  ILogger logger = null)
        {
            _logger = logger;
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _mgmtOptions = mgmtOptions ??  throw new ArgumentNullException(nameof(mgmtOptions));
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

        public virtual bool ShouldInvoke()
        {
            _logger?.LogDebug($"endpoint: {_endpoint.Id}, contextPath: {_mgmtOptions.Path}");
            return _endpoint.ShouldInvoke(_mgmtOptions);
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
                //options.Converters.Add(new MetricsResponseConverter());

                return JsonSerializer.Serialize(result, options);
            }
            catch (Exception e)
            {
                _logger?.LogError("Error {Exception} serializing {MiddlewareResponse}", e, result);
            }

            return string.Empty;
        }
    }

    public class MetricsResponseConverter : JsonConverter<MetricsListNamesResponse>
    {
        public override MetricsListNamesResponse Read(ref Utf8JsonReader reader, Type typeToConvert,
            JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, MetricsListNamesResponse value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            if (value is MetricsListNamesResponse metricslist)
            {
                writer.WritePropertyName("names");

                foreach (var name in metricslist.Names)
                {
                    JsonSerializer.Serialize(writer, name);
                }
            }

            writer.WriteEndObject();
        }
    }

#pragma warning disable SA1402 // File may only contain a single class
    public class EndpointHandler<TResult, TRequest> : EndpointHandler<TResult>
    { 
        protected new IEndpoint<TResult, TRequest> _endpoint;
        //
        // internal new IEndpoint<TResult, TRequest> Endpoint
        // {
        //     get
        //     {
        //         return _endpoint;
        //     }
        //
        //     set
        //     {
        //         _endpoint = value;
        //     }
        // }

        public EndpointHandler(IEndpoint<TResult, TRequest> endpoint, IManagementOptions mgmtOptions, ILogger logger = null)
            : base(mgmtOptions, logger)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        }

        public EndpointHandler(IManagementOptions mgmtOptions, ILogger logger = null)
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
