﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Net;
using System.Net.Http.Headers;

namespace Steeltoe.Discovery.Eureka.Transport
{
    public class EurekaHttpResponse
    {
        public HttpStatusCode StatusCode { get; private set; }

        public HttpResponseHeaders Headers { get; set; }

        public Uri Location { get; set; }

        public EurekaHttpResponse(HttpStatusCode statusCode)
        {
            StatusCode = statusCode;
        }
    }

#pragma warning disable SA1402 // File may only contain a single class
    public class EurekaHttpResponse<T> : EurekaHttpResponse
#pragma warning restore SA1402 // File may only contain a single class
    {
        public T Response { get; private set; }

        public EurekaHttpResponse(HttpStatusCode statusCode, T response)
            : base(statusCode)
        {
            Response = response;
        }
    }
}
