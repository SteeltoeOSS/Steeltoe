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
using Microsoft.Extensions.Options;
using Xunit;

namespace Steeltoe.Common.Security.Test
{
    public class PemConfigureCertificateOptionsTest
    {
        [Fact]
        public void AddPemFiles_ReadsFiles_CreatesCertificate()
        {
            var config = new ConfigurationBuilder()
                .AddPemFiles("instance.crt", "instance.key")
                .Build();
            Assert.NotNull(config["certificate"]);
            Assert.NotNull(config["privateKey"]);
            var pemConfig = new PemConfigureCertificateOptions(config);
            CertificateOptions opts = new CertificateOptions();
            pemConfig.Configure(opts);
            Assert.NotNull(opts.Certificate);
            Assert.Equal(Options.DefaultName, opts.Name);
            Assert.True(opts.Certificate.HasPrivateKey);
        }
    }
}
