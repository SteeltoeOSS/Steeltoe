// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Steeltoe.Management.Endpoint.Env
{
#if NETSTANDARD2_1
    public class GenericHostingEnvironment : IHostEnvironment
#else
    public class GenericHostingEnvironment : IHostingEnvironment
#endif
    {
        public string EnvironmentName { get; set; }

        public string ApplicationName { get; set; }

        public string ContentRootPath { get; set; }

        public IFileProvider ContentRootFileProvider { get; set; }
    }
}
