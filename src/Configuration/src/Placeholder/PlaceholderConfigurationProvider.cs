// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Configuration.Placeholder;

internal sealed partial class PlaceholderConfigurationProvider : CompositeConfigurationProvider
{
    private readonly ILogger<PlaceholderConfigurationProvider> _logger;
    private readonly PropertyPlaceholderHelper _propertyPlaceholderHelper;

    public PlaceholderConfigurationProvider(IList<IConfigurationProvider> providers, ILoggerFactory loggerFactory)
        : base(providers, loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<PlaceholderConfigurationProvider>();

        ILogger<PropertyPlaceholderHelper> placeholderHelperLogger = loggerFactory.CreateLogger<PropertyPlaceholderHelper>();
        _propertyPlaceholderHelper = new PropertyPlaceholderHelper(placeholderHelperLogger);
    }

    public override bool TryGet(string key, out string? value)
    {
        bool found = base.TryGet(key, out value);

        if (found)
        {
            string? replacementValue = _propertyPlaceholderHelper.ResolvePlaceholders(value, ConfigurationRoot);

            if (replacementValue != value)
            {
                LogReplacement(key, value, replacementValue);
                value = replacementValue;
            }
        }

        return value != null;
    }

    [LoggerMessage(Level = LogLevel.Trace, Message = "Replaced value '{OriginalValue}' at key '{Key}' with '{ReplacementValue}'.")]
    private partial void LogReplacement(string key, string? originalValue, string? replacementValue);
}
