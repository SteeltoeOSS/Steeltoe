// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Configuration.Placeholder.Test;

internal sealed partial class TestConfigurationSource(string name, ILoggerFactory loggerFactory) : IConfigurationSource
{
    private readonly string _name = name;
    private readonly ILogger<TestConfigurationSource> _logger = loggerFactory.CreateLogger<TestConfigurationSource>();
    private readonly ILogger<TestConfigurationProvider> _providerLogger = loggerFactory.CreateLogger<TestConfigurationProvider>();

    public Guid Id { get; } = Guid.NewGuid();
    public TestConfigurationProvider? LastProvider { get; private set; }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        LogBuild(_logger, _name);

        LastProvider = new TestConfigurationProvider(_name, _providerLogger);
        return LastProvider;
    }

    [LoggerMessage(Level = LogLevel.Trace, Message = "Build ({Name}).")]
    private static partial void LogBuild(ILogger<TestConfigurationSource> logger, string name);
}
