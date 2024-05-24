// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Options;
using Xunit;

namespace Steeltoe.Common.Security.Test;

public sealed class ConfigureCertificateOptionsTest
{
    private const string CertificateName = "test";

    [Fact]
    public void ConfigureCertificateOptions_ThrowsOnNull()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();
        var configCertOpts = new ConfigureNamedCertificateOptions(configurationRoot);

        var constructorException = Assert.Throws<ArgumentNullException>(() => new ConfigureNamedCertificateOptions(null));
        Assert.Equal("configuration", constructorException.ParamName);
        var configureException = Assert.Throws<ArgumentNullException>(() => configCertOpts.Configure(null, null));
        Assert.Equal("options", configureException.ParamName);
    }

    [Fact]
    public void ConfigureCertificateOptions_NoPath_NoCertificate()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            { $"{CertificateOptions.ConfigurationPrefix}:{CertificateName}:certificate", string.Empty }
        }).Build();

        Assert.NotNull(configurationRoot[$"{CertificateOptions.ConfigurationPrefix}:{CertificateName}:certificate"]);
        var options = new ConfigureNamedCertificateOptions(configurationRoot);
        var opts = new CertificateOptions();
        options.Configure(CertificateName, opts);
        Assert.Null(opts.Certificate);
        Assert.Equal(CertificateName, opts.Name);
    }

    [Fact]
    public void ConfigureCertificateOptions_ReadsFile_CreatesCertificate()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddCertificateFile(CertificateName, "instance.p12").Build();
        Assert.NotNull(configurationRoot[$"{CertificateOptions.ConfigurationPrefix}:{CertificateName}:certificate"]);
        var options = new ConfigureNamedCertificateOptions(configurationRoot);
        var opts = new CertificateOptions();
        options.Configure(CertificateName, opts);
        Assert.NotNull(opts.Certificate);
        Assert.Equal(CertificateName, opts.Name);
        Assert.True(opts.Certificate.HasPrivateKey);
    }
}
