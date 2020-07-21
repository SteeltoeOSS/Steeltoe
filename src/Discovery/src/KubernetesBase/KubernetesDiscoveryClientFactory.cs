// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s;
using Steeltoe.Discovery.KubernetesBase.Discovery;
using System;

namespace Steeltoe.Discovery.KubernetesBase
{
    public static class KubernetesDiscoveryClientFactory
    {
        public static IDiscoveryClient CreateClient(KubernetesDiscoveryOptions options, IKubernetes kubernetes)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (kubernetes == null)
            {
                throw new ArgumentNullException(nameof(kubernetes));
            }

            var isServicePortSecureResolver = new DefaultIsServicePortSecureResolver(options);

            return new KubernetesDiscoveryClient(isServicePortSecureResolver, kubernetes, options);
        }
    }
}