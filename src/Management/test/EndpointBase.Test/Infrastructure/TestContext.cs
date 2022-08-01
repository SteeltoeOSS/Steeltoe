// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Test.Infrastructure;

/// <summary>
/// Provides encapsulation of data for a single test.
/// </summary>
internal sealed class TestContext : IDisposable
{
    private readonly TestOutputLoggerProvider _loggerProvider;
    private readonly IServiceCollection _serviceCollection = new ServiceCollection();
    private IServiceProvider _serviceProvider;
    private IConfigurationRoot _configurationRoot;

    /// <summary>
    /// Gets or sets a delegate that allows tests to configure <see cref="IServiceCollection"/>.
    /// </summary>
    public Action<IServiceCollection, IConfiguration> AdditionalServices { get; set; }

    /// <summary>
    /// Gets or sets a delegate that allows tests to manipulate configuration.
    /// </summary>
    public Action<IConfigurationBuilder> AdditionalConfiguration { get; set; }

    /// <summary>
    /// Gets the configuration root.
    /// </summary>
    public IConfigurationRoot Configuration
    {
        get
        {
            if (_configurationRoot == null)
            {
                var configurationBuilder = new ConfigurationBuilder();
                AdditionalConfiguration?.Invoke(configurationBuilder);
                _configurationRoot = configurationBuilder.Build();
            }

            return _configurationRoot;
        }
    }

    public TestContext(ITestOutputHelper output)
    {
        _loggerProvider = new TestOutputLoggerProvider(output);
    }

    public T GetService<T>()
    {
        if (_serviceProvider == null)
        {
            // add standard services
            _serviceCollection.AddOptions();
            _serviceCollection.AddLogging(setup => setup.AddProvider(_loggerProvider));
            _serviceCollection.AddSingleton<IConfiguration>(Configuration);

            // allow test to customize services
            AdditionalServices?.Invoke(_serviceCollection, Configuration);

            _serviceProvider = _serviceCollection.BuildServiceProvider();
        }

        return _serviceProvider.GetRequiredService<T>();
    }

    public void Dispose()
    {
        _loggerProvider.Dispose();
    }
}
