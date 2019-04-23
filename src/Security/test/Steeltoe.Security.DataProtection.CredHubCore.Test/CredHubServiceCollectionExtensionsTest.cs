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
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Security.DataProtection.CredHub;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Steeltoe.Security.DataProtection.CredHubCore.Test
{
    public class CredHubServiceCollectionExtensionsTest
    {
        ////[Fact(Skip = "This test is incomplete, would require a mocked server to return the token")]
        ////public void AddCredHubClient_WithUAACreds_UsesUAACreds()
        ////{
        ////    // Arrange
        ////    IServiceCollection services = new ServiceCollection();
        ////    var appsettings = new Dictionary<string, string>()
        ////    {
        ////        ["CredHubClient:CredHubUser"] = "credhub_client",
        ////        ["CredHubClient:CredHubPassword"] = "secret",
        ////    };

        ////    ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
        ////    configurationBuilder.AddInMemoryCollection(appsettings);
        ////    var config = configurationBuilder.Build();

        ////    // Act and Assert
        ////    services.AddCredHubClient(config);
        ////    Assert.True(services.Contains(new ServiceDescriptor(typeof(ICredHubClient), null)));
        ////}
    }
}
