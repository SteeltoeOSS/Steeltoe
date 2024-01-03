// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.Hosting;

namespace Steeltoe.Configuration.Kubernetes;

internal static class KubernetesHostBuilderWrapperExtensions
{
    public static HostBuilderWrapper AddKubernetesConfiguration(this HostBuilderWrapper wrapper, Action<KubernetesClientConfiguration>? configureClient,
        ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(wrapper);
        ArgumentGuard.NotNull(loggerFactory);

        wrapper.ConfigureAppConfiguration(builder => builder.AddKubernetes(configureClient, loggerFactory));
        wrapper.ConfigureServices(services => services.AddKubernetesConfigurationServices());

        return wrapper;
    }
}
