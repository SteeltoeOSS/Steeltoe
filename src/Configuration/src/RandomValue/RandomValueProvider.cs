// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common;

namespace Steeltoe.Extensions.Configuration.RandomValue;

/// <summary>
/// Configuration provider that provides random values. Note: This code was inspired by the Spring Boot equivalent class.
/// </summary>
public sealed class RandomValueProvider : ConfigurationProvider
{
    private readonly ILogger<RandomValueProvider> _logger;
    private readonly string _prefix;
    private Random _random;

    /// <summary>
    /// Initializes a new instance of the <see cref="RandomValueProvider" /> class. The new placeholder resolver wraps the provided configuration.
    /// </summary>
    /// <param name="prefix">
    /// Key prefix to use to match random number keys.
    /// </param>
    public RandomValueProvider(string prefix)
        : this(prefix, NullLoggerFactory.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RandomValueProvider" /> class. The new placeholder resolver wraps the provided configuration.
    /// </summary>
    /// <param name="prefix">
    /// Key prefix to use to match random number keys.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public RandomValueProvider(string prefix, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(prefix);
        ArgumentGuard.NotNull(loggerFactory);

        _prefix = prefix;
        _logger = loggerFactory.CreateLogger<RandomValueProvider>();
        _random = new Random();
    }

    /// <summary>
    /// Tries to get a configuration value for the specified key. If the key starts with the configured prefix (default 'random:'), then the key will be
    /// parsed and the appropriate random value will be generated.
    /// </summary>
    /// <param name="key">
    /// The configuration key, which should start with the prefix specified during construction.
    /// </param>
    /// <param name="value">
    /// Contains the produced random value if this method returns <c>true</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if a value for the specified key was found, otherwise <c>false</c>.
    /// </returns>
    public override bool TryGet(string key, out string value)
    {
        ArgumentGuard.NotNull(key);

        value = null;

        if (!key.StartsWith(_prefix, StringComparison.Ordinal))
        {
            return false;
        }

        value = GetRandomValue(key[_prefix.Length..]);
        _logger.LogDebug("Generated random value {value} for '{key}'", value, key);
        return true;
    }

    /// <summary>
    /// Sets a configuration value for the specified key. Currently ignored.
    /// </summary>
    /// <param name="key">
    /// The configuration key.
    /// </param>
    /// <param name="value">
    /// The configuration value.
    /// </param>
    public override void Set(string key, string value)
    {
        // for future use
    }

    /// <summary>
    /// Creates a new underlying random number generator.
    /// </summary>
    public override void Load()
    {
        _random = new Random();
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
    public override IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string parentPath)
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
        if (type.Equals("int"))
        {
            return _random.Next().ToString();
        }

        // random:long
        if (type.Equals("long"))
        {
            return GetLong().ToString();
        }

        // random:int(10), random:int(10,20), random:int[10], random:int[10,20]
        string range = GetRange(type, "int");

        if (range != null)
        {
            return GetNextIntInRange(range).ToString();
        }

        // random:long(10), random:long(10,20), random:long[10], random:long[10,20]
        range = GetRange(type, "long");

        if (range != null)
        {
            return GetNextLongInRange(range).ToString();
        }

        // random:uuid
        if (type.Equals("uuid"))
        {
            return Guid.NewGuid().ToString();
        }

        return GetRandomBytes();
    }

    private long GetLong()
    {
        return ((long)_random.Next() << 32) + _random.Next();
    }

    private string GetRange(string type, string prefix)
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
        int.TryParse(tokens[0], out int start);

        if (tokens.Length == 1)
        {
            return _random.Next(start);
        }

        int.TryParse(tokens[1], out int max);
        return _random.Next(start, max);
    }

    private long GetNextLongInRange(string range)
    {
        string[] tokens = range.Split(',');
        long.TryParse(tokens[0], out long start);

        if (tokens.Length == 1)
        {
            return Math.Abs(GetLong() % start);
        }

        long lowerBound = start;
        long.TryParse(tokens[1], out long upperBound);
        upperBound -= lowerBound;
        return lowerBound + Math.Abs(GetLong() % upperBound);
    }

    private string GetRandomBytes()
    {
        byte[] bytes = new byte[16];
        _random.NextBytes(bytes);
        return BitConverter.ToString(bytes).Replace("-", string.Empty);
    }
}
