// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.Support;
using Xunit;

namespace Steeltoe.Messaging.Converter.Test;

public class NewtonJsonMessageConverterTest
{
    [Fact]
    public void DefaultConstructor()
    {
        var converter = new NewtonJsonMessageConverter();
        Assert.Contains(new MimeType("application", "json", Encoding.UTF8), converter.SupportedMimeTypes);
        Assert.Equal(MissingMemberHandling.Ignore, converter.Settings.MissingMemberHandling);
    }

    [Fact]
    public void MimetypeParametrizedConstructor()
    {
        var mimetype = new MimeType("application", "xml", Encoding.UTF8);
        var converter = new NewtonJsonMessageConverter(mimetype);
        Assert.Contains(mimetype, converter.SupportedMimeTypes);
        Assert.Equal(MissingMemberHandling.Ignore, converter.Settings.MissingMemberHandling);
    }

    [Fact]
    public void MimetypesParametrizedConstructor()
    {
        var jsonMimetype = new MimeType("application", "json", Encoding.UTF8);
        var xmlMimetype = new MimeType("application", "xml", Encoding.UTF8);
        var converter = new NewtonJsonMessageConverter(jsonMimetype, xmlMimetype);
        Assert.Contains(jsonMimetype, converter.SupportedMimeTypes);
        Assert.Contains(xmlMimetype, converter.SupportedMimeTypes);
        Assert.Equal(MissingMemberHandling.Ignore, converter.Settings.MissingMemberHandling);
    }

    [Fact]
    public void FromMessage()
    {
        var converter = new NewtonJsonMessageConverter();

        const string payload = "{" + "\"bytes\":\"AQI=\"," + "\"array\":[\"Foo\",\"Bar\"]," + "\"number\":42," + "\"string\":\"Foo\"," + "\"bool\":true," +
            "\"fraction\":42.0}";

        byte[] bytes = Encoding.UTF8.GetBytes(payload);
        IMessage message = MessageBuilder.WithPayload(bytes).Build();
        var actual = (MyBean)converter.FromMessage(message, typeof(MyBean));

        Assert.Equal("Foo", actual.String);
        Assert.Equal(42, actual.Number);
        Assert.Equal(42F, actual.Fraction);

        Assert.Equal(new[]
        {
            "Foo",
            "Bar"
        }, actual.Array);

        Assert.True(actual.Bool);

        Assert.Equal(new byte[]
        {
            0x1,
            0x2
        }, actual.Bytes);
    }

    [Fact(Skip = "Failing with NewtonSoft, need to dig into")]
    public void FromMessageUntyped()
    {
        var converter = new NewtonJsonMessageConverter();
        const string payload = "{\"bytes\":\"AQI=\",\"array\":[\"Foo\",\"Bar\"]," + "\"number\":42,\"string\":\"Foo\",\"bool\":true,\"fraction\":42.0}";
        byte[] bytes = Encoding.UTF8.GetBytes(payload);
        IMessage message = MessageBuilder.WithPayload(bytes).Build();

        var actual = converter.FromMessage<Dictionary<string, object>>(message);

        Assert.Equal("Foo", actual["string"]);
        Assert.Equal(42L, actual["number"]);
        Assert.Equal(42D, (double)actual["fraction"]);

        Assert.Equal(new[]
        {
            "Foo",
            "Bar"
        }, actual["array"]);

        Assert.Equal(true, actual["bool"]);
        Assert.Equal("AQI=", actual["bytes"]);
    }

    [Fact]
    public void FromMessageMatchingInstance()
    {
        var myBean = new MyBean();
        var converter = new NewtonJsonMessageConverter();
        IMessage message = MessageBuilder.WithPayload(myBean).Build();
        Assert.Same(myBean, converter.FromMessage(message, typeof(MyBean)));
    }

    [Fact]
    public void FromMessageInvalidJson()
    {
        var converter = new NewtonJsonMessageConverter();
        const string payload = "FooBar";
        byte[] bytes = Encoding.UTF8.GetBytes(payload);
        IMessage message = MessageBuilder.WithPayload(bytes).Build();
        Assert.Throws<MessageConversionException>(() => converter.FromMessage<MyBean>(message));
    }

    [Fact]
    public void FromMessageValidJsonWithUnknownProperty()
    {
        var converter = new NewtonJsonMessageConverter();
        const string payload = "{\"string\":\"string\",\"unknownProperty\":\"value\"}";
        byte[] bytes = Encoding.UTF8.GetBytes(payload);
        IMessage message = MessageBuilder.WithPayload(bytes).Build();
        var myBean = converter.FromMessage<MyBean>(message);
        Assert.Equal("string", myBean.String);
    }

    [Fact]
    public void FromMessageToList()
    {
        var converter = new NewtonJsonMessageConverter();
        const string payload = "[1, 2, 3, 4, 5, 6, 7, 8, 9]";
        byte[] bytes = Encoding.UTF8.GetBytes(payload);
        IMessage message = MessageBuilder.WithPayload(bytes).Build();

        ParameterInfo info = GetType().GetMethod(nameof(HandleList), BindingFlags.Instance | BindingFlags.NonPublic).GetParameters()[0];
        object actual = converter.FromMessage(message, typeof(List<long>), info);

        Assert.NotNull(actual);

        Assert.Equal(new List<long>
        {
            1L,
            2L,
            3L,
            4L,
            5L,
            6L,
            7L,
            8L,
            9L
        }, actual);
    }

    [Fact]
    public void FromMessageToMessageWithPojo()
    {
        var converter = new NewtonJsonMessageConverter();
        const string payload = "{\"string\":\"foo\"}";
        byte[] bytes = Encoding.UTF8.GetBytes(payload);
        IMessage message = MessageBuilder.WithPayload(bytes).Build();

        ParameterInfo info = GetType().GetMethod(nameof(HandleMessage), BindingFlags.Instance | BindingFlags.NonPublic).GetParameters()[0];
        object actual = converter.FromMessage(message, typeof(MyBean), info);

        Assert.IsType<MyBean>(actual);
        Assert.Equal("foo", ((MyBean)actual).String);
    }

    [Fact]
    public void ToMessage()
    {
        var converter = new NewtonJsonMessageConverter();

        var payload = new MyBean
        {
            String = "Foo",
            Number = 42,
            Fraction = 42F,
            Array = new[]
            {
                "Foo",
                "Bar"
            },
            Bool = true,
            Bytes = new byte[]
            {
                0x1,
                0x2
            }
        };

        IMessage message = converter.ToMessage(payload, null);

        string actual = Encoding.UTF8.GetString((byte[])message.Payload);

        Assert.Contains("\"string\":\"Foo\"", actual);
        Assert.Contains("\"number\":42", actual);
        Assert.Contains("\"fraction\":42.0", actual);
        Assert.Contains("\"array\":[\"Foo\",\"Bar\"]", actual);
        Assert.Contains("\"bool\":true", actual);
        Assert.Contains("\"bytes\":\"AQI=\"", actual);
        Assert.Equal(new MimeType("application", "json", Encoding.UTF8), message.Headers[MessageHeaders.ContentType]);
    }

    [Fact]
    public void ToMessageUtf16()
    {
        var converter = new NewtonJsonMessageConverter();
        var encoding = new UnicodeEncoding(true, false);
        var contentType = new MimeType("application", "json", encoding);

        var map = new Dictionary<string, object>
        {
            { MessageHeaders.ContentType, contentType }
        };

        var headers = new MessageHeaders(map);
        const string payload = "H\u00e9llo W\u00f6rld";
        IMessage message = converter.ToMessage(payload, headers);
        string actual = encoding.GetString((byte[])message.Payload);
        const string expected = $"\"{payload}\"";
        Assert.Equal(expected, actual);
        Assert.Equal(contentType, message.Headers[MessageHeaders.ContentType]);
    }

    [Fact]
    public void ToMessageUtf16String()
    {
        var converter = new NewtonJsonMessageConverter
        {
            SerializedPayloadClass = typeof(string)
        };

        var encoding = new UnicodeEncoding(true, false);
        var contentType = new MimeType("application", "json", encoding);

        var map = new Dictionary<string, object>
        {
            { MessageHeaders.ContentType, contentType }
        };

        var headers = new MessageHeaders(map);
        const string payload = "H\u00e9llo W\u00f6rld";
        IMessage message = converter.ToMessage(payload, headers);

        Assert.Equal($"\"{payload}\"", message.Payload);
        Assert.Equal(contentType, message.Headers[MessageHeaders.ContentType]);
    }

    [Fact]
    public void GetIMessageGenericType()
    {
        Assert.Null(NewtonJsonMessageConverter.GetIMessageGenericType(typeof(T1)));
        Assert.Equal(typeof(MyBean), NewtonJsonMessageConverter.GetIMessageGenericType(typeof(T2)));
        Assert.Equal(typeof(MyBean), NewtonJsonMessageConverter.GetIMessageGenericType(typeof(T3<MyBean>)));
        Assert.Equal(typeof(MyBean), NewtonJsonMessageConverter.GetIMessageGenericType(typeof(T4<MyBean>)));
        Assert.Equal(typeof(MyBean), NewtonJsonMessageConverter.GetIMessageGenericType(typeof(IMessage<MyBean>)));
        Assert.Equal(typeof(MyBean), NewtonJsonMessageConverter.GetIMessageGenericType(typeof(IMyInterface<MyBean>)));
        Assert.Null(NewtonJsonMessageConverter.GetIMessageGenericType(typeof(IMessage)));
    }

    internal void HandleList(IList<long> payload)
    {
    }

    internal void HandleMessage(IMessage<MyBean> message)
    {
    }

    public interface IMyInterface<T> : IMessage<T>
    {
    }

    public class T1 : IMessage
    {
        public object Payload => throw new NotImplementedException();

        public IMessageHeaders Headers => throw new NotImplementedException();
    }

    public class T2 : IMessage<MyBean>
    {
        object IMessage.Payload => throw new NotImplementedException();

        public MyBean Payload => throw new NotImplementedException();

        public IMessageHeaders Headers => throw new NotImplementedException();
    }

    public class T4<T> : IMyInterface<T>
    {
        object IMessage.Payload => throw new NotImplementedException();

        public T Payload => throw new NotImplementedException();

        public IMessageHeaders Headers => throw new NotImplementedException();
    }

    public class T3<T> : IMessage<T>
    {
        object IMessage.Payload => throw new NotImplementedException();

        public T Payload => throw new NotImplementedException();

        public IMessageHeaders Headers => throw new NotImplementedException();
    }

    public class MyBean
    {
        public byte[] Bytes { get; set; }

        public bool Bool { get; set; }

        public string String { get; set; }

        public string[] Array { get; set; }

        public int Number { get; set; }

        public float Fraction { get; set; }
    }
}
