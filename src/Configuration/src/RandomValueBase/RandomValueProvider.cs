// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Extensions.Configuration.RandomValue;

/// <summary>
/// Configuration provider that provides random values.
/// Note: This code was inspired by the Spring Boot equivalent class.
/// </summary>
public class RandomValueProvider : ConfigurationProvider
{
    internal ILogger<RandomValueProvider> _logger;
    internal Random _random;
    internal string _prefix;

    /// <summary>
    /// Initializes a new instance of the <see cref="RandomValueProvider"/> class.
    /// The new placeholder resolver wraps the provided configuration
    /// </summary>
    /// <param name="prefix">key prefix to use to match random number keys</param>
    /// <param name="logFactory">the logger factory to use</param>
    public RandomValueProvider(string prefix, ILoggerFactory logFactory = null)
    {
        _prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
        _logger = logFactory?.CreateLogger<RandomValueProvider>();
        _random = new System.Random();
    }

    /// <summary>
    /// Tries to get a configuration value for the specified key. If the key starts with the
    /// configured prefix (default random:), then the key will be parsed and the appropriate
    /// random value will be generated.
    /// </summary>
    /// <param name="key">The key which should start with prefix</param>
    /// <param name="value">The random value returned</param>
    /// <returns><c>True</c> if a value for the specified key was found, otherwise <c>false</c>.</returns>
    public override bool TryGet(string key, out string value)
    {
        value = null;
        if (!key.StartsWith(_prefix))
        {
            return false;
        }

        value = GetRandomValue(key.Substring(_prefix.Length));
        _logger?.LogDebug("Generated random value {value} for '{key}'", value, key);
        return true;
    }

    /// <summary>
    /// Sets a configuration value for the specified key. Currently ignored.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public override void Set(string key, string value)
    {
        // for future use
    }

    /// <summary>
    /// Creates a new underlying Random number generator.
    /// </summary>
    public override void Load()
    {
        _random = new Random();
    }

    /// <summary>
    /// Returns an empty list.
    /// </summary>
    /// <param name="earlierKeys">The child keys returned by the preceding providers for the same parent path.</param>
    /// <param name="parentPath">The parent path.</param>
    /// <returns>The child keys.</returns>
    public override IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string parentPath)
    {
        if (string.IsNullOrEmpty(parentPath))
        {
            var list = new List<string>() { _prefix.Substring(0, _prefix.Length - 1) };
            return list.Concat(earlierKeys)
                .OrderBy(k => k, ConfigurationKeyComparer.Instance);
        }

        return earlierKeys
            .OrderBy(k => k, ConfigurationKeyComparer.Instance);
    }

    internal string GetRandomValue(string type)
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
        var range = GetRange(type, "int");
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

    internal long GetLong()
    {
        return ((long)_random.Next() << 32) + _random.Next();
    }

    internal string GetRange(string type, string prefix)
    {
        if (type.StartsWith(prefix))
        {
            var startIndex = prefix.Length + 1;
            if (type.Length > startIndex)
            {
                return type.Substring(startIndex, type.Length - 1 - startIndex);
            }
        }

        return null;
    }

    internal int GetNextIntInRange(string range)
    {
        var tokens = range.Split(',');
        int.TryParse(tokens[0], out var start);
        if (tokens.Length == 1)
        {
            return _random.Next(start);
        }

        int.TryParse(tokens[1], out var max);
        return _random.Next(start, max);
    }

    internal long GetNextLongInRange(string range)
    {
        var tokens = range.Split(',');
        long.TryParse(tokens[0], out var start);
        if (tokens.Length == 1)
        {
            return Math.Abs(GetLong() % start);
        }

        var lowerBound = start;
        long.TryParse(tokens[1], out var upperBound);
        upperBound -= lowerBound;
        return lowerBound + Math.Abs(GetLong() % upperBound);
    }

    internal string GetRandomBytes()
    {
        var bytes = new byte[16];
        _random.NextBytes(bytes);
        return BitConverter.ToString(bytes).Replace("-", string.Empty);
    }
}