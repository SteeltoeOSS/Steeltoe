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

using System.Collections.Specialized;
using System.Net;

namespace Steeltoe.Management.EndpointWeb.Test
{
    [System.Serializable]
    public class TestResponse
    {
        public TestResponse(string content, HttpStatusCode statusCode, NameValueCollection headers)
        {
            Content = content;
            StatusCode = statusCode;
            Headers = headers;
        }

        public string Content { get; set; }

        public HttpStatusCode StatusCode { get; set; }

        public NameValueCollection Headers { get; set; }
    }
}
