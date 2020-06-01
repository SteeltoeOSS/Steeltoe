// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
