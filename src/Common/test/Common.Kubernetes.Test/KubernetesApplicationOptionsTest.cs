// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Xunit;

namespace Steeltoe.Common.Kubernetes.Test;

public class KubernetesApplicationOptionsTest
{
    [Fact]
    public void ConstructorBindsValuesFromConfig()
    {
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
                                ""enabled"": false,
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
                                ""enabled"": false,
                                ""sources"": [
                                    {
                                        ""name"": ""testapp.extrasecret"",
                                        ""namespace"": ""not-default2""
                                    }
                                ]
                            },
                            ""reload"": {
                                ""secrets"": true,
                                ""configmaps"": true,
                                ""mode"": ""event"",
                                ""period"": 30
                            }
                        }
                    }
                }
            }";
        var config = new ConfigurationBuilder().AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json))).Build();

        var appInfo = new KubernetesApplicationOptions(config);

        Assert.Equal("testapp", appInfo.Name);
        Assert.Equal("not-default", appInfo.NameSpace);
        Assert.Single(appInfo.Config.Paths);
        Assert.Equal("some/local/path", appInfo.Config.Paths.First());
        Assert.Single(appInfo.Config.Sources);
        Assert.False(appInfo.Config.Enabled);
        Assert.Equal("testapp.extra", appInfo.Config.Sources.First().Name);
        Assert.Equal("not-default1", appInfo.Config.Sources.First().Namespace);
        Assert.False(appInfo.Secrets.Enabled);
        Assert.Single(appInfo.Secrets.Sources);
        Assert.Equal("testapp.extrasecret", appInfo.Secrets.Sources.First().Name);
        Assert.Equal("not-default2", appInfo.Secrets.Sources.First().Namespace);
        Assert.True(appInfo.Reload.Secrets);
        Assert.True(appInfo.Reload.ConfigMaps);
        Assert.Equal(ReloadMethods.Event, appInfo.Reload.Mode);
        Assert.Equal(30, appInfo.Reload.Period);
    }

    [Fact]
    public void Spring_Application_Name__UsedInAppName()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { { "spring:application:name", "springappname" } }).Build();

        var appInfo = new KubernetesApplicationOptions(config);

        Assert.Equal("springappname", appInfo.Name);
    }

    [Fact]
    public void AssemblyNameIsDefaultAppName()
    {
        var config = new ConfigurationBuilder().Build();

        var appInfo = new KubernetesApplicationOptions(config);

        Assert.Equal(Assembly.GetEntryAssembly().GetName().Name, appInfo.Name);
    }
}