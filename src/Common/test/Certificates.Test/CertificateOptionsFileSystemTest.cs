// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Common.TestResources.IO;

namespace Steeltoe.Common.Certificates.Test;

/// <summary>
/// Tests how <see cref="IOptionsMonitor{TOptions}" /> picks up changes of certificates, using the real file system and clock. The timing of operating
/// system file events is not under our control, so these tests only wait for the outcome.
/// </summary>
public sealed class CertificateOptionsFileSystemTest
{
    private const string CertificateName = "test";

    [Theory]
    [InlineData("", false)]
    [InlineData("", true)]
    [InlineData(CertificateName, false)]
    [InlineData(CertificateName, true)]
    public async Task Applies_changes_to_certificate_file_content(string certificateName, bool useAtomicFileMove)
    {
        using var sandbox = new Sandbox();
        string certificatePath = sandbox.CreateFile("instance.crt", await File.ReadAllTextAsync("instance.crt", TestContext.Current.CancellationToken));
        string privateKeyPath = sandbox.CreateFile("instance.key", await File.ReadAllTextAsync("instance.key", TestContext.Current.CancellationToken));

        var appSettingsProvider = new MemoryFileProvider();
        appSettingsProvider.IncludeAppSettingsJsonFile(GetAppSettingsJson(certificateName, certificatePath, privateKeyPath));

        await using ServiceProvider serviceProvider = BuildServiceProvider(certificateName, appSettingsProvider);
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<CertificateOptions>>();

        using var collector = new CertificateCollector();
        collector.Attach(optionsMonitor, certificateName);

        using var originalCertificate = X509Certificate2.CreateFromPemFile("instance.crt", "instance.key");
        using var rotatedCertificate = X509Certificate2.CreateFromPemFile("secondInstance.crt", "secondInstance.key");

        collector.GetCurrent().Should().Be(originalCertificate);

        await WaitUntilCertificateChangedAsync(certificateName, optionsMonitor, rotatedCertificate, async () =>
        {
            await RotateFileAsync(certificatePath, "secondInstance.crt", useAtomicFileMove);
            await RotateFileAsync(privateKeyPath, "secondInstance.key", useAtomicFileMove);
        });

        collector.GetCurrent().Should().Be(rotatedCertificate);
    }

    [Theory]
    [InlineData("")]
    [InlineData(CertificateName)]
    public async Task Applies_changed_paths_from_the_configuration_file_and_watches_the_new_files(string certificateName)
    {
        using var sandbox = new Sandbox();

        string certificatePathBeforeMove =
            sandbox.CreateFile("before-move.crt", await File.ReadAllTextAsync("instance.crt", TestContext.Current.CancellationToken));

        string privateKeyPathBeforeMove =
            sandbox.CreateFile("before-move.key", await File.ReadAllTextAsync("instance.key", TestContext.Current.CancellationToken));

        string certificatePathAfterMove =
            sandbox.CreateFile("after-move.crt", await File.ReadAllTextAsync("instance.crt", TestContext.Current.CancellationToken));

        string privateKeyPathAfterMove =
            sandbox.CreateFile("after-move.key", await File.ReadAllTextAsync("instance.key", TestContext.Current.CancellationToken));

        var appSettingsProvider = new MemoryFileProvider();
        appSettingsProvider.IncludeAppSettingsJsonFile(GetAppSettingsJson(certificateName, certificatePathBeforeMove, privateKeyPathBeforeMove));

        await using ServiceProvider serviceProvider = BuildServiceProvider(certificateName, appSettingsProvider);
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<CertificateOptions>>();

        using var collector = new CertificateCollector();
        collector.Attach(optionsMonitor, certificateName);

        using var originalCertificate = X509Certificate2.CreateFromPemFile("instance.crt", "instance.key");
        using var rotatedCertificate = X509Certificate2.CreateFromPemFile("secondInstance.crt", "secondInstance.key");

        // Step 1: the options start out with the certificate from the files at the paths in the configuration file.
        collector.GetCurrent().Should().Be(originalCertificate);

        // Step 2: point the configuration to the other pair of files. The certificate stays the same, because the files at the new paths contain the
        // same certificate. We wait for the options to be rebuilt, which proves that the configuration reload was picked up. The configuration is
        // in memory, so it reloads exactly once. Otherwise, a late second reload could pick up the rotated files in step 3 without watching them.
        await WaitUntilCertificateChangedAsync(certificateName, optionsMonitor, originalCertificate, () =>
        {
            appSettingsProvider.ReplaceAppSettingsJsonFile(GetAppSettingsJson(certificateName, certificatePathAfterMove, privateKeyPathAfterMove));
            appSettingsProvider.NotifyChanged();
            return Task.CompletedTask;
        });

        collector.GetCurrent().Should().Be(originalCertificate);

        // Step 3: rotate the files at the new paths. The files at the old paths do not change, so the only way for the rotated certificate to appear is that
        // the files at the new paths are being watched.
        await WaitUntilCertificateChangedAsync(certificateName, optionsMonitor, rotatedCertificate, async () =>
        {
            await RotateFileAsync(certificatePathAfterMove, "secondInstance.crt", false);
            await RotateFileAsync(privateKeyPathAfterMove, "secondInstance.key", false);
        });

        collector.GetCurrent().Should().Be(rotatedCertificate);
    }

    private static string GetAppSettingsJson(string certificateName, string certificatePath, string privateKeyPath)
    {
        // Forward slashes are valid in paths on all platforms, and do not need escaping in JSON.
        string certificateJsonPath = certificatePath.Replace('\\', '/');
        string privateKeyJsonPath = privateKeyPath.Replace('\\', '/');

        return certificateName.Length == 0
            ? $$"""
            {
              "{{CertificateOptions.ConfigurationKeyPrefix}}": {
                "CertificateFilePath": "{{certificateJsonPath}}",
                "PrivateKeyFilePath": "{{privateKeyJsonPath}}"
              }
            }
            """
            : $$"""
            {
              "{{CertificateOptions.ConfigurationKeyPrefix}}": {
                "{{certificateName}}": {
                  "CertificateFilePath": "{{certificateJsonPath}}",
                  "PrivateKeyFilePath": "{{privateKeyJsonPath}}"
                }
              }
            }
            """;
    }

    private static ServiceProvider BuildServiceProvider(string certificateName, MemoryFileProvider appSettingsProvider)
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryAppSettingsJsonFile(appSettingsProvider);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        IServiceCollection services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(_ => configurationRoot);
        services.ConfigureCertificateOptions(certificateName);

        return services.BuildServiceProvider(true);
    }

    /// <summary>
    /// Runs the specified action, then waits until the options monitor reports options with the expected certificate.
    /// </summary>
    private static async Task WaitUntilCertificateChangedAsync(string certificateName, IOptionsMonitor<CertificateOptions> optionsMonitor,
        X509Certificate2 expectedCertificate, Func<Task> triggerAction)
    {
        var certificateChanged = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        using IDisposable? changeListener = optionsMonitor.OnChange((options, name) =>
        {
            if (name == certificateName && Equals(options.Certificate, expectedCertificate))
            {
                certificateChanged.TrySetResult();
            }
        });

        await triggerAction();
        await certificateChanged.Task.WaitAsync(Timeouts.Safety, TestContext.Current.CancellationToken);
    }

    private static async Task RotateFileAsync(string path, string rotatedContentPath, bool useAtomicFileMove)
    {
        string content = await File.ReadAllTextAsync(rotatedContentPath, TestContext.Current.CancellationToken);
        await WriteFileAsync(path, content, useAtomicFileMove);
    }

    private static async Task WriteFileAsync(string path, string content, bool useAtomicFileMove)
    {
        // Certificate files are deliberately read without allowing writers, so that a file is never read while it is being written. The consequence
        // is that writing a file can fail briefly while the options are being rebuilt, which can happen between the writes of the certificate and the
        // private key file when the machine is heavily loaded. Rotation tools are expected to retry, as we do here.
        int attempt = 0;

        while (true)
        {
            attempt++;

            try
            {
                if (useAtomicFileMove)
                {
                    string temporaryPath = $"{path}.tmp";
                    await File.WriteAllTextAsync(temporaryPath, content, TestContext.Current.CancellationToken);
                    File.Move(temporaryPath, path, true);
                }
                else
                {
                    await File.WriteAllTextAsync(path, content, TestContext.Current.CancellationToken);
                }

                return;
            }
            catch (IOException) when (attempt < 20)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(50), TestContext.Current.CancellationToken);
            }
        }
    }
}
