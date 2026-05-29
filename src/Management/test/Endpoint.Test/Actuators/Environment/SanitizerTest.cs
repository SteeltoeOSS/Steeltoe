// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Actuators.Environment;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Environment;

public sealed class SanitizerTest
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("normalValue", "normalValue")]
    [InlineData("amqp://127.0.0.1", "amqp://127.0.0.1")]
    [InlineData("https://host:8080/path", "https://host:8080/path")]
    [InlineData("http://user@host", "http://user@host")]
    public void Sanitize_Does_Not_Mask_Non_Sensitive_Key_Value(string? value, string? expected)
    {
        var sanitizer = new Sanitizer([
            "password",
            "secret"
        ]);

        sanitizer.Sanitize("someKey", value).Should().Be(expected);
    }

    [Theory]
    [InlineData("somePassword", null)]
    [InlineData("mySecret", null)]
    [InlineData("somePassword", "mysecretvalue")]
    [InlineData("mySecret", "anothervalue")]
    public void Sanitize_Fully_Masks_Sensitive_Key_Value(string key, string? value)
    {
        var sanitizer = new Sanitizer([
            "password",
            "secret"
        ]);

        sanitizer.Sanitize(key, value).Should().Be("******");
    }

    [Theory]
    [InlineData("ConnectionStrings:OrderDb", "Server=orders.db.com;Pwd=order-pass;Uid=order-user")]
    [InlineData("vcap:services:p.rabbitmq:0:credentials:uri", "amqp://user:pass@127.0.0.1/instance")]
    public void Sanitize_Fully_Masks_Sensitive_Key_Value_Even_When_Value_Contains_Credentials(string key, string value)
    {
        var sanitizer = new Sanitizer([
            ".*connectionstring.*",
            ".*credentials.*"
        ]);

        sanitizer.Sanitize(key, value).Should().Be("******");
    }

    [Theory]
    [InlineData("host=localhost;password=secret;port=1", "host=localhost;password=******;port=1")]
    [InlineData("host=localhost;pwd=secret;port=1", "host=localhost;pwd=******;port=1")]
    [InlineData("password=secret;host=localhost;port=1", "password=******;host=localhost;port=1")]
    [InlineData("host=localhost;port=1;password=secret", "host=localhost;port=1;password=******")]
    [InlineData("host=localhost;notapassword=secret;port=1", "host=localhost;notapassword=secret;port=1")]
    [InlineData("PWD=secret;Password=other", "PWD=******;Password=******")]
    [InlineData("password=secret", "password=******")]
    [InlineData("host=1; password = abc ;port=2", "host=1; password = ******;port=2")]
    [InlineData("", "")]
    [InlineData("host=localhost;notapassword=secret;password=real;port=1", "host=localhost;notapassword=secret;password=******;port=1")]
    [InlineData("host=localhost;password=;port=1", "host=localhost;password=;port=1")]
    [InlineData("password=;host=localhost", "password=;host=localhost")]
    [InlineData("host=localhost;port=1;password=", "host=localhost;port=1;password=")]
    [InlineData("host=localhost;pwd=;password=;port=1", "host=localhost;pwd=;password=;port=1")]
    [InlineData("host=localhost;password=;password=secret;port=1", "host=localhost;password=;password=******;port=1")]
    [InlineData("amqp://user:pass@127.0.0.1", "amqp://user:******@127.0.0.1")]
    [InlineData("https://user:pass@host:8080/path", "https://user:******@host:8080/path")]
    [InlineData("ftp://user:pass@host", "ftp://user:******@host")]
    [InlineData("http://:password@127.0.0.1", "http://:******@127.0.0.1")]
    [InlineData("http://user1:pass1@127.0.0.1,https://user2:pass2@host2", "http://user1:******@127.0.0.1,https://user2:******@host2")]
    public void Sanitize_Masks_Passwords_Within_Uri_And_Connection_String_Values(string input, string expected)
    {
        var sanitizer = new Sanitizer([]);

        sanitizer.Sanitize("someKey", input).Should().Be(expected);
    }
}
