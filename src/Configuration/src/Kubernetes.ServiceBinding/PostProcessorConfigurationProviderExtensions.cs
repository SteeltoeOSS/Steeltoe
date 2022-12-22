// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Extensions.Configuration.Kubernetes.ServiceBinding;

internal static class PostProcessorConfigurationProviderExtensions
{
    public static bool IsBindingTypeEnabled(this PostProcessorConfigurationProvider provider, string bindingTypeKey)
    {
        return GetValue(provider, $"steeltoe:kubernetes:bindings:{bindingTypeKey}:enable", true);
    }

    public static bool IsSteeltoeBindingsEnabled(this PostProcessorConfigurationProvider provider)
    {
        return GetValue(provider, "steeltoe:kubernetes:bindings:enable", false);
    }

    private static bool GetValue(PostProcessorConfigurationProvider provider, string key, bool defaultValue)
    {
        return provider.Source.ParentConfiguration?.GetValue(key, defaultValue) ?? defaultValue;
    }
}
