// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        using ConfigurationRoot configurationRoot = new ConfigurationBuilder().BuildAsRoot();
        var configureOptions = new ConfigureCertificateOptions(configurationRoot);
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

        using ConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).BuildAsRoot();
        var configureOptions = new ConfigureCertificateOptions(configurationRoot);
        var options = new CertificateOptions();

        configureOptions.Configure(certificateName, options);

        options.Certificate.Should().BeNull();
    }

    [TheorySkippedOnPlatform(nameof(OSPlatform.OSX))]
    [InlineData("")]
    [InlineData(CertificateName)]
    public void ConfigureCertificateOptions_ThrowsOnEmptyFile(string certificateName)
    {
        var appSettings = new Dictionary<string, string?>
        {
            [$"{GetConfigurationKey(certificateName, "CertificateFilePath")}"] = "empty.crt"
        };

        using ConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).BuildAsRoot();

        var configureOptions = new ConfigureCertificateOptions(configurationRoot);
        var options = new CertificateOptions();

        Action configureAction = () => configureOptions.Configure(certificateName, options);
        configureAction.Should().ThrowExactly<CryptographicException>();

        options.Certificate.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(CertificateName)]
    public void ConfigureCertificateOptions_ThrowsOnInvalidKey(string certificateName)
    {
        var appSettings = new Dictionary<string, string?>
        {
            [$"{GetConfigurationKey(certificateName, "CertificateFilePath")}"] = "instance.crt",
            [$"{GetConfigurationKey(certificateName, "PrivateKeyFilePath")}"] = "invalid.key"
        };

        using ConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).BuildAsRoot();
        var configureOptions = new ConfigureCertificateOptions(configurationRoot);
        var options = new CertificateOptions();

        Action configureAction = () => configureOptions.Configure(certificateName, options);
        configureAction.Should().ThrowExactly<CryptographicException>();

        options.Certificate.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(CertificateName)]
    public void ConfigureCertificateOptions_ReadsP12File_CreatesCertificateWithChain(string certificateName)
    {
        var appSettings = new Dictionary<string, string?>
        {
            [$"{GetConfigurationKey(certificateName, "CertificateFilePath")}"] = "instance.p12"
        };

        using ConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).BuildAsRoot();
        var configureOptions = new ConfigureCertificateOptions(configurationRoot);
        var options = new CertificateOptions();

        configureOptions.Configure(certificateName, options);

        using X509Certificate2? certificate = options.Certificate;

        options.Certificate.Should().NotBeNull();
        options.Certificate.HasPrivateKey.Should().BeTrue();
        options.IssuerChain.Should().HaveCount(1);
        using X509Certificate2 issuer = options.IssuerChain[0];
        issuer.Subject.Should().Be("CN=Diego Instance Identity Intermediate CA");
    }

    [Theory]
    [InlineData("")]
    [InlineData(CertificateName)]
    public void ConfigureCertificateOptions_ReadsPemFiles_CreatesCertificateWithChain(string certificateName)
    {
        var appSettings = new Dictionary<string, string?>
        {
            [$"{GetConfigurationKey(certificateName, "CertificateFilePath")}"] = "instance.crt",
            [$"{GetConfigurationKey(certificateName, "PrivateKeyFilePath")}"] = "instance.key"
        };

        using ConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).BuildAsRoot();
        var configureOptions = new ConfigureCertificateOptions(configurationRoot);
        var options = new CertificateOptions();

        configureOptions.Configure(certificateName, options);

        using X509Certificate2? certificate = options.Certificate;

        options.Certificate.Should().NotBeNull();
        options.Certificate.HasPrivateKey.Should().BeTrue();
        options.IssuerChain.Should().HaveCount(1);
        using X509Certificate2 issuer = options.IssuerChain[0];
        issuer.Subject.Should().Be("CN=Diego Instance Identity Intermediate CA");
    }

    [Theory]
    [InlineData("")]
    [InlineData(CertificateName)]
    public async Task CertificateOptions_update_on_changed_contents(string certificateName)
    {
        using var collector = new CertificateCollector();

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
        string appSettingsPath = sandbox.CreateFile("appsettings.json", appSettings);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddJsonFile(appSettingsPath, false, true);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        IServiceCollection services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(_ => configurationRoot);
        services.ConfigureCertificateOptions(certificateName);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<CertificateOptions>>();
        collector.Attach(optionsMonitor, certificateName);
        collector.GetCurrent().Should().BeEquivalentTo(firstX509);

        await WaitUntilCertificateChangedToAsync(certificateName, secondX509, optionsMonitor, async () =>
        {
            await File.WriteAllTextAsync(certificateFilePath, secondCertificateContent, TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(privateKeyFilePath, secondPrivateKeyContent, TestContext.Current.CancellationToken);
        });

        collector.GetCurrent().Should().Be(secondX509);
    }

    [Theory]
    [InlineData("")]
    [InlineData(CertificateName)]
    public async Task CertificateOptions_update_on_changed_path(string certificateName)
    {
        using var collector = new CertificateCollector();

        using var sandbox = new Sandbox();
        string firstCertificateContent = await File.ReadAllTextAsync("instance.crt", TestContext.Current.CancellationToken);
        string firstPrivateKeyContent = await File.ReadAllTextAsync("instance.key", TestContext.Current.CancellationToken);
        using var firstX509 = X509Certificate2.CreateFromPemFile("instance.crt", "instance.key");
        string firstCertificateFilePath = sandbox.CreateFile(Guid.NewGuid() + ".crt", firstCertificateContent);
        string firstPrivateKeyFilePath = sandbox.CreateFile(Guid.NewGuid() + ".key", firstPrivateKeyContent);
        using var secondX509 = X509Certificate2.CreateFromPemFile("secondInstance.crt", "secondInstance.key");
        string appSettings = BuildAppSettingsJson(certificateName, firstCertificateFilePath, firstPrivateKeyFilePath);

        var fileProvider = new MemoryFileProvider();
        fileProvider.IncludeAppSettingsJsonFile(appSettings);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryAppSettingsJsonFile(fileProvider);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        IServiceCollection services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(_ => configurationRoot);
        services.ConfigureCertificateOptions(certificateName);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<CertificateOptions>>();
        collector.Attach(optionsMonitor, certificateName);
        collector.GetCurrent().Should().BeEquivalentTo(firstX509);

        await WaitUntilCertificateChangedToAsync(certificateName, secondX509, optionsMonitor, () =>
        {
            appSettings = BuildAppSettingsJson(certificateName, "secondInstance.crt", "secondInstance.key");
            fileProvider.ReplaceAppSettingsJsonFile(appSettings);
            fileProvider.NotifyChanged();
            return Task.CompletedTask;
        });

        collector.GetCurrent().Should().Be(secondX509);
    }

    private static string BuildAppSettingsJson(string certificateName, string certificatePath, string keyPath)
    {
        string escapedCertificatePath = certificatePath.Replace(@"\", @"\\", StringComparison.Ordinal);
        string escapedKeyPath = keyPath.Replace(@"\", @"\\", StringComparison.Ordinal);

        return string.IsNullOrEmpty(certificateName)
            ? $$"""
            {
              "Certificates": {
                "CertificateFilePath": "{{escapedCertificatePath}}",
                "PrivateKeyFilePath": "{{escapedKeyPath}}"
              }
            }
            """
            : $$"""
            {
              "Certificates": {
                "{{certificateName}}": {
                  "CertificateFilePath": "{{escapedCertificatePath}}",
                  "PrivateKeyFilePath": "{{escapedKeyPath}}"
                }
              }
            }
            """;
    }

    private static async Task WaitUntilCertificateChangedToAsync(string certificateName, X509Certificate2 expectedCertificate,
        IOptionsMonitor<CertificateOptions> optionsMonitor, Func<Task> triggerAction)
    {
        var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        using IDisposable? changeListener = optionsMonitor.OnChange((options, name) =>
        {
            if (name == certificateName && Equals(options.Certificate, expectedCertificate))
            {
                completionSource.TrySetResult();
            }
        });

        await triggerAction();
        await completionSource.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    private static string GetConfigurationKey(string? optionName, string propertyName)
    {
        return string.IsNullOrEmpty(optionName)
            ? string.Join(ConfigurationPath.KeyDelimiter, CertificateOptions.ConfigurationKeyPrefix, propertyName)
            : string.Join(ConfigurationPath.KeyDelimiter, CertificateOptions.ConfigurationKeyPrefix, optionName, propertyName);
    }

    /// <summary>
    /// Collects every certificate that the options monitor creates (including intermediate ones that tests never see, because a changed certificate path
    /// triggers a reload for both the certificate and key file watchers) and disposes them, so that no private key material is left on disk.
    /// </summary>
    private sealed class CertificateCollector : IDisposable
    {
        private readonly HashSet<X509Certificate2> _certificatesToDispose = new(ReferenceEqualityComparer.Instance);
        private IOptionsMonitor<CertificateOptions>? _optionsMonitor;
        private string? _certificateName;
        private IDisposable? _changeListener;

        public void Attach(IOptionsMonitor<CertificateOptions> optionsMonitor, string certificateName)
        {
            _optionsMonitor = optionsMonitor;
            _certificateName = certificateName;

            GetCurrent();

            _changeListener = optionsMonitor.OnChange((options, _) => Add(options.Certificate));
        }

        /// <summary>
        /// Gets the current certificate from the options monitor, which may create a new one when the options were invalidated in the meantime.
        /// </summary>
        public X509Certificate2? GetCurrent()
        {
            if (_optionsMonitor != null)
            {
                X509Certificate2? certificate = _optionsMonitor.Get(_certificateName).Certificate;
                Add(certificate);
                return certificate;
            }

            return null;
        }

        private void Add(X509Certificate2? certificate)
        {
            if (certificate != null)
            {
                lock (_certificatesToDispose)
                {
                    _certificatesToDispose.Add(certificate);
                }
            }
        }

        public void Dispose()
        {
            _changeListener?.Dispose();

            lock (_certificatesToDispose)
            {
                foreach (X509Certificate2 certificate in _certificatesToDispose)
                {
                    certificate.Dispose();
                }

                _certificatesToDispose.Clear();
            }
        }
    }
}
