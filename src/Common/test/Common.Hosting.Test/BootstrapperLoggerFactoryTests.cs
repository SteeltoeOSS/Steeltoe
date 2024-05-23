// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Steeltoe.Common.Logging;
using Xunit;

namespace Steeltoe.Common.Hosting.Test;

public sealed class BootstrapperLoggerFactoryTests
{
    [Fact]
    public void BootstrapLoggerFactory_CorrectTypeResolved()
    {
        IBootstrapLoggerFactory sut = BootstrapLoggerFactory.Default;
        sut.Should().BeOfType<UpgradableBootstrapLoggerFactory>();
    }

    [Fact]
    public void UpgradableBootLogger()
    {
        IBootstrapLoggerFactory sut = BootstrapLoggerFactory.Default;
        sut.Should().BeOfType<UpgradableBootstrapLoggerFactory>();

        var logProvider = new Mock<ILoggerProvider>();
        var mockLogger = new Mock<ILogger>();
        logProvider.Setup(provider => provider.CreateLogger(It.IsAny<string>())).Returns(() => mockLogger.Object);

        sut = new UpgradableBootstrapLoggerFactory((builder, configuration) =>
        {
            builder.Services.AddSingleton(logProvider.Object);
            builder.AddConfiguration(configuration.GetSection("Logging"));
        });

        ILogger logger = sut.CreateLogger("test");

        // test default bootstrapper mode
        logger.LogInformation("Test");

        mockLogger.Verify(x => x.Log(LogLevel.Information, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => string.Equals("Test", o.ToString(), StringComparison.OrdinalIgnoreCase)), It.IsAny<Exception>(),
            It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)));

        // test change to log levels after updated with configuration
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            { "Logging:LogLevel:Default", nameof(LogLevel.Warning) }
        }).Build();

        sut.Update(configurationRoot);
        logger.LogInformation("Test2");

        mockLogger.Verify(
            x => x.Log(LogLevel.Information, It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => string.Equals("Test2", o.ToString(), StringComparison.OrdinalIgnoreCase)), It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Never);

        // upgrade bootstrapper with new log factory, and confirm that it delegates to loggers spawned from it
        var newLogProvider = new Mock<ILoggerProvider>();
        var newMockLogger = new Mock<ILogger>();
        newLogProvider.Setup(provider => provider.CreateLogger(It.IsAny<string>())).Returns(() => newMockLogger.Object);
        ILoggerFactory newLoggerFactory = LoggerFactory.Create(builder => builder.Services.AddSingleton(newLogProvider));

        sut.Update(newLoggerFactory);
        logger.LogInformation("Test3");

        newMockLogger.Verify(
            x => x.Log(LogLevel.Information, It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => string.Equals("Test3", o.ToString(), StringComparison.OrdinalIgnoreCase)), It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Never);
    }
}
