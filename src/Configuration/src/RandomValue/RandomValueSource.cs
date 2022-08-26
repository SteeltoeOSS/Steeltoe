// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common;

namespace Steeltoe.Extensions.Configuration.RandomValue;

/// <summary>
/// Configuration source used in creating a <see cref="RandomValueProvider" /> that generates random numbers.
/// </summary>
public sealed class RandomValueSource : IConfigurationSource
{
    private const string RandomPrefix = "random:";

    internal string Prefix { get; }
    internal ILoggerFactory LoggerFactory { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RandomValueSource" /> class.
    /// </summary>
    public RandomValueSource()
        : this(RandomPrefix, NullLoggerFactory.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RandomValueSource" /> class.
    /// </summary>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public RandomValueSource(ILoggerFactory loggerFactory)
        : this(RandomPrefix, loggerFactory)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RandomValueSource" /> class using a custom prefix for key values.
    /// </summary>
    /// <param name="prefix">
    /// Key prefix to use to match random number keys. Should end with the configuration separator.
    /// </param>
    public RandomValueSource(string prefix)
        : this(prefix, NullLoggerFactory.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RandomValueSource" /> class using a custom prefix for key values.
    /// </summary>
    /// <param name="prefix">
    /// Key prefix to use to match random number keys. Should end with the configuration separator.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public RandomValueSource(string prefix, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(prefix);
        ArgumentGuard.NotNull(loggerFactory);

        Prefix = prefix;
        LoggerFactory = loggerFactory;
    }

    /// <summary>
    /// Builds a <see cref="RandomValueProvider" /> from the sources.
    /// </summary>
    /// <param name="builder">
    /// The configuration builder.
    /// </param>
    /// <returns>
    /// The random number provider.
    /// </returns>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new RandomValueProvider(Prefix, LoggerFactory);
    }
}
