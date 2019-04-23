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

using Microsoft.Owin;
using Moq;
using System.Collections.Generic;
using System.IO;

namespace Steeltoe.Security.Authentication.CloudFoundry.Owin.Test
{
    public static class OwinTestHelpers
    {
        public static readonly Dictionary<string, string> Appsettings = new Dictionary<string, string>()
        {
        };

        public static IOwinContext CreateRequest(string method, string path, string scheme = "http", string host = "localhost", int? port = null, Stream bodyStream = null)
        {
            var context = new Mock<OwinContext>();
            bodyStream = bodyStream ?? new MemoryStream();
            context.Setup(r => r.Response).Returns(new OwinResponse { Body = bodyStream });
            context.Setup(r => r.Request).Returns(new OwinRequest
            {
                Method = method,
                Path = new PathString(path),
                Scheme = scheme,
                Host = new HostString(host + AddPortIfNotNull(port)),
            });
            return context.Object;
        }

        private static string AddPortIfNotNull(int? port)
        {
            if (port != null)
            {
                return ":" + port;
            }

            return string.Empty;
        }
    }
}