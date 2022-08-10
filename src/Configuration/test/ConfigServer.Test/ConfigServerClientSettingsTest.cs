// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Extensions.Configuration.ConfigServer.Test;

public class ConfigServerClientSettingsTest
{
    [Fact]
    public void DefaultConstructor_InitializedWithDefaults()
    {
        var settings = new ConfigServerClientSettings();

        TestHelper.VerifyDefaults(settings);
    }

    [Fact]
    public void GetRawUris_GoodWithUserPass()
    {
        var settings = new ConfigServerClientSettings
        {
            Uri = "https://user:pass@localhost:8888/"
        };

        Assert.Equal("https://localhost:8888/", settings.RawUris[0]);
    }

    [Fact]
    public void GetRawUris_MultipleUris_GoodWithUserPass()
    {
        var settings = new ConfigServerClientSettings
        {
            Uri = "https://user:pass@localhost:8888/, https://user:pass@localhost:9999/"
        };

        Assert.Equal("https://localhost:8888/", settings.RawUris[0]);
        Assert.Equal("https://localhost:9999/", settings.RawUris[1]);
    }

    [Fact]
    public void GetRawUris_Bad()
    {
        var settings = new ConfigServerClientSettings
        {
            Uri = "blahblah"
        };

        Assert.Empty(settings.RawUris);
    }

    [Fact]
    public void GetUserName_GoodWithUserPassOnUri()
    {
        var settings = new ConfigServerClientSettings
        {
            Uri = "https://user:pass@localhost:8888/"
        };

        Assert.Equal("user", settings.Username);
    }

    [Fact]
    public void GetUserName_MultipleUrisWithUserPass_ReturnsNull()
    {
        var settings = new ConfigServerClientSettings
        {
            Uri = "https://user:pass@localhost:8888/, https://user1:pass1@localhost:9999/"
        };

        Assert.Null(settings.Username);
    }

    [Fact]
    public void GetUserName_MultipleUrisWithUserPass_ReturnsUserNameSetting()
    {
        var settings = new ConfigServerClientSettings
        {
            Uri = "https://user:pass@localhost:8888/, https://user1:pass1@localhost:9999/",
            Username = "user"
        };

        Assert.Equal("user", settings.Username);
    }

    [Fact]
    public void GetPassword_GoodWithUserPassOnUri()
    {
        var settings = new ConfigServerClientSettings
        {
            Uri = "https://user:pass@localhost:8888/"
        };

        Assert.Equal("pass", settings.Password);
    }

    [Fact]
    public void GetPassword_MultipleUrisWithUserPass_ReturnsNull()
    {
        var settings = new ConfigServerClientSettings
        {
            Uri = "https://user:pass@localhost:8888/, https://user1:pass1@localhost:9999/"
        };

        Assert.Null(settings.Password);
    }

    [Fact]
    public void GetPassword_MultipleUrisWithUserPass_ReturnsPasswordSetting()
    {
        var settings = new ConfigServerClientSettings
        {
            Uri = "https://user:pass@localhost:8888/, https://user1:pass1@localhost:9999/",
            Password = "password"
        };

        Assert.Equal("password", settings.Password);
    }

    [Fact]
    public void GetUserName_GoodWithUserPassOnUri_SettingsOverrides()
    {
        var settings = new ConfigServerClientSettings
        {
            Uri = "https://user:pass@localhost:8888/",
            Username = "explicitOverrides"
        };

        Assert.Equal("explicitOverrides", settings.Username);
        Assert.Equal("pass", settings.Password);
    }

    [Fact]
    public void GetPassword_GoodWithUserPassOnUri_SettingsOverrides()
    {
        var settings = new ConfigServerClientSettings
        {
            Uri = "https://user:pass@localhost:8888/",
            Password = "explicitOverrides"
        };

        Assert.Equal("explicitOverrides", settings.Password);
        Assert.Equal("user", settings.Username);
    }

    [Fact]
    public void GetUserName_MultipleUrisWithUserPass_SettingsUsed()
    {
        var settings = new ConfigServerClientSettings
        {
            Uri = "https://user:pass@localhost:8888/, https://user1:pass1@localhost:9999/",
            Username = "explicitOverrides"
        };

        Assert.Equal("explicitOverrides", settings.Username);
        Assert.Null(settings.Password);
    }

    [Fact]
    public void GetPassword_MultipleUrisWithUserPass_SettingsOverrides()
    {
        var settings = new ConfigServerClientSettings
        {
            Uri = "https://user:pass@localhost:8888/, https://user1:pass1@localhost:9999/",
            Password = "explicitOverrides"
        };

        Assert.Equal("explicitOverrides", settings.Password);
        Assert.Null(settings.Username);
    }
}
