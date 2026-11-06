// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

#pragma warning disable S3881 // "IDisposable" should be implemented correctly

namespace Steeltoe.Configuration.CloudFoundry.ServiceBindings.PostProcessors;

internal abstract partial class CloudFoundryPostProcessor : IConfigurationPostProcessor, IDisposable
{
    private const int RegexMatchTimeoutInMilliseconds = 1_000;

    /// <summary>
    /// Gets the temporary files (such as certificates and keys) that are shared by all providers that use this post-processor, which is deliberately the
    /// case for the temporary copies of the parent configuration. A file is created once per distinct content, instead of each time the configuration is
    /// processed again (such as when it reloads), and files are deleted when the post-processor is disposed.
    /// </summary>
    protected TempFileRegistry TempFileRegistry { get; } = new();

    [GeneratedRegex("^vcap:services:[^:]+:[0-9]+:tags:[0-9]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        RegexMatchTimeoutInMilliseconds)]
    private static partial Regex TagsConfigurationKeyRegex();

    [GeneratedRegex("^vcap:services:[^:]+:[0-9]+:label+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        RegexMatchTimeoutInMilliseconds)]
    private static partial Regex LabelConfigurationKeyRegex();

    public abstract void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string?> configurationData);

    protected ICollection<string> FilterKeys(IDictionary<string, string?> configurationData, string valueToFind, KeyFilterSources sources)
    {
        List<string> keys = [];

        foreach ((string key, string? value) in configurationData)
        {
            if ((sources & KeyFilterSources.Tag) != 0 && TagsConfigurationKeyRegex().IsMatch(key) &&
                string.Equals(value, valueToFind, StringComparison.OrdinalIgnoreCase))
            {
                string? parentKey = ConfigurationPath.GetParentPath(key);

                if (parentKey != null)
                {
                    string? serviceBindingKey = ConfigurationPath.GetParentPath(parentKey);

                    if (serviceBindingKey != null)
                    {
                        keys.Add(serviceBindingKey);
                    }
                }
            }

            if ((sources & KeyFilterSources.Label) != 0 && LabelConfigurationKeyRegex().IsMatch(key) &&
                string.Equals(value, valueToFind, StringComparison.OrdinalIgnoreCase))
            {
                string? serviceBindingKey = ConfigurationPath.GetParentPath(key);

                if (serviceBindingKey != null)
                {
                    keys.Add(serviceBindingKey);
                }
            }
        }

        return keys;
    }

    public virtual void Dispose()
    {
        TempFileRegistry.Dispose();
    }

    [Flags]
    internal enum KeyFilterSources
    {
        Tag = 1,
        Label = 2
    }
}
