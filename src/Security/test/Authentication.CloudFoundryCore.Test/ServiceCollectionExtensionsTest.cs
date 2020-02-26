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

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Security;
using System;
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test
{
    public class ServiceCollectionExtensionsTest
    {
        [Fact]
        public void AddCloudFoundryCertificateAuth_ChecksNulls()
        {
            // arrange
            var sColl = new ServiceCollection();

            // act & assert
            var servicesException = Assert.Throws<ArgumentNullException>(() => ServiceCollectionExtensions.AddCloudFoundryCertificateAuth(null, null));
            var configException = Assert.Throws<ArgumentNullException>(() => ServiceCollectionExtensions.AddCloudFoundryCertificateAuth(sColl, null));
            Assert.Equal("services", servicesException.ParamName);
            Assert.Equal("configuration", configException.ParamName);
        }

        [Fact]
        public void AddCloudFoundryCertificateAuth_AddsServices()
        {
            // arrange
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
            services.AddSingleton<IConfiguration>(config);

            // act
            services.AddCloudFoundryCertificateAuth(config);
            var provider = services.BuildServiceProvider();

            // assert
            Assert.NotNull(provider.GetRequiredService<IOptions<CertificateOptions>>());
            Assert.NotNull(provider.GetRequiredService<ICertificateRotationService>());
            Assert.NotNull(provider.GetRequiredService<IAuthorizationHandler>());
        }
    }
}
