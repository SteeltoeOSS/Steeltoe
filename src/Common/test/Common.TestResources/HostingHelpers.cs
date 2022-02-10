// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;

namespace Steeltoe.Common
{
    public static class HostingHelpers
    {
        public static IHostEnvironment GetHostingEnvironment(string environmentName = "EnvironmentName")
        {
            return new HostingEnvironment() { EnvironmentName = environmentName };
        }
    }
}
