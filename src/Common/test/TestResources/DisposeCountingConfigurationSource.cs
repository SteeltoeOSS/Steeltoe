// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Common.TestResources;

/// <summary>
/// A configuration source that tracks the <see cref="DisposeCountingConfigurationProvider" /> instances it creates, so tests can verify they are
/// disposed.
/// </summary>
public sealed class DisposeCountingConfigurationSource : IConfigurationSource
{
    /// <summary>
    /// Gets all providers that were created by this source, in creation order.
    /// </summary>
    public List<DisposeCountingConfigurationProvider> Providers { get; } = [];

    /// <summary>
    /// Gets the maximum number of providers this source creates. Exceeding it throws, which turns code that keeps rebuilding the configuration (and would
    /// otherwise overflow the stack and crash the test run) into a test failure.
    /// </summary>
    public int MaxProvidersCreated { get; init; } = 100;

    /// <inheritdoc />
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        if (Providers.Count >= MaxProvidersCreated)
        {
            throw new InvalidOperationException($"Infinite recursion detected: more than {MaxProvidersCreated} providers were created.");
        }

        var provider = new DisposeCountingConfigurationProvider();
        Providers.Add(provider);
        return provider;
    }
}
