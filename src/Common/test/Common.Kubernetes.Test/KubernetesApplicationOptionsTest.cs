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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Xunit;

namespace Steeltoe.Common.Kubernetes.Test
{
    public class KubernetesApplicationOptionsTest
    {
        [Fact]
        public void ConstructorBindsValuesFromConfig()
        {
            // arrange
            var json = @"
            {
                ""spring"": {
                    ""application"": {
                        ""name"": ""springappname""
                    },
                    ""cloud"": {
                        ""kubernetes"": {
                            ""name"": ""testapp"",
                            ""namespace"": ""not-default"",
                            ""config"": {
                                ""paths"": [
                                    ""some/local/path""
                                ],
                                ""sources"": [
                                    {
                                        ""name"": ""testapp.extra"",
                                        ""namespace"": ""not-default1""
                                    }
                                ]
                            },
                            ""secrets"": {
                                ""sources"": [
                                    {
                                        ""name"": ""testapp.extrasecret"",
                                        ""namespace"": ""not-default2""
                                    }
                                ]
                            },
                            ""reload"": {
                                ""enabled"": true
                            }
                        }
                    }
                }
            }";
            var config = new ConfigurationBuilder().AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json))).Build();

            // act
            var appInfo = new KubernetesApplicationOptions(config);

            // assert
            Assert.Equal("testapp", appInfo.Name);
            Assert.Equal("not-default", appInfo.NameSpace);
            Assert.Single(appInfo.Config.Paths);
            Assert.Equal("some/local/path", appInfo.Config.Paths.First());
            Assert.Single(appInfo.Config.Sources);
            Assert.True(appInfo.Config.Enabled);
            Assert.Equal("testapp.extra", appInfo.Config.Sources.First().Name);
            Assert.Equal("not-default1", appInfo.Config.Sources.First().Namespace);
            Assert.True(appInfo.Secrets.Enabled);
            Assert.Single(appInfo.Secrets.Sources);
            Assert.Equal("testapp.extrasecret", appInfo.Secrets.Sources.First().Name);
            Assert.Equal("not-default2", appInfo.Secrets.Sources.First().Namespace);
            Assert.True(appInfo.Reload.Enabled);
        }

        [Fact]
        public void Spring_Application_Name__UsedInAppName()
        {
            // arrange
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { { "spring:application:name", "springappname" } }).Build();

            // act
            var appInfo = new KubernetesApplicationOptions(config);

            // assert
            Assert.Equal("springappname", appInfo.Name);
        }

        [Fact]
        public void AssemblyNameIsDefaultAppName()
        {
            // arrange
            var config = new ConfigurationBuilder().Build();

            // act
            var appInfo = new KubernetesApplicationOptions(config);

            // assert
            Assert.Equal(Assembly.GetEntryAssembly().GetName().Name, appInfo.Name);
        }
    }
}
