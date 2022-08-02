// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Xunit;

namespace Steeltoe.Extensions.Configuration.ConfigServer.Test;

public class ConfigurationSettingsHelperTest
{
    [Fact]
    public void Initialize_ThrowsOnNulls()
    {
        const string configPrefix = null;
        const ConfigServerClientSettings settings = null;
        const IConfiguration config = null;

        var ex = Assert.Throws<ArgumentNullException>(() => ConfigurationSettingsHelper.Initialize(configPrefix, settings, config));
        Assert.Contains(nameof(configPrefix), ex.Message);
        ex = Assert.Throws<ArgumentNullException>(() => ConfigurationSettingsHelper.Initialize("foobar", settings, config));
        Assert.Contains(nameof(settings), ex.Message);
        ex = Assert.Throws<ArgumentNullException>(() => ConfigurationSettingsHelper.Initialize("foobar", new ConfigServerClientSettings(), config));
        Assert.Contains(nameof(config), ex.Message);
    }

    [Fact]
    public void Initialize_WithDefaultSettings()
    {
        string prefix = "spring:cloud:config";
        var settings = new ConfigServerClientSettings();
        IConfiguration config = new ConfigurationRoot(new List<IConfigurationProvider>());

        ConfigurationSettingsHelper.Initialize(prefix, settings, config);
        TestHelper.VerifyDefaults(settings);
    }
}
