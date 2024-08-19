// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Steeltoe.Configuration.RandomValue;

/// <summary>
/// Configuration provider that provides random values. Note: This code was inspired by the Spring Boot equivalent class.
/// </summary>
internal sealed class RandomValueProvider : ConfigurationProvider
{
    private readonly ILogger<RandomValueProvider> _logger;
    private readonly string _prefix;

    /// <summary>
    /// Initializes a new instance of the <see cref="RandomValueProvider" /> class. The new placeholder resolver wraps the provided configuration.
    /// </summary>
    /// <param name="prefix">
    /// Prefix to use to match random number keys.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public RandomValueProvider(string prefix, ILoggerFactory loggerFactory)
    {
        ArgumentException.ThrowIfNullOrEmpty(prefix);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _prefix = prefix;
        _logger = loggerFactory.CreateLogger<RandomValueProvider>();
    }

    /// <summary>
    /// Tries to get a configuration value for the specified key. If the key starts with the configured prefix (default 'random:'), then the key will be
    /// parsed and the appropriate random value will be generated.
    /// </summary>
    /// <param name="key">
    /// The configuration key, which should start with the prefix specified during construction.
    /// </param>
    /// <param name="value">
    /// When this method returns, contains the produced random value, if a value for the specified key was found.
    /// </param>
    /// <returns>
    /// <c>true</c> if a value for the specified key was found, otherwise <c>false</c>.
    /// </returns>
    public override bool TryGet(string key, out string? value)
    {
        ArgumentNullException.ThrowIfNull(key);

        value = null;

        if (!key.StartsWith(_prefix, StringComparison.Ordinal))
        {
            return false;
        }

        value = GetRandomValue(key[_prefix.Length..]);
        _logger.LogDebug("Generated random value {Value} for '{Key}'", value, key);
        return true;
    }

    /// <summary>
    /// Sets a configuration value for the specified key. This method is currently ignored.
    /// </summary>
    /// <param name="key">
    /// The configuration key.
    /// </param>
    /// <param name="value">
    /// The configuration value.
    /// </param>
    public override void Set(string key, string? value)
    {
        // for future use
    }

    /// <summary>
    /// Returns the immediate descendant configuration keys for a given parent path, based on this <see cref="Configuration" />'s data and the set of keys
    /// returned by all the preceding providers.
    /// </summary>
    /// <param name="earlierKeys">
    /// The child keys returned by the preceding providers for the same parent path.
    /// </param>
    /// <param name="parentPath">
    /// The parent path.
    /// </param>
    /// <returns>
    /// The child keys.
    /// </returns>
    public override IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string? parentPath)
    {
        IEnumerable<string> keys = earlierKeys;

        if (string.IsNullOrEmpty(parentPath))
        {
            var list = new List<string>
            {
                _prefix[..^1]
            };

            keys = keys.Concat(list);
        }

        return keys.OrderBy(key => key, ConfigurationKeyComparer.Instance);
    }

    private string GetRandomValue(string type)
    {
        // random:int
        if (type == "int")
        {
            return Random.Shared.Next().ToString(CultureInfo.InvariantCulture);
        }

        // random:long
        if (type == "long")
        {
            return GetLong().ToString(CultureInfo.InvariantCulture);
        }

        // random:int(10), random:int(10,20), random:int[10], random:int[10,20]
        string? range = GetRange(type, "int");

        if (range != null)
        {
            return GetNextIntInRange(range).ToString(CultureInfo.InvariantCulture);
        }

        // random:long(10), random:long(10,20), random:long[10], random:long[10,20]
        range = GetRange(type, "long");

        if (range != null)
        {
            return GetNextLongInRange(range).ToString(CultureInfo.InvariantCulture);
        }

        // random:uuid
        if (type == "uuid")
        {
            return Guid.NewGuid().ToString();
        }

        return GetRandomBytes();
    }

    private long GetLong()
    {
        return ((long)Random.Shared.Next() << 32) + Random.Shared.Next();
    }

    private string? GetRange(string type, string prefix)
    {
        if (type.StartsWith(prefix, StringComparison.Ordinal))
        {
            int startIndex = prefix.Length + 1;

            if (type.Length > startIndex)
            {
                return type.Substring(startIndex, type.Length - 1 - startIndex);
            }
        }

        return null;
    }

    private int GetNextIntInRange(string range)
    {
        string[] tokens = range.Split(',');
        int.TryParse(tokens[0], CultureInfo.InvariantCulture, out int start);

        if (tokens.Length == 1)
        {
            return Random.Shared.Next(start);
        }

        int.TryParse(tokens[1], CultureInfo.InvariantCulture, out int max);
        return Random.Shared.Next(start, max);
    }

    private long GetNextLongInRange(string range)
    {
        string[] tokens = range.Split(',');
        long.TryParse(tokens[0], CultureInfo.InvariantCulture, out long start);

        if (tokens.Length == 1)
        {
            return Math.Abs(GetLong() % start);
        }

        long lowerBound = start;
        long.TryParse(tokens[1], CultureInfo.InvariantCulture, out long upperBound);
        upperBound -= lowerBound;
        return lowerBound + Math.Abs(GetLong() % upperBound);
    }

    private string GetRandomBytes()
    {
        byte[] bytes = new byte[16];
        Random.Shared.NextBytes(bytes);
        return BitConverter.ToString(bytes).Replace("-", string.Empty, StringComparison.Ordinal);
    }
}
