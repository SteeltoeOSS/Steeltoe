// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Options;
using Xunit;

namespace Steeltoe.Common.Security.Test;

public class ConfigureCertificateOptionsTest
{
    [Fact]
    public void ConfigureCertificateOptions_ThrowsOnNull()
    {
        IConfigurationRoot config = new ConfigurationBuilder().Build();
        var configCertOpts = new ConfigureCertificateOptions(config);

        var constructorException = Assert.Throws<ArgumentNullException>(() => new ConfigureCertificateOptions(null));
        Assert.Equal("config", constructorException.ParamName);
        var configureException = Assert.Throws<ArgumentNullException>(() => configCertOpts.Configure(null, null));
        Assert.Equal("options", configureException.ParamName);
    }

    [Fact]
    public void ConfigureCertificateOptions_NoPath_NoCertificate()
    {
        IConfigurationRoot config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            { "certificate", string.Empty }
        }).Build();

        Assert.NotNull(config["certificate"]);
        var options = new ConfigureCertificateOptions(config);
        var opts = new CertificateOptions();
        options.Configure(opts);
        Assert.Null(opts.Certificate);
        Assert.Equal(Microsoft.Extensions.Options.Options.DefaultName, opts.Name);
    }

    [Fact]
    [Trait("Category", "SkipOnMacOS")]
    public void ConfigureCertificateOptions_ReadsFile_CreatesCertificate()
    {
        // Skipped on Mac due to inability to open a PKCS#12 with no password
        // https://github.com/dotnet/runtime/issues/23635
        IConfigurationRoot config = new ConfigurationBuilder().AddCertificateFile("instance.p12").Build();
        Assert.NotNull(config["certificate"]);
        var options = new ConfigureCertificateOptions(config);
        var opts = new CertificateOptions();
        options.Configure(opts);
        Assert.NotNull(opts.Certificate);
        Assert.Equal(Microsoft.Extensions.Options.Options.DefaultName, opts.Name);
        Assert.True(opts.Certificate.HasPrivateKey);
    }
}
