// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Common.TestResources.IO;

namespace Steeltoe.Common.Certificates.Test;

public sealed class ConfigureCertificateOptionsTest
{
    private const string CertificateName = "test";

    [Theory]
    [InlineData("")]
    [InlineData(CertificateName)]
    public void ConfigureCertificateOptions_NoPath_NoCertificate(string certificateName)
    {
        var configureOptions = new ConfigureCertificateOptions(new ConfigurationBuilder().Build(), NullLogger<ConfigureCertificateOptions>.Instance);
        var options = new CertificateOptions();

        configureOptions.Configure(certificateName, options);

        options.Certificate.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(CertificateName)]
    public void ConfigureCertificateOptions_BadPath_NoCertificate(string certificateName)
    {
        var appSettings = new Dictionary<string, string?>
        {
            [$"{GetConfigurationKey(certificateName, "CertificateFilePath")}"] = "does-not-exist.crt"
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        var configureOptions = new ConfigureCertificateOptions(configuration, NullLogger<ConfigureCertificateOptions>.Instance);
        var options = new CertificateOptions();

        configureOptions.Configure(certificateName, options);

        options.Certificate.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(CertificateName)]
    public void ConfigureCertificateOptions_EmptyFile_Crash_logged(string certificateName)
    {
        CapturingLoggerProvider loggerProvider = new()
        {
            IncludeStackTraces = true
        };

        using var loggerFactory = new LoggerFactory([loggerProvider]);
        ILogger<ConfigureCertificateOptions> logger = loggerFactory.CreateLogger<ConfigureCertificateOptions>();

        var appSettings = new Dictionary<string, string?>
        {
            [$"{GetConfigurationKey(certificateName, "CertificateFilePath")}"] = "empty.crt"
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        var configureOptions = new ConfigureCertificateOptions(configuration, logger);
        var options = new CertificateOptions();

        configureOptions.Configure(certificateName, options);

        options.Certificate.Should().BeNull();

        loggerProvider.GetAll().Should().ContainSingle(message =>
            message.Contains(typeof(CryptographicException).FullName!, StringComparison.OrdinalIgnoreCase) && message.StartsWith(
                $"WARN {typeof(ConfigureCertificateOptions).FullName}: Failed to parse file contents for '{certificateName}' from 'empty.crt'. Will retry on next reload.",
                StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("")]
    [InlineData(CertificateName)]
    public void ConfigureCertificateOptions_ThrowsOnInvalidKey(string certificateName)
    {
        CapturingLoggerProvider loggerProvider = new()
        {
            IncludeStackTraces = true
        };

        using var loggerFactory = new LoggerFactory([loggerProvider]);
        ILogger<ConfigureCertificateOptions> logger = loggerFactory.CreateLogger<ConfigureCertificateOptions>();

        var appSettings = new Dictionary<string, string?>
        {
            [$"{GetConfigurationKey(certificateName, "CertificateFilePath")}"] = "instance.crt",
            [$"{GetConfigurationKey(certificateName, "PrivateKeyFilePath")}"] = "invalid.key"
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        var configureOptions = new ConfigureCertificateOptions(configuration, logger);
        var options = new CertificateOptions();

        configureOptions.Configure(certificateName, options);
        options.Certificate.Should().BeNull();

        loggerProvider.GetAll().Should().ContainSingle(message =>
            message.Contains(typeof(CryptographicException).FullName!, StringComparison.OrdinalIgnoreCase) && message.StartsWith(
                $"WARN {typeof(ConfigureCertificateOptions).FullName}: Failed to parse file contents for '{certificateName}' from 'instance.crt'. Will retry on next reload.",
                StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("")]
    [InlineData(CertificateName)]
    public void ConfigureCertificateOptions_ReadsP12File_CreatesCertificate(string certificateName)
    {
        var appSettings = new Dictionary<string, string?>
        {
            [$"{GetConfigurationKey(certificateName, "CertificateFilePath")}"] = "instance.p12"
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        configuration[$"{GetConfigurationKey(certificateName, "CertificateFilePath")}"].Should().NotBeNull();
        var configureOptions = new ConfigureCertificateOptions(configuration, NullLogger<ConfigureCertificateOptions>.Instance);
        var options = new CertificateOptions();

        configureOptions.Configure(certificateName, options);

        options.Certificate.Should().NotBeNull();
        options.Certificate.HasPrivateKey.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(CertificateName)]
    public void ConfigureCertificateOptions_ReadsPemFiles_CreatesCertificate(string certificateName)
    {
        var appSettings = new Dictionary<string, string?>
        {
            [$"{GetConfigurationKey(certificateName, "CertificateFilePath")}"] = "instance.crt",
            [$"{GetConfigurationKey(certificateName, "PrivateKeyFilePath")}"] = "instance.key"
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        var configureOptions = new ConfigureCertificateOptions(configuration, NullLogger<ConfigureCertificateOptions>.Instance);
        var options = new CertificateOptions();

        configureOptions.Configure(certificateName, options);

        options.Certificate.Should().NotBeNull();
        options.Certificate.HasPrivateKey.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(CertificateName)]
    public async Task CertificateOptions_update_on_changed_contents(string certificateName)
    {
        using var sandbox = new Sandbox();
        string firstCertificateContent = await File.ReadAllTextAsync("instance.crt", TestContext.Current.CancellationToken);
        string firstPrivateKeyContent = await File.ReadAllTextAsync("instance.key", TestContext.Current.CancellationToken);
        using var firstX509 = X509Certificate2.CreateFromPemFile("instance.crt", "instance.key");
        string certificateFilePath = sandbox.CreateFile(Guid.NewGuid() + ".crt", firstCertificateContent);
        string privateKeyFilePath = sandbox.CreateFile(Guid.NewGuid() + ".key", firstPrivateKeyContent);
        string secondCertificateContent = await File.ReadAllTextAsync("secondInstance.crt", TestContext.Current.CancellationToken);
        string secondPrivateKeyContent = await File.ReadAllTextAsync("secondInstance.key", TestContext.Current.CancellationToken);
        using var secondX509 = X509Certificate2.CreateFromPemFile("secondInstance.crt", "secondInstance.key");
        string appSettings = BuildAppSettingsJson(certificateName, certificateFilePath, privateKeyFilePath);
        string appSettingsPath = sandbox.CreateFile(MemoryFileProvider.DefaultAppSettingsFileName, appSettings);
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddJsonFile(appSettingsPath, false, true);
        IConfiguration configuration = configurationBuilder.Build();

        IServiceCollection services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(configuration);
        services.ConfigureCertificateOptions(certificateName);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<CertificateOptions>>();
        optionsMonitor.Get(certificateName).Certificate.Should().BeEquivalentTo(firstX509);

        await File.WriteAllTextAsync(certificateFilePath, secondCertificateContent, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(privateKeyFilePath, secondPrivateKeyContent, TestContext.Current.CancellationToken);

        using Task pollTask = WaitUntilCertificateChangedToAsync(secondX509, optionsMonitor, certificateName, TestContext.Current.CancellationToken);
        await pollTask.WaitAsync(TimeSpan.FromSeconds(4), TestContext.Current.CancellationToken);

        optionsMonitor.Get(certificateName).Certificate.Should().Be(secondX509);
    }

    [Theory]
    [InlineData("")]
    [InlineData(CertificateName)]
    public async Task CertificateOptions_update_on_changed_path(string certificateName)
    {
        using var sandbox = new Sandbox();
        string firstCertificateContent = await File.ReadAllTextAsync("instance.crt", TestContext.Current.CancellationToken);
        string firstPrivateKeyContent = await File.ReadAllTextAsync("instance.key", TestContext.Current.CancellationToken);
        using var firstX509 = X509Certificate2.CreateFromPemFile("instance.crt", "instance.key");
        string firstCertificateFilePath = sandbox.CreateFile(Guid.NewGuid() + ".crt", firstCertificateContent);
        string firstPrivateKeyFilePath = sandbox.CreateFile(Guid.NewGuid() + ".key", firstPrivateKeyContent);
        using var secondX509 = X509Certificate2.CreateFromPemFile("secondInstance.crt", "secondInstance.key");
        string appSettings = BuildAppSettingsJson(certificateName, firstCertificateFilePath, firstPrivateKeyFilePath);
        string appSettingsPath = sandbox.CreateFile(MemoryFileProvider.DefaultAppSettingsFileName, appSettings);
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddJsonFile(appSettingsPath, false, true);
        IConfiguration configuration = configurationBuilder.Build();

        IServiceCollection services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(configuration);
        services.ConfigureCertificateOptions(certificateName);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<CertificateOptions>>();
        optionsMonitor.Get(certificateName).Certificate.Should().BeEquivalentTo(firstX509);

        appSettings = BuildAppSettingsJson(certificateName, "secondInstance.crt", "secondInstance.key");
        await File.WriteAllTextAsync(appSettingsPath, appSettings, TestContext.Current.CancellationToken);

        using Task pollTask = WaitUntilCertificateChangedToAsync(secondX509, optionsMonitor, certificateName, TestContext.Current.CancellationToken);
        await pollTask.WaitAsync(TimeSpan.FromSeconds(4), TestContext.Current.CancellationToken);

        optionsMonitor.Get(certificateName).Certificate.Should().Be(secondX509);
    }

    private static string BuildAppSettingsJson(string certificateName, string certificatePath, string keyPath)
    {
        string certificateBlock = $"""
                "CertificateFilePath": {JsonSerializer.Serialize(certificatePath)},
                "PrivateKeyFilePath": {JsonSerializer.Serialize(keyPath)}
            """;

        string namedCertificateSection = string.IsNullOrEmpty(certificateName)
            ? certificateBlock
            : $"{JsonSerializer.Serialize(certificateName)}: {{ {certificateBlock} }}";

        return $$"""
            {
              "Certificates": {
                {{namedCertificateSection}}
              }
            }
            """;
    }

    private static async Task WaitUntilCertificateChangedToAsync(X509Certificate2 expectedCertificate, IOptionsMonitor<CertificateOptions> optionsMonitor,
        string certificateName, CancellationToken cancellationToken)
    {
        while (!Equals(optionsMonitor.Get(certificateName).Certificate, expectedCertificate))
        {
            await Task.Delay(50, cancellationToken);
        }
    }

    private static string GetConfigurationKey(string? optionName, string propertyName)
    {
        return string.IsNullOrEmpty(optionName)
            ? string.Join(ConfigurationPath.KeyDelimiter, CertificateOptions.ConfigurationKeyPrefix, propertyName)
            : string.Join(ConfigurationPath.KeyDelimiter, CertificateOptions.ConfigurationKeyPrefix, optionName, propertyName);
    }
}
