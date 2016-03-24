//
// Copyright 2015 the original author or authors.
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
//

using System;
using System.Net;
using System.Net.Http.Headers;

namespace SteelToe.Discovery.Eureka.Transport
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
    public class EurekaHttpResponse<T> : EurekaHttpResponse
    {
        public T Response { get; private set; }
        public EurekaHttpResponse(HttpStatusCode statusCode, T response) :
            base(statusCode)
        {
            Response = response;
        }
    }
}
