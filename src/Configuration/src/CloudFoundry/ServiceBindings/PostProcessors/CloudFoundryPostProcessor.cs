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
    private readonly HashSet<string> _tempFilePaths = [];

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

    protected void TrackTempFiles(params IEnumerable<string?> paths)
    {
        foreach (string? path in paths)
        {
            if (path != null)
            {
                _tempFilePaths.Add(path);
            }
        }
    }

    public virtual void Dispose()
    {
        foreach (string path in _tempFilePaths)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
            {
                // Intentionally left empty.
            }
        }
    }

    [Flags]
    internal enum KeyFilterSources
    {
        Tag = 1,
        Label = 2
    }
}
