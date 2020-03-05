// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Options;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Common.Security.Test
{
    public class ConfigureCertificateOptionsTest
    {
        [Fact]
        public void ConfigureCertificateOptions_ThrowsOnNull()
        {
            var config = new ConfigurationBuilder().Build();
            var configCertOpts = new ConfigureCertificateOptions(config);

            var constructorException = Assert.Throws<ArgumentNullException>(() => new ConfigureCertificateOptions(null));
            Assert.Equal("config", constructorException.ParamName);
            var configureException = Assert.Throws<ArgumentNullException>(() => configCertOpts.Configure(null, null));
            Assert.Equal("options", configureException.ParamName);
        }

        [Fact]
        public void ConfigureCertificateOptions_NoPath_NoCertificate()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { { "certificate", string.Empty } })
                .Build();
            Assert.NotNull(config["certificate"]);
            var pkcs12Config = new ConfigureCertificateOptions(config);
            var opts = new CertificateOptions();
            pkcs12Config.Configure(opts);
            Assert.Null(opts.Certificate);
            Assert.Equal(Microsoft.Extensions.Options.Options.DefaultName, opts.Name);
        }

        [Fact]
        [Trait("Category", "SkipOnMacOS")]
        public void ConfigureCertificateOptions_ReadsFile_CreatesCertificate()
        {
            // Skipped on Mac due to inability to open a PKCS#12 with no password
            // https://github.com/dotnet/runtime/issues/23635
            var config = new ConfigurationBuilder()
                .AddCertificateFile("instance.p12")
                .Build();
            Assert.NotNull(config["certificate"]);
            var pkcs12Config = new ConfigureCertificateOptions(config);
            var opts = new CertificateOptions();
            pkcs12Config.Configure(opts);
            Assert.NotNull(opts.Certificate);
            Assert.Equal(Microsoft.Extensions.Options.Options.DefaultName, opts.Name);
            Assert.True(opts.Certificate.HasPrivateKey);
        }
    }
}
