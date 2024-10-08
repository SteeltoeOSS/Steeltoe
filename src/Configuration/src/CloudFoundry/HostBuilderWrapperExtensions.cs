// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Hosting;

namespace Steeltoe.Configuration.CloudFoundry;

internal static class HostBuilderWrapperExtensions
{
    public static HostBuilderWrapper AddCloudFoundryConfiguration(this HostBuilderWrapper wrapper, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(wrapper);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        wrapper.ConfigureAppConfiguration(builder => builder.AddCloudFoundry(null, loggerFactory));
        wrapper.ConfigureServices(services => services.AddCloudFoundryOptions());

        return wrapper;
    }
}
