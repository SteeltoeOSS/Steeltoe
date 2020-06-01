// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Owin;
using Moq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Steeltoe.Management.EndpointOwin.Test
{
    public static class OwinTestHelpers
    {
        public static readonly Dictionary<string, string> Appsettings = new Dictionary<string, string>()
        {
            ["Logging:IncludeScopes"] = "false",
            ["Logging:LogLevel:Default"] = "Warning",
            ["Logging:LogLevel:Pivotal"] = "Information",
            ["Logging:LogLevel:Steeltoe"] = "Information",
            ["management:endpoints:enabled"] = "true",
            ["management:endpoints:path"] = "/cloudfoundryapplication"
        };

        public static string CreateTempFile(string contents)
        {
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, contents);
            return tempFile;
        }

        public static Stream StringToStream(string str)
        {
            var memStream = new MemoryStream();
            var textWriter = new StreamWriter(memStream);
            textWriter.Write(str);
            textWriter.Flush();
            memStream.Seek(0, SeekOrigin.Begin);

            return memStream;
        }

        public static string StreamToString(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }

        public static IOwinContext CreateRequest(string method, string path, Stream bodyStream = null)
        {
            var context = new Mock<OwinContext>();
            bodyStream = bodyStream ?? new MemoryStream();
            context.Setup(r => r.Response).Returns(new OwinResponse { Body = bodyStream });
            context.Setup(r => r.Request).Returns(new OwinRequest
                {
                    Method = method,
                    Path = new PathString(path),
                    Scheme = "http",
                    Host = new HostString("localhost")
                });
            return context.Object;
        }

        public static async Task<string> InvokeAndReadResponse(this OwinMiddleware middle, IOwinContext context)
        {
            await middle.Invoke(context);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var rdr = new StreamReader(context.Response.Body);
            return await rdr.ReadToEndAsync();
        }
    }
}