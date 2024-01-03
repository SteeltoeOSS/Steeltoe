// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;
using Steeltoe.Common.Hosting;

namespace Steeltoe.Configuration.CloudFoundry;

internal static class HostBuilderWrapperExtensions
{
    public static HostBuilderWrapper AddCloudFoundryConfiguration(this HostBuilderWrapper wrapper)
    {
        ArgumentGuard.NotNull(wrapper);

        wrapper.ConfigureAppConfiguration(builder => builder.AddCloudFoundry());
        wrapper.ConfigureServices(services => services.RegisterCloudFoundryApplicationInstanceInfo());

        return wrapper;
    }
}
