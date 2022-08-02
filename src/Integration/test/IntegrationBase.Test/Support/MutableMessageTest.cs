// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;
using Xunit;

namespace Steeltoe.Integration.Support.Test;

public class MutableMessageTest
{
    [Fact]
    public void TestMessageIdTimestampRemains()
    {
        var uuid = Guid.NewGuid();
        long timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        object payload = new object();

        var headerDictionary = new Dictionary<string, object>
        {
            { MessageHeaders.IdName, uuid },
            { MessageHeaders.TimestampName, timestamp }
        };

        var mutableMessage = new MutableMessage<object>(payload, headerDictionary);
        var headers = mutableMessage.Headers as MutableMessageHeaders;

        Assert.Equal(uuid.ToString(), headers.RawHeaders[MessageHeaders.IdName]);
        Assert.Equal(timestamp, headers.RawHeaders[MessageHeaders.TimestampName]);
    }

    [Fact]
    public void TestMessageHeaderIsSettable()
    {
        object payload = new object();
        var headerDictionary = new Dictionary<string, object>();
        var additional = new Dictionary<string, object>();

        var mutableMessage = new MutableMessage<object>(payload, headerDictionary);
        var headers = mutableMessage.Headers as MutableMessageHeaders;

        // Should not throw an UnsupportedOperationException
        headers.Add("foo", "bar");
        headers.Add("eep", "bar");
        headers.Remove("eep");
        headers.AddRange(additional);

        Assert.Equal("bar", headers.RawHeaders["foo"]);
    }

    [Fact]
    public void TestMessageHeaderIsSerializable()
    {
        object payload = new object();

        var uuid = Guid.NewGuid();
        long timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        // UUID as string; timestamp as string
        var headerDictionaryStrings = new Dictionary<string, object>
        {
            { MessageHeaders.IdName, uuid.ToString() },
            { MessageHeaders.TimestampName, timestamp.ToString() }
        };

        var mutableMessageStrings = new MutableMessage<object>(payload, headerDictionaryStrings);
        Assert.Equal(uuid.ToString(), mutableMessageStrings.Headers.Id);
        Assert.Equal(timestamp, mutableMessageStrings.Headers.Timestamp);

        // UUID as byte[]; timestamp as Long
        var headerDictionaryByte = new Dictionary<string, object>();
        byte[] uuidAsBytes = uuid.ToByteArray();

        headerDictionaryByte.Add(MessageHeaders.IdName, uuidAsBytes);
        headerDictionaryByte.Add(MessageHeaders.TimestampName, timestamp);
        var mutableMessageBytes = new MutableMessage<object>(payload, headerDictionaryByte);
        Assert.Equal(uuid.ToString(), mutableMessageBytes.Headers.Id);
        Assert.Equal(timestamp, mutableMessageBytes.Headers.Timestamp);
    }
}
