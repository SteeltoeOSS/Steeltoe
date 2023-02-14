// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;

namespace Steeltoe.Configuration.CloudFoundry.ServiceBinding;

internal static class PostProcessorConfigurationProviderExtensions
{
    public static bool IsCloudFoundryBindingsEnabled(this PostProcessorConfigurationProvider provider)
    {
        ArgumentGuard.NotNull(provider);

        return GetBooleanValue(provider, "steeltoe:cloudfoundry:service-bindings:enable", true);
    }

    private static bool GetBooleanValue(PostProcessorConfigurationProvider provider, string key, bool defaultValue)
    {
        return provider.Source.ParentConfiguration?.GetValue(key, defaultValue) ?? defaultValue;
    }
}
