// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Extensions.Configuration.ConfigServer.Test;

public class ConfigurationSettingsHelperTest
{
    [Fact]
    public void Initalize_ThrowsOnNulls()
    {
        string configPrefix = null;
        ConfigServerClientSettings settings = null;
        IConfiguration config = null;

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
        var prefix = "spring:cloud:config";
        var settings = new ConfigServerClientSettings();
        IConfiguration config = new ConfigurationRoot(new List<IConfigurationProvider>());

        ConfigurationSettingsHelper.Initialize(prefix, settings, config);
        TestHelper.VerifyDefaults(settings);
    }
}