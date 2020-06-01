// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Management.EndpointWeb.Test
{
    [Serializable]
    public class TestRequest
    {
        public TestRequest(string path, string host = "localhost", string httpMethod = "GET")
        {
            Host = host;
            Path = path;
            HttpMethod = httpMethod;
        }

        public string Host { get; set; }

        public string Path { get; set; }

        public string HttpMethod { get; set; }

        public Uri Uri => new Uri($"http://{Host}{Path}");
    }
}
