// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Xunit;

namespace Steeltoe.Configuration.ConfigServer.Test;

public sealed class ConfigurationSettingsHelperTest
{
    [Fact]
    public void Initialize_ThrowsOnNulls()
    {
        const string sectionPrefix = null;
        const ConfigServerClientSettings settings = null;
        const IConfiguration configuration = null;

        var ex = Assert.Throws<ArgumentNullException>(() => ConfigurationSettingsHelper.Initialize(sectionPrefix, settings, configuration));
        Assert.Contains(nameof(sectionPrefix), ex.Message, StringComparison.Ordinal);
        ex = Assert.Throws<ArgumentNullException>(() => ConfigurationSettingsHelper.Initialize("foobar", settings, configuration));
        Assert.Contains(nameof(settings), ex.Message, StringComparison.Ordinal);
        ex = Assert.Throws<ArgumentNullException>(() => ConfigurationSettingsHelper.Initialize("foobar", new ConfigServerClientSettings(), configuration));
        Assert.Contains(nameof(configuration), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Initialize_WithDefaultSettings()
    {
        const string prefix = "spring:cloud:config";
        var settings = new ConfigServerClientSettings();
        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>());

        ConfigurationSettingsHelper.Initialize(prefix, settings, configuration);
        TestHelper.VerifyDefaults(settings);
    }
}
