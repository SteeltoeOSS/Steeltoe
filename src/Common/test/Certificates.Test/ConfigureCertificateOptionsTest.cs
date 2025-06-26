// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
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
        var configureOptions = new ConfigureCertificateOptions(new ConfigurationBuilder().Build());
        var options = new CertificateOptions();

        configureOptions.Configure(certificateName, options);

        options.Certificate.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(CertificateName)]
    public void ConfigureCertificateOptions_BadPath_NoCertificate(string certificateName)
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{GetConfigurationKey(certificateName, "CertificateFilePath")}"] = "does-not-exist.crt"
        }).Build();

        var configureOptions = new ConfigureCertificateOptions(configuration);

        var options = new CertificateOptions();

        configureOptions.Configure(certificateName, options);

        options.Certificate.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(CertificateName)]
    public void ConfigureCertificateOptions_EmptyFile_Crash_logged(string certificateName)
    {
        CapturingLoggerProvider loggerProvider = new();
        using var loggerFactory = new LoggerFactory([loggerProvider]);

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{GetConfigurationKey(certificateName, "CertificateFilePath")}"] = "empty.crt"
        }).Build();

        var configureOptions = new ConfigureCertificateOptions(configuration, loggerFactory.CreateLogger<ConfigureCertificateOptions>());
        var options = new CertificateOptions();

        configureOptions.Configure(certificateName, options);
        options.Certificate.Should().BeNull();

        loggerProvider.GetAll().Should()
            .Contain(
                $"DBUG Steeltoe.Common.Certificates.ConfigureCertificateOptions: CryptographicException while parsing certificate for '{certificateName}' from 'empty.crt'. Will retry on next reload.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(CertificateName)]
    public void ConfigureCertificateOptions_ThrowsOnInvalidKey(string certificateName)
    {
        CapturingLoggerProvider loggerProvider = new();
        using var loggerFactory = new LoggerFactory([loggerProvider]);

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{GetConfigurationKey(certificateName, "CertificateFilePath")}"] = "instance.crt",
            [$"{GetConfigurationKey(certificateName, "PrivateKeyFilePath")}"] = "invalid.key"
        }).Build();

        var configureOptions = new ConfigureCertificateOptions(configuration, loggerFactory.CreateLogger<ConfigureCertificateOptions>());
        var options = new CertificateOptions();

        configureOptions.Configure(certificateName, options);
        options.Certificate.Should().BeNull();

        loggerProvider.GetAll().Should()
            .Contain(
                $"DBUG Steeltoe.Common.Certificates.ConfigureCertificateOptions: CryptographicException while parsing certificate for '{certificateName}' from 'instance.crt'. Will retry on next reload.");
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
        var configureOptions = new ConfigureCertificateOptions(configuration);
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
        var configureOptions = new ConfigureCertificateOptions(configuration);
        var options = new CertificateOptions();

        configureOptions.Configure(certificateName, options);

        options.Certificate.Should().NotBeNull();
        options.Certificate.HasPrivateKey.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(CertificateName)]
    public async Task CertificateOptionsUpdateOnFileContentChange(string certificateName)
    {
        using var sandbox = new Sandbox();
        string firstCertificateContent = await File.ReadAllTextAsync("instance.crt", TestContext.Current.CancellationToken);
        string firstPrivateKeyContent = await File.ReadAllTextAsync("instance.key", TestContext.Current.CancellationToken);
        using var firstX509 = X509Certificate2.CreateFromPemFile("instance.crt", "instance.key");
        string secondCertificateContent = await File.ReadAllTextAsync("secondInstance.crt", TestContext.Current.CancellationToken);
        string secondPrivateKeyContent = await File.ReadAllTextAsync("secondInstance.key", TestContext.Current.CancellationToken);
        using var secondX509 = X509Certificate2.CreateFromPemFile("secondInstance.crt", "secondInstance.key");
        string certificateFilePath = sandbox.CreateFile(Guid.NewGuid() + ".crt", firstCertificateContent);
        string privateKeyFilePath = sandbox.CreateFile(Guid.NewGuid() + ".key", firstPrivateKeyContent);

        MemoryFileProvider fileProvider = new();
        fileProvider.IncludeFile(MemoryFileProvider.DefaultAppSettingsFileName, BuildAppSettingsJson(certificateName, certificateFilePath, privateKeyFilePath));
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddJsonFile(fileProvider, MemoryFileProvider.DefaultAppSettingsFileName, false, true);
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

        using Task pollTask = PollCertificateAsync(optionsMonitor, certificateName, secondX509, TestContext.Current.CancellationToken);
        await pollTask.WaitAsync(TimeSpan.FromSeconds(4), TestContext.Current.CancellationToken);

        optionsMonitor.Get(certificateName).Certificate.Should().Be(secondX509);
    }

    [Theory]
    [InlineData("")]
    [InlineData(CertificateName)]
    public async Task CertificateOptionsUpdateOnFileLocationChange(string certificateName)
    {
        using var sandbox = new Sandbox();
        string firstInstanceCertificate = await File.ReadAllTextAsync("instance.crt", TestContext.Current.CancellationToken);
        string firstInstancePrivateKey = await File.ReadAllTextAsync("instance.key", TestContext.Current.CancellationToken);
        using var firstX509 = X509Certificate2.CreateFromPemFile("instance.crt", "instance.key");
        string firstCertificateFilePath = sandbox.CreateFile(Guid.NewGuid() + ".crt", firstInstanceCertificate);
        string firstPrivateKeyFilePath = sandbox.CreateFile(Guid.NewGuid() + ".key", firstInstancePrivateKey);
        string secondInstanceCertificate = await File.ReadAllTextAsync("secondInstance.crt", TestContext.Current.CancellationToken);
        string secondInstancePrivateKey = await File.ReadAllTextAsync("secondInstance.key", TestContext.Current.CancellationToken);
        using var secondX509 = X509Certificate2.CreateFromPemFile("secondInstance.crt", "secondInstance.key");
        string secondCertificateFilePath = sandbox.CreateFile(Guid.NewGuid() + ".crt", secondInstanceCertificate);
        string secondPrivateKeyFilePath = sandbox.CreateFile(Guid.NewGuid() + ".key", secondInstancePrivateKey);

        MemoryFileProvider fileProvider = new();

        fileProvider.IncludeFile(MemoryFileProvider.DefaultAppSettingsFileName,
            BuildAppSettingsJson(certificateName, firstCertificateFilePath, firstPrivateKeyFilePath));

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddJsonFile(fileProvider, MemoryFileProvider.DefaultAppSettingsFileName, false, true);
        IConfiguration configuration = configurationBuilder.Build();

        IServiceCollection services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(configuration);
        services.ConfigureCertificateOptions(certificateName);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<CertificateOptions>>();
        optionsMonitor.Get(certificateName).Certificate.Should().BeEquivalentTo(firstX509);

        IOptionsChangeTokenSource<CertificateOptions>[] tokenSources = [.. serviceProvider.GetServices<IOptionsChangeTokenSource<CertificateOptions>>()];

        tokenSources.OfType<FilePathInOptionsChangeTokenSource<CertificateOptions>>().Should().HaveCount(2);

        IChangeToken configurationChangeToken =
            tokenSources.OfType<ConfigurationChangeTokenSource<CertificateOptions>>().Should().ContainSingle().Which.GetChangeToken();

        bool configurationChangeCalled = false;

        using (configurationChangeToken.RegisterChangeCallback(_ => configurationChangeCalled = true, null))
        {
            configurationChangeCalled.Should().BeFalse("file path information has not changed yet");

            fileProvider.ReplaceFile(MemoryFileProvider.DefaultAppSettingsFileName,
                BuildAppSettingsJson(certificateName, secondCertificateFilePath, secondPrivateKeyFilePath));

            fileProvider.NotifyChanged();

            configurationChangeCalled.Should().BeTrue("file path information changed");
            optionsMonitor.Get(certificateName).Certificate.Should().Be(secondX509);
        }

        bool certificateContentsChangeCalled = false;
        bool keyContentsChangeCalled = false;

        List<FilePathInOptionsChangeTokenSource<CertificateOptions>> matchingTokenSources =
        [
            .. tokenSources.OfType<FilePathInOptionsChangeTokenSource<CertificateOptions>>()
        ];

        matchingTokenSources.Should().HaveCount(2);

        List<IChangeToken> changeTokens = [.. matchingTokenSources.Select(s => s.GetChangeToken())];

        using IDisposable certificateContentChangeToken = changeTokens[0].RegisterChangeCallback(_ => certificateContentsChangeCalled = true, null);
        using IDisposable keyContentChangeToken = changeTokens[1].RegisterChangeCallback(_ => keyContentsChangeCalled = true, null);

        certificateContentsChangeCalled.Should().BeFalse("certificate file content has not changed yet");
        keyContentsChangeCalled.Should().BeFalse("key file content has not changed yet");
        await File.WriteAllTextAsync(secondCertificateFilePath, firstInstanceCertificate, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(secondPrivateKeyFilePath, firstInstancePrivateKey, TestContext.Current.CancellationToken);

        using Task pollTask = PollCertificateAsync(optionsMonitor, certificateName, firstX509, TestContext.Current.CancellationToken);
        await pollTask.WaitAsync(TimeSpan.FromSeconds(4), TestContext.Current.CancellationToken);

        certificateContentsChangeCalled.Should().BeTrue("certificate file contents changed");
        keyContentsChangeCalled.Should().BeTrue("key file contents changed");
        optionsMonitor.Get(certificateName).Certificate.Should().Be(firstX509);
    }

    private static string BuildAppSettingsJson(string certName, string certPath, string keyPath)
    {
        string certificateBlock = $"""
                "CertificateFilePath": {JsonSerializer.Serialize(certPath)},
                "PrivateKeyFilePath": {JsonSerializer.Serialize(keyPath)}
            """;

        string namedCertificateSection = string.IsNullOrEmpty(certName) ? certificateBlock : $"{JsonSerializer.Serialize(certName)}: {{ {certificateBlock} }}";

        return $$"""
            {
              "Certificates": {
                {{namedCertificateSection}}
              }
            }
            """;
    }

    private static async Task PollCertificateAsync(IOptionsMonitor<CertificateOptions> optionsMonitor, string certificateName,
        X509Certificate2 expectedCertificate, CancellationToken cancellationToken)
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
