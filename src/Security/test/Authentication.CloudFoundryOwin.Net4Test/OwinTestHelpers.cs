// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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